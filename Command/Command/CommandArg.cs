using EggLink.DanhengServer.GameServer.Server;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.Command.Command;

public class CommandArg
{
    public CommandArg(string raw, ICommandSender sender, Connection? con = null)
    {
        Raw = raw;
        Sender = sender;
        var args = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.IsNullOrEmpty(arg)) continue;

            // Support flags split by whitespace: `r 6`, `-r 6`, `l 80`, etc.
            if (i + 1 < args.Length && IsSingleLetterFlag(arg))
            {
                var key = arg.StartsWith('-') ? arg[1].ToString() : arg[0].ToString();
                var value = args[i + 1];

                if (!CharacterArgs.ContainsKey(key))
                    CharacterArgs.Add(key, value);

                Args.Add(arg);

                // Preserve previous behavior where numeric values are still part of BasicArgs/Args.
                if (IsBasicArg(value)) BasicArgs.Add(value);
                Args.Add(value);

                i++; // consume value
                continue;
            }

            if (IsBasicArg(arg))
            {
                BasicArgs.Add(arg);
                Args.Add(arg);
                continue;
            }

            try
            {
                CharacterArgs.Add(arg[..1], arg[1..]);
                Args.Add(arg);
            }
            catch
            {
                BasicArgs.Add(arg);
                Args.Add(arg);
            }
        }

        if (con != null) Target = con;

        CharacterArgs.TryGetValue("@", out var target);
        if (target == null) return;
        if (DanhengListener.Connections.Values.ToList()
                .Find(item => (item as Connection)?.Player?.Uid.ToString() == target) is Connection connection)
            Target = connection;
    }

    public string Raw { get; }
    public List<string> Args { get; } = [];
    public List<string> BasicArgs { get; } = [];
    public Dictionary<string, string> CharacterArgs { get; } = [];
    public Connection? Target { get; set; }
    public ICommandSender Sender { get; }

    public int GetInt(int index)
    {
        if (BasicArgs.Count <= index) return 0;
        _ = int.TryParse(BasicArgs[index], out var res);
        return res;
    }

    public async ValueTask SendMsg(string msg)
    {
        await Sender.SendMsg(msg);
    }

    private static bool IsBasicArg(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return false;
        var character = arg[0];
        return int.TryParse(character.ToString(), out _) || character == '-';
    }

    private static bool IsSingleLetterFlag(string arg)
    {
        if (arg.Length == 1)
            return !IsBasicArg(arg);
        if (arg.Length == 2 && arg[0] == '-' && !IsBasicArg(arg[1].ToString()))
            return true;
        return false;
    }

    public override string ToString()
    {
        return $"BasicArg: {BasicArgs.ToArrayString()}. CharacterArg: {CharacterArgs.ToJsonString()}.";
    }
}
