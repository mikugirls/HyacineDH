using System.Text;
using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Internationalization;
using EggLink.DanhengServer.Program.Program;
using EggLink.DanhengServer.Util;
using Newtonsoft.Json;

namespace EggLink.DanhengServer.Program.Handbook;

public static class HandbookGenerator
{
    public static void GenerateAll()
    {
        var config = ConfigManager.Config;
        var directory = new DirectoryInfo(config.Path.ResourcePath + "/TextMap");
        var handbook = new DirectoryInfo("GM Handbook");
        if (!handbook.Exists)
            handbook.Create();
        if (!directory.Exists)
            return;

        foreach (var langFile in directory.GetFiles())
        {
            if (langFile.Extension != ".json") continue;
            if (langFile.Name.StartsWith("TextMapMain", StringComparison.OrdinalIgnoreCase)) continue;
            var lang = langFile.Name.Replace("TextMap", "").Replace(".json", "");

            // Check if handbook needs to regenerate
            var handbookPath = $"GM Handbook/GM Handbook {lang}.txt";
            if (File.Exists(handbookPath))
            {
                var handbookInfo = new FileInfo(handbookPath);
                if (handbookInfo.LastWriteTime >= langFile.LastWriteTime)
                    continue; // Skip if handbook is newer than language file
            }

            Generate(lang);
        }

        Logger.GetByClassName()
            .Info(I18NManager.Translate("Server.ServerInfo.GeneratedItem", I18NManager.Translate("Word.Handbook")));
    }

