using EggLink.DanhengServer.Util;
using SqlSugar;

namespace EggLink.DanhengServer.Database.Account;

[SugarTable("Account")]
public class AccountData : BaseDatabaseDataHelper
{
    public string? Username { get; set; }

    [SugarColumn(IsNullable = true)] public string? ComboToken { get; set; }

    [SugarColumn(IsNullable = true)] public string? DispatchToken { get; set; }

    [SugarColumn(IsNullable = true)]
    public string? Permissions { get; set; } // type: permission1,permission2,permission3...

    public static string NormalizeUsername(string username)
    {
        username = username.Trim();
        if (string.IsNullOrEmpty(username)) return "";
        var atIndex = username.IndexOf('@');
        if (atIndex > 0) username = username[..atIndex];
        return username.Trim();
    }

    public static AccountData? GetAccountByUserName(string username)
    {
        username = NormalizeUsername(username);
        if (string.IsNullOrEmpty(username)) return null;

        // Prefer in-memory cache (UidInstanceMap) so token updates are consistent across handlers.
        foreach (var instances in DatabaseHelper.UidInstanceMap.Values)
        {
            var account = instances.OfType<AccountData>().FirstOrDefault();
            if (account?.Username == null) continue;
            if (string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase)) return account;
        }

        // Fallback to DB query (e.g. before cache is initialized).
        return DatabaseHelper.GetAllInstance<AccountData>()
            ?.FirstOrDefault(a =>
                !string.IsNullOrEmpty(a.Username) &&
                string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public static AccountData? GetAccountByUid(int uid)
    {
        var result = DatabaseHelper.Instance?.GetInstance<AccountData>(uid);
        return result;
    }

    public string GenerateDispatchToken()
    {
        DispatchToken = Crypto.CreateSessionKey(Uid.ToString());
        DatabaseHelper.SaveDatabaseType(this);
        return DispatchToken;
    }

    public string GenerateComboToken()
    {
        ComboToken = Crypto.CreateSessionKey(Uid.ToString());
        DatabaseHelper.SaveDatabaseType(this);
        return ComboToken;
    }
}
