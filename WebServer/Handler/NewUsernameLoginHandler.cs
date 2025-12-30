using EggLink.DanhengServer.Database.Account;
using EggLink.DanhengServer.Util;
using EggLink.DanhengServer.WebServer.Objects;
using EggLink.DanhengServer.WebServer.Util;
using Microsoft.AspNetCore.Mvc;
using static EggLink.DanhengServer.WebServer.Objects.NewLoginResJson;

namespace EggLink.DanhengServer.WebServer.Handler;

public class NewUsernameLoginHandler
{
    private static readonly Logger Logger = new("NewUsernameLogin");

    public JsonResult Handle(string account, string password, string deviceId)
    {
        NewLoginResJson res = new();

        var rawAccount = account ?? "";
        var identity = LoginIdentityResolver.Resolve(account, password, isCrypto: null, deviceId);
        account = identity.Username;

        if (identity.EncryptedCredentials)
        {
            Logger.Info(
                $"Encrypted credentials detected, binding by {identity.Reason}: username={account}, device_id={(string.IsNullOrWhiteSpace(deviceId) ? "<missing>" : deviceId)}");
        }

        Logger.Debug($"Login request: raw_account={rawAccount}, resolved={account}");
        var accountData = AccountData.GetAccountByUserName(account);

        if (accountData == null)
        {
            if (ConfigManager.Config.ServerOption.AutoCreateUser)
            {
                Logger.Info($"Account not found, auto-creating: username={account}");
                AccountHelper.CreateAccount(account, 0);
                accountData = AccountData.GetAccountByUserName(account);
            }
            else
            {
                Logger.Warn($"Account not found: username={account}");
                return new JsonResult(new NewLoginResJson { message = "Account not found", retcode = -201 });
            }
        }

        if (accountData != null)
        {
            res.message = "OK";
            res.data = new VerifyData(accountData.Uid.ToString(), accountData.Username + "@egglink.me",
                accountData.GenerateDispatchToken());
            res.data.user_info.account_name = accountData.Username ?? "";
            Logger.Debug($"Login ok: username={accountData.Username}, uid={accountData.Uid}");
        }

        return new JsonResult(res);
    }
}

