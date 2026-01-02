using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Enums.Mission;
using EggLink.DanhengServer.Enums.Quest;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using System.Collections.Generic;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Mission;

public class PacketGetMissionDataScRsp : BasePacket
{
    public PacketGetMissionDataScRsp(PlayerInstance player) : base(CmdIds.GetMissionDataScRsp)
    {
        var proto = new GetMissionDataScRsp
        {
            TrackMissionId = (uint)player.MissionManager!.Data.TrackingMainMissionId
        };

        // When missions are disabled, the client may still evaluate feature unlocks (FuncUnlockData)
        // against mission completion. Provide a minimal "finished mission" view so UI entries don't stay locked.
        if (!ConfigManager.Config.ServerOption.EnableMission)
        {
            HashSet<int> requiredFinishedMainMissions = [];
            HashSet<int> requiredFinishedSubMissions = [];

            foreach (var unlock in GameData.FuncUnlockDataData.Values)
            {
                foreach (var condition in unlock.Conditions)
                {
                    if (!int.TryParse(condition.Param, out var id)) continue;

                    switch (condition.Type)
                    {
                        case ConditionTypeEnum.FinishMainMission:
                            requiredFinishedMainMissions.Add(id);
                            break;
                        case ConditionTypeEnum.FinishSubMission:
                        case ConditionTypeEnum.RealFinishSubMission:
                            requiredFinishedSubMissions.Add(id);
                            break;
                    }
                }
            }

            foreach (var id in requiredFinishedMainMissions)
                proto.CFOMOIPJFDJ.Add((uint)id);

            foreach (var id in requiredFinishedSubMissions)
                proto.MissionList.Add(new Proto.Mission
                {
                    Id = (uint)id,
                    Status = MissionStatus.MissionFinish,
                    Progress = (uint)player.MissionManager!.GetMissionProgress(id)
                });
        }

        foreach (var mission in GameData.MainMissionData.Keys)
            if (player.MissionManager!.GetMainMissionStatus(mission) == MissionPhaseEnum.Accept)
                proto.MainMissionList.Add(new MainMission
                {
                    Id = (uint)mission,
                    Status = MissionStatus.MissionDoing
                });

        foreach (var mission in GameData.SubMissionInfoData.Keys)
            if (player.MissionManager!.GetSubMissionStatus(mission) == MissionPhaseEnum.Accept)
                proto.MissionList.Add(new Proto.Mission
                {
                    Id = (uint)mission,
                    Status = MissionStatus.MissionDoing,
                    Progress = (uint)player.MissionManager!.GetMissionProgress(mission)
                });

        SetData(proto);
    }
}
