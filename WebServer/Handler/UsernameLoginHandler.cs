using EggLink.DanhengServer.Database.Account;
using EggLink.DanhengServer.Util;
using EggLink.DanhengServer.WebServer.Objects;
using EggLink.DanhengServer.WebServer.Util;
using Microsoft.AspNetCore.Mvc;
using static EggLink.DanhengServer.WebServer.Objects.LoginResJson;

namespace EggLink.DanhengServer.WebServer.Handler;

public class UsernameLoginHandler
{
    private static readonly Logger Logger = new("UsernameLogin");

    public JsonResult Handle(string account, string password, bool isCrypto, string deviceId)
    {
        LoginResJson res = new();

        var rawAccount = account ?? "";
        var identity = LoginIdentityResolver.Resolve(account, password, isCrypto, deviceId);
        account = identity.Username;

        if (identity.EncryptedCredentials)
        {
            Logger.Info(
                $"Encrypted credentials detected (is_crypto={isCrypto}), binding by {identity.Reason}: username={account}, device_id={(string.IsNullOrWhiteSpace(deviceId) ? "<missing>" : deviceId)}");
        }

        Logger.Debug($"Login request: raw_account={rawAccount}, resolved={account}, isCrypto={isCrypto}");
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
                return new JsonResult(new LoginResJson { message = "Account not found", retcode = -201 });
            }
        }

        if (accountData != null)
        {
            res.message = "OK";
            var email = $"{accountData.Username}@egglink.me";
            res.data = new VerifyData(accountData.Uid.ToString(), email, accountData.GenerateDispatchToken());
            res.data.account.name = accountData.Username ?? "";
            Logger.Debug($"Login ok: username={accountData.Username}, uid={accountData.Uid}");
        }

        return new JsonResult(res);
    }
}

