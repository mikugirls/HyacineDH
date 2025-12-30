using EggLink.DanhengServer.GameServer.Server.Packet.Send.Scene;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.GetSceneMapInfoCsReq)]
public class HandlerGetSceneMapInfoCsReq : Handler
{
    private static readonly Logger Logger = new("GetSceneMapInfo");

    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetSceneMapInfoCsReq.Parser.ParseFrom(data);
        Logger.Debug(
            $"Recv: floorIds=[{string.Join(",", req.FloorIdList)}] contentId={req.ContentId} entryStoryLineId={req.EntryStoryLineId}");
        await connection.SendPacket(new PacketGetSceneMapInfoScRsp(req, connection.Player!));
        Logger.Debug("Send: GetSceneMapInfoScRsp");
    }
}
