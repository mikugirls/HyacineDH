using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Activity;
using EggLink.DanhengServer.GameServer.Game.Activity.Activities;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Activity;

public class ActivityManager : BasePlayerManager
{
    public ActivityManager(PlayerInstance player) : base(player)
    {
        Data = DatabaseHelper.Instance!.GetInstanceOrCreateNew<ActivityData>(player.Uid);

        if (Data.TrialActivityData.CurTrialStageId != 0) TrialActivityInstance = new TrialActivityInstance(this);
    }

    #region Data

    public ActivityData Data { get; set; }

    #endregion

    #region Instance

    public TrialActivityInstance? TrialActivityInstance { get; set; }

    #endregion

    public List<ActivityScheduleData> ToProto()
    {
        var proto = new List<ActivityScheduleData>();
        var now = Extensions.GetUnixSec();
        var forceAlwaysOpen = !ConfigManager.Config.ServerOption.EnableMission;

        foreach (var activity in GameData.ActivityConfig.ScheduleData)
        {
            var begin = activity.BeginTime;
            var end = activity.EndTime;

            if (forceAlwaysOpen)
            {
                begin = 0;
                end = uint.MaxValue;
            }
            else
            {
                if (end > 0 && end < now) end = uint.MaxValue;
                if (begin > now) begin = 0;
            }

            proto.Add(new ActivityScheduleData
            {
                ActivityId = (uint)activity.ActivityId,
                BeginTime = begin,
                EndTime = end,
                PanelId = (uint)activity.PanelId
            });
        }

        return proto;
    }
}
