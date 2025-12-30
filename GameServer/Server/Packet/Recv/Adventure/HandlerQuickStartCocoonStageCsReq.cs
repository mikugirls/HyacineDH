using EggLink.DanhengServer.GameServer.Server.Packet.Send.Adventure;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Scene;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Adventure;

[Opcode(CmdIds.QuickStartCocoonStageCsReq)]
public class HandlerQuickStartCocoonStageCsReq : Handler
{
    private static readonly Logger Logger = new("QuickStartCocoon");

    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = QuickStartCocoonStageCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var cocoonId = (int)req.CocoonId;
        var worldLevel = (int)req.WorldLevel;
        if (worldLevel <= 0) worldLevel = player.Data.WorldLevel;

        var count = (int)req.Count;
        if (count <= 0) count = (int)req.Wave;
        if (count <= 0) count = 1;

        var (retcode, battle) = await player.BattleManager!.StartCocoonStageWithRetcode(cocoonId, count, worldLevel);

        if (battle != null)
        {
            await connection.SendPacket(new PacketSceneEnterStageScRsp(battle));
            player.SceneInstance?.OnEnterStage();
            var rspWave = (int)req.Wave;
            if (rspWave <= 0) rspWave = count;
            if (rspWave <= 0) rspWave = 1;
            await connection.SendPacket(new PacketQuickStartCocoonStageScRsp(battle, cocoonId, rspWave));
            return;
        }

        Logger.Warn(
            $"QuickStartCocoon failed: uid={player.Uid}, cocoonId={cocoonId}, worldLevel={worldLevel}, wave={count}, stamina={player.Data.Stamina}, retcode={retcode}");
        await connection.SendPacket(new PacketQuickStartCocoonStageScRsp(retcode));
    }
}

