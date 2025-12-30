using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Config;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Game.Scene;
using EggLink.DanhengServer.GameServer.Game.Scene.Entity;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;
using System.Collections.Generic;

namespace EggLink.DanhengServer.GameServer.Game.Battle;

public class SceneSkillManager(PlayerInstance player) : BasePlayerManager(player)
{
    private static readonly Logger Logger = new("SceneSkill");

    public async ValueTask<SkillResultData> OnCast(SceneCastSkillCsReq req)
    {
        var scene = Player.SceneInstance;
        if (scene == null) return new SkillResultData(Retcode.RetFail);

        var castEntityId = (int)req.CastEntityId;
        var attackedByEntityId = (int)req.AttackedByEntityId;

        var castEntity = castEntityId != 0 ? scene.Entities.GetValueOrDefault(castEntityId) : null;
        var attackedByEntity = attackedByEntityId != 0 ? scene.Entities.GetValueOrDefault(attackedByEntityId) : null;

        // Determine caster robustly across client versions.
        BaseGameEntity? attackEntity = castEntity as AvatarSceneInfo
                                       ?? attackedByEntity as AvatarSceneInfo
                                       ?? castEntity
                                       ?? attackedByEntity;

        if (attackEntity == null)
            return new SkillResultData(Retcode.RetSceneEntityNotExist);

        // Build target list.
        var targetEntities = new List<BaseGameEntity>();
        var targetEntityIds = new HashSet<int>();

        void AddTargets(IEnumerable<uint> ids)
        {
            foreach (var id in ids)
            {
                if (id == 0) continue;
                targetEntityIds.Add((int)id);
            }
        }

        // New clients populate `hit_target_entity_id_list` for overworld attacks; keep old fields as fallback.
        AddTargets(req.HitTargetEntityIdList);
        AddTargets(req.AssistMonsterEntityIdList);
        foreach (var info in req.AssistMonsterEntityInfo)
            AddTargets(info.EntityIdList);

        // Some clients set attacked_by_entity_id to the monster that got hit (despite the field name).
        if (attackedByEntityId != 0 && attackedByEntity is EntityMonster)
            targetEntityIds.Add(attackedByEntityId);
        if (castEntityId != 0 && castEntity is EntityMonster)
            targetEntityIds.Add(castEntityId);

        foreach (var id in targetEntityIds)
            if (scene.Entities.TryGetValue(id, out var entity))
                targetEntities.Add(entity);

        if (attackEntity is AvatarSceneInfo && targetEntities.Count == 0)
        {
            Logger.Debug(
                $"SceneCastSkill has no targets: uid={Player.Uid}, cast_entity_id={castEntityId}, attacked_by_entity_id={attackedByEntityId}, skill_index={req.SkillIndex}");
        }
        // get ability file
        var abilities = GetAbilityConfig(attackEntity);
        if (abilities == null || abilities.AbilityList.Count < 1)
            return new SkillResultData(Retcode.RetMazeNoAbility);

        var abilityName = !string.IsNullOrEmpty(req.MazeAbilityStr) ? req.MazeAbilityStr :
            req.SkillIndex == 0 ? "NormalAtk01" : "MazeSkill";
        var targetAbility = abilities.AbilityList.Find(x => x.Name.Contains(abilityName));
        if (targetAbility == null)
        {
            targetAbility = abilities.AbilityList.FirstOrDefault();
            if (targetAbility == null)
                return new SkillResultData(Retcode.RetMazeNoAbility);
        }

        // execute ability
        var res = await Player.TaskManager!.AbilityLevelTask.TriggerTasks(abilities, targetAbility.OnStart,
            attackEntity, targetEntities, req);

        var instance = res.Instance;

        // Fallback: some client/build combinations don't include (or we fail to resolve) the AdventureTriggerAttack tasks
        // that should start a battle. If we clearly have an avatar casting at monsters, try to start battle anyway.
        // This is intentionally conservative: StartBattle will return null when targets are dead/invalid.
        if (instance == null && attackEntity is AvatarSceneInfo && targetEntities.Any(e => e is EntityMonster))
        {
            var battle = await Player.BattleManager!.StartBattle(attackEntity, targetEntities, req.SkillIndex == 1);
            if (battle != null)
            {
                Logger.Debug(
                    $"Fallback StartBattle succeeded: uid={Player.Uid}, cast_entity_id={castEntityId}, attacked_by_entity_id={attackedByEntityId}, targets={targetEntities.Count}");
                instance = battle;
            }
            else
            {
                Logger.Debug(
                    $"Fallback StartBattle failed: uid={Player.Uid}, cast_entity_id={castEntityId}, attacked_by_entity_id={attackedByEntityId}, targets={targetEntities.Count}");
            }
        }

        // check if avatar execute
        if (attackEntity is AvatarSceneInfo) await Player.SceneInstance!.OnUseSkill(req);

        return new SkillResultData(Retcode.RetSucc, instance, res.BattleInfos);
    }

    private AdventureAbilityConfigListInfo? GetAbilityConfig(BaseGameEntity entity)
    {
        if (entity is EntityMonster monster)
            return GameData.AdventureAbilityConfigListData.GetValueOrDefault(monster.MonsterData.ID);

        if (entity is AvatarSceneInfo avatar)
            if (GameData.AvatarConfigData.TryGetValue(avatar.AvatarInfo.AvatarId, out var excel))
                return GameData.AdventureAbilityConfigListData.GetValueOrDefault(excel.AdventurePlayerID);

        return null;
    }
}

public record SkillResultData(
    Retcode RetCode,
    BattleInstance? Instance = null,
    List<HitMonsterInstance>? TriggerBattleInfos = null);
