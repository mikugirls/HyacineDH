using Newtonsoft.Json;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("CocoonConfig.json")]
public class CocoonConfigExcel : ExcelResource
{
    public int ID { get; set; }
    public int MappingInfoID { get; set; }
    public int WorldLevel { get; set; }
    public int PropID { get; set; }
    public int StaminaCost { get; set; }

    [JsonProperty("MaxChallengeCnt")]
    public int MaxChallengeCnt { get; set; }

    [JsonProperty("MaxWave")]
    public int MaxWave { get; set; }

    public List<int> StageIDList { get; set; } = [];
    public List<int> DropList { get; set; } = [];

    public override int GetId()
    {
        return ID * 100 + WorldLevel;
    }

    public override void Loaded()
    {
        if (MaxWave <= 0) MaxWave = MaxChallengeCnt;
        if (MaxWave <= 0) MaxWave = StageIDList.Count;
        GameData.CocoonConfigData.Add(GetId(), this);
    }
}

