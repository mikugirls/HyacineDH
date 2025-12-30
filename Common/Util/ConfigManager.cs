using EggLink.DanhengServer.Configuration;
using EggLink.DanhengServer.Internationalization;
using Newtonsoft.Json;

namespace EggLink.DanhengServer.Util;

public static class ConfigManager
{
    public static readonly Logger Logger = new("ConfigManager");
    private static readonly string ConfigFileName = "Config.json";
    private static string HotfixFilePath => Path.Combine(Config.Path.ConfigPath, "Hotfix.json");
    public static ConfigContainer Config { get; private set; } = new();
    public static HotfixContainer Hotfix { get; private set; } = new();

    public static void LoadConfig()
    {
        LoadConfigData();
        LoadHotfixData();
        InitDirectories();
    }

    private static void LoadConfigData()
    {
        var configFilePath = ResolveConfigFilePath();
        Logger.Info($"Config path: {configFilePath}");

        var file = new FileInfo(configFilePath);
        if (!file.Exists)
        {
            Config = new ConfigContainer
            {
                MuipServer =
                {
                    AdminKey = Guid.NewGuid().ToString()
                },
                ServerOption =
                {
                    Language = UtilTools.GetCurrentLanguage()
                }
            };

            Logger.Info("Current Language is " + Config.ServerOption.Language);
            Logger.Info("Muipserver Admin key: " + Config.MuipServer.AdminKey);
            SaveData(Config, file.FullName);
        }

        using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            Config = JsonConvert.DeserializeObject<ConfigContainer>(json, new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            })!;
        }

        NormalizePaths(file.DirectoryName ?? Directory.GetCurrentDirectory());
        SaveData(Config, file.FullName);
    }

    private static void LoadHotfixData()
    {
        var file = new FileInfo(HotfixFilePath);

        // Generate all necessary versions
        var verList = new List<string>();
        var prefix = new List<string> { "CN", "OS" };
        foreach (var pre in prefix)
            if (GameConstants.GAME_VERSION[^1] == '5')
                for (var i = 1; i < 6; i++)
                    verList.Add(pre + GameConstants.GAME_VERSION + i);
            else
                verList.Add(pre + GameConstants.GAME_VERSION);

        if (!file.Exists)
        {
            Hotfix = new HotfixContainer();
            SaveData(Hotfix, HotfixFilePath);
            file.Refresh();
        }

        using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            Hotfix = JsonConvert.DeserializeObject<HotfixContainer>(json)!;
        }

        foreach (var version in verList)
            if (!Hotfix.HotfixData.TryGetValue(version, out _))
                Hotfix.HotfixData[version] = new DownloadUrlConfig();

        Logger.Info(I18NManager.Translate("Server.ServerInfo.CurrentVersion", GameConstants.GAME_VERSION));

        SaveData(Hotfix, HotfixFilePath);
    }

    private static void SaveData(object data, string path)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(stream);
        writer.Write(json);
    }

    private static string ResolveConfigFilePath()
    {
        var env = Environment.GetEnvironmentVariable("DANHENG_CONFIG");
        if (!string.IsNullOrWhiteSpace(env))
            return Path.GetFullPath(env);

        // Prefer config next to the solution file if this repo uses a nested solution directory (common in this workspace).
        var cwd = Directory.GetCurrentDirectory();

        var nestedSln = Path.Combine(cwd, "HyacineDH", "DanhengServer.sln");
        if (File.Exists(nestedSln))
            return Path.Combine(cwd, "HyacineDH", ConfigFileName);

        var localSln = Path.Combine(cwd, "DanhengServer.sln");
        if (File.Exists(localSln))
            return Path.Combine(cwd, ConfigFileName);

        // Fallback: if there's a Config.json in current dir, use it; otherwise create one here.
        return Path.Combine(cwd, ConfigFileName);
    }

    private static void NormalizePaths(string configRootDir)
    {
        foreach (var property in Config.Path.GetType().GetProperties())
        {
            if (property.PropertyType != typeof(string)) continue;

            var value = property.GetValue(Config.Path) as string;
            if (string.IsNullOrWhiteSpace(value)) continue;

            var normalized = value.Replace('/', Path.DirectorySeparatorChar);
            if (!Path.IsPathRooted(normalized))
                normalized = Path.GetFullPath(Path.Combine(configRootDir, normalized));

            property.SetValue(Config.Path, normalized);
        }
    }

    public static void InitDirectories()
    {
        foreach (var property in Config.Path.GetType().GetProperties())
        {
            var dir = property.GetValue(Config.Path)?.ToString();

            if (!string.IsNullOrEmpty(dir))
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
        }
    }
}