    public static void Generate(string lang)
    {
        var config = ConfigManager.Config;
        var textMap = LoadMergedTextMap(lang);
        var fallbackTextMap = LoadMergedTextMap(config.ServerOption.FallbackLanguage);
        if (textMap == null || fallbackTextMap == null) return;

        var builder = new StringBuilder();
        builder.AppendLine("#Handbook generated in " + DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
        builder.AppendLine();
        builder.AppendLine("#Command");
        builder.AppendLine();
        GenerateCmd(builder, lang);

        builder.AppendLine();
        builder.AppendLine("#Avatar");
        builder.AppendLine();
        GenerateAvatar(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

        builder.AppendLine();
        builder.AppendLine("#Item");
        builder.AppendLine();
        GenerateItem(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

        builder.AppendLine();
        builder.AppendLine("#StageId");
        builder.AppendLine();
        GenerateStageId(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#MainMission");
        builder.AppendLine();
        GenerateMainMissionId(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#SubMission");
        builder.AppendLine();
        GenerateSubMissionId(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#RogueBuff");
        builder.AppendLine();
        GenerateRogueBuff(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

        builder.AppendLine();
        builder.AppendLine("#RogueMiracle");
        builder.AppendLine();
        GenerateRogueMiracleDisplay(builder, textMap, fallbackTextMap, lang == config.ServerOption.Language);

#if DEBUG
        builder.AppendLine();
        builder.AppendLine("#RogueDiceSurface");
        builder.AppendLine();
        GenerateRogueDiceSurfaceDisplay(builder, textMap, fallbackTextMap);

        builder.AppendLine();
        builder.AppendLine("#RogueDialogue");
        builder.AppendLine();
        GenerateRogueDialogueDisplay(builder, textMap, fallbackTextMap);
#endif

        builder.AppendLine();
        WriteToFile(lang, builder.ToString());
    }

    public static void GenerateCmd(StringBuilder builder, string lang)
    {
        foreach (var cmd in EntryPoint.CommandManager.CommandInfo)
        {
            builder.Append("\t" + cmd.Key);
            var desc = I18NManager.TranslateAsCertainLang(lang, cmd.Value.Description).Replace("\n", "\n\t\t");
            builder.AppendLine(": " + desc);
        }
    }

    public static void GenerateItem(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback, bool setName)
    {
        foreach (var item in GameData.ItemConfigData.Values)
        {
            var key = ToTextMapKey(item.ItemName.Hash);
            var name = map.TryGetValue(key, out var value)
                ? value
                : fallback.TryGetValue(key, out value)
                    ? value
                    : $"[{key}]";
            builder.AppendLine(item.ID + ": " + name);

            if (setName && name != $"[{key}]") item.Name = name;
        }
    }

    public static void GenerateAvatar(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback, bool setName)
    {
        foreach (var avatar in GameData.AvatarConfigData.Values)
        {
            var key = ToTextMapKey(avatar.AvatarName.Hash);
            var name = map.TryGetValue(key, out var value)
                ? value
                : fallback.TryGetValue(key, out value)
                    ? value
                    : $"[{key}]";
            builder.AppendLine(avatar.AvatarID + ": " + name);

            if (setName && name != $"[{key}]") avatar.Name = name;
        }
    }

    public static void GenerateMainMissionId(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback)
    {
        foreach (var mission in GameData.MainMissionData.Values)
        {
            var key = ToTextMapKey(mission.Name.Hash);
            var name = map.TryGetValue(key, out var value)
                ? value
                : fallback.TryGetValue(key, out value)
                    ? value
                    : $"[{key}]";
            builder.AppendLine(mission.MainMissionID + ": " + name);
        }
    }

    public static void GenerateSubMissionId(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback)
    {
        foreach (var mission in GameData.SubMissionData.Values)
        {
            var key = ToTextMapKey(mission.TargetText.Hash);
            var name = map.TryGetValue(key, out var value)
                ? value
                : fallback.TryGetValue(key, out value)
                    ? value
                    : $"[{key}]";
            builder.AppendLine(mission.SubMissionID + ": " + name);
        }
    }

    public static void GenerateStageId(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback)
    {
        foreach (var stage in GameData.StageConfigData.Values)
        {
            var key = ToTextMapKey(stage.StageName.Hash);
            var name = map.TryGetValue(key, out var value)
                ? value
                : fallback.TryGetValue(key, out value)
                    ? value
                    : $"[{key}]";
            builder.AppendLine(stage.StageID + ": " + name);
        }
    }

    public static void GenerateRogueBuff(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback, bool setName)
    {
        foreach (var buff in GameData.RogueMazeBuffData)
        {
            var key = ToTextMapKey(buff.Value.BuffName.Hash);
            var name = map.TryGetValue(key, out var value)
                ? value
                : fallback.TryGetValue(key, out value)
                    ? value
                    : $"[{key}]";
            builder.AppendLine(buff.Key + ": " + name + " --- Level:" + buff.Value.Lv);

            if (setName && name != $"[{key}]") buff.Value.Name = name;
        }
    }

    public static void GenerateRogueMiracleDisplay(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback, bool setName)
    {
        foreach (var display in GameData.RogueMiracleData.Values)
        {
            var key = ToTextMapKey(display.MiracleName.Hash);
            var name = map.TryGetValue(key, out var value)
                ? value
                : fallback.TryGetValue(key, out value)
                    ? value
                    : $"[{key}]";
            builder.AppendLine(display.MiracleID + ": " + name);

            if (setName && name != $"[{key}]") display.Name = name;
        }
    }

    public static string GetNameFromTextMap(long key, Dictionary<ulong, string> map, Dictionary<ulong, string> fallback)
    {
        var normalizedKey = ToTextMapKey(key);
        if (map.TryGetValue(normalizedKey, out var value)) return value;
        if (fallback.TryGetValue(normalizedKey, out value)) return value;
        return $"[{normalizedKey}]";
    }

    public static void WriteToFile(string lang, string content)
    {
        File.WriteAllText($"GM Handbook/GM Handbook {lang}.txt", content);
    }

    private static ulong ToTextMapKey(long key)
    {
        return unchecked((ulong)key);
    }

    private static Dictionary<ulong, string>? LoadMergedTextMap(string lang)
    {
        var config = ConfigManager.Config;
        var basePath = Path.Combine(config.Path.ResourcePath, "TextMap");

        var primaryPath = Path.Combine(basePath, $"TextMap{lang}.json");
        var mainPath = Path.Combine(basePath, $"TextMapMain{lang}.json");

        var primary = LoadTextMapFile(primaryPath);
        var main = LoadTextMapFile(mainPath);

        // Some builds only ship one of them; merge to maximize hash coverage.
        if (primary == null && main == null)
        {
            Logger.GetByClassName().Error(I18NManager.Translate("Server.ServerInfo.FailedToReadItem", primaryPath,
                I18NManager.Translate("Word.NotFound")));
            return null;
        }

        var merged = primary ?? new Dictionary<ulong, string>();
        if (main != null)
            foreach (var (k, v) in main)
                merged[k] = v;

        return merged;
    }

    private static Dictionary<ulong, string>? LoadTextMapFile(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var map = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(File.ReadAllText(path));
            if (map == null)
            {
                Logger.GetByClassName().Error(I18NManager.Translate("Server.ServerInfo.FailedToReadItem", path,
                    I18NManager.Translate("Word.Error")));
            }
            return map;
        }
        catch (Exception e)
        {
            Logger.GetByClassName().Error(I18NManager.Translate("Server.ServerInfo.FailedToReadItem", path,
                I18NManager.Translate("Word.Error")), e);
            return null;
        }
    }

#if DEBUG
    public static void GenerateRogueDiceSurfaceDisplay(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback)
    {
        foreach (var display in GameData.RogueNousDiceSurfaceData.Values)
        {
            var nameKey = ToTextMapKey(display.SurfaceName.Hash);
            var name = map.TryGetValue(nameKey, out var value)
                ? value
                : fallback.TryGetValue(nameKey, out value)
                    ? value
                    : $"[{nameKey}]";
            var descKey = ToTextMapKey(display.SurfaceDesc.Hash);
            var desc = map.TryGetValue(descKey, out var c) ? c : $"[{descKey}]";
            builder.AppendLine(display.SurfaceID + ": " + name + "\n" + "Desc: " + desc);
        }
    }

    public static void GenerateRogueDialogueDisplay(StringBuilder builder, Dictionary<ulong, string> map,
        Dictionary<ulong, string> fallback)
    {
        foreach (var npc in GameData.RogueNPCData.Values.Where(x => x.RogueNpcConfig != null))
        {
            builder.AppendLine("NpcId: " + npc.RogueNPCID);
            foreach (var dialogue in npc.RogueNpcConfig?.DialogueList ?? [])
            {
                var eventNameHash =
                    GameData.RogueTalkNameConfigData.GetValueOrDefault(dialogue.TalkNameID)?.Name.Hash ??
                    -1;
                var eventName = GetNameFromTextMap(eventNameHash, map, fallback);
                builder.AppendLine($"  Progress: {dialogue.DialogueProgress} | {eventName}");
                builder.AppendLine($"  Type: {npc.RogueNpcConfig!.DialogueType}");
                builder.AppendLine("  Options: ");

                foreach (var option in dialogue.OptionInfo?.OptionList ?? [])
                {
                    var display = GameData.RogueDialogueOptionDisplayData.GetValueOrDefault(option.DisplayID);
                    if (display == null) continue;

                    var optionName = GetNameFromTextMap(display.OptionTitle.Hash, map, fallback);
                    var optionDesc = GetNameFromTextMap(display.OptionDesc.Hash, map, fallback);
                    builder.AppendLine($"    Option: {option.OptionID} - {optionName}");
                    builder.AppendLine($"      {optionDesc}".Replace("#2", option.DescValue.ToString())
                        .Replace("#5", option.DescValue2.ToString()).Replace("#6", option.DescValue3.ToString())
                        .Replace("#7", option.DescValue4.ToString()));
                    if (option.DynamicMap.Count == 0) continue;

                    builder.AppendLine("      Dynamic Value:");
                    foreach (var value in option.DynamicMap)
                    {
                        var dynamic = GameData.RogueDialogueDynamicDisplayData.GetValueOrDefault(value.Value.DisplayID);
                        if (dynamic == null) continue;
                        var dynamicName = GetNameFromTextMap(dynamic.ContentText.Hash, map, fallback);
                        builder.AppendLine($"        Dynamic Id: {value.Key} | {dynamicName}");
                    }
                }
            }
        }
    }
#endif
}
