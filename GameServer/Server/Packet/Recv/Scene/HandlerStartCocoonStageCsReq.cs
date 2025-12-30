using EggLink.DanhengServer.GameServer.Server.Packet.Send.Scene;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.StartCocoonStageCsReq)]
public class HandlerStartCocoonStageCsReq : Handler
{
    private static readonly Logger Logger = new("StartCocoon");

    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = StartCocoonStageCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var (retcode, battle) =
            await player.BattleManager!.StartCocoonStageWithRetcode((int)req.CocoonId, (int)req.Wave,
                (int)req.WorldLevel);
        player.SceneInstance?.OnEnterStage();

        if (battle != null)
        {
            await connection.SendPacket(new PacketStartCocoonStageScRsp(battle, (int)req.CocoonId, (int)req.Wave));
            return;
        }

        Logger.Warn(
            $"StartCocoon failed: uid={player.Uid}, cocoonId={(int)req.CocoonId}, worldLevel={(int)req.WorldLevel}, wave={(int)req.Wave}, stamina={player.Data.Stamina}, retcode={retcode}");
        await connection.SendPacket(new PacketStartCocoonStageScRsp(retcode));
    }
}

