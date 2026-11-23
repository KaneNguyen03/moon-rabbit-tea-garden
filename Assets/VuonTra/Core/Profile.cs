using System;

namespace VuonTra.Core
{
    public sealed class PlayerProfile
    {
        public string GardenName = "";
        public long AvatarRabbitId;
        public string Code = "";
        public bool LinkRewarded;
    }

    public static class Profile
    {
        public const int MaxNameLength = 16;
        public const string CodePrefix = "THO-";
        public const int CodeLength = 6;
        public const string CodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

        public static string NewCode()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            var chars = new char[CodeLength];
            for (int i = 0; i < CodeLength; i++)
                chars[i] = CodeAlphabet[(bytes[i] ^ bytes[i + 8]) % CodeAlphabet.Length];
            return CodePrefix + new string(chars);
        }

        public static bool IsCode(string code)
        {
            if (code == null || code.Length != CodePrefix.Length + CodeLength || !code.StartsWith(CodePrefix, StringComparison.Ordinal)) return false;
            for (int i = CodePrefix.Length; i < code.Length; i++)
                if (CodeAlphabet.IndexOf(code[i]) < 0) return false;
            return true;
        }

        public static string CleanName(string raw)
        {
            if (raw == null) return null;
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw) sb.Append(char.IsControl(c) ? ' ' : c);
            string name = sb.ToString().Trim();
            while (name.Contains("  ")) name = name.Replace("  ", " ");
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength).TrimEnd();
            return name.Length == 0 ? null : name;
        }

        public static bool Rename(GameState s, string raw)
        {
            string name = CleanName(raw);
            if (name == null) return false;
            s.Profile.GardenName = name;
            return true;
        }

        public const int LinkRewardClovers = 3;

        public static bool OnLinked(GameState s)
        {
            if (s.Profile.LinkRewarded) return false;
            s.Profile.LinkRewarded = true;
            s.Clovers += LinkRewardClovers;
            return true;
        }

        public static Rabbit Avatar(GameState s)
        {
            Rabbit chosen = s.Profile.AvatarRabbitId > 0 ? Rabbits.Find(s, s.Profile.AvatarRabbitId) : null;
            if (chosen != null) return chosen;
            return s.Rabbits.Count > 0 ? s.Rabbits[0] : null;
        }

        public static Rabbit NextAvatar(GameState s)
        {
            Rabbit current = Avatar(s);
            if (current == null) return null;
            int at = s.Rabbits.IndexOf(current);
            Rabbit next = s.Rabbits[(at + 1) % s.Rabbits.Count];
            s.Profile.AvatarRabbitId = next.Id;
            return next;
        }
    }

    public enum AccountProvider
    {
        None,
        Apple,
        Google,
        GameCenter,
        PlayGames
    }

    public enum BackupState
    {
        LocalOnly,
        Linking,
        Linked,
        Failed
    }

    public interface IAccountService
    {
        AccountProvider Provider { get; }
        BackupState State { get; }
        bool CanLink { get; }
        string LinkedName { get; }
        void Link(Action<bool> onDone);
        void Unlink();
        event Action Changed;
    }

    public interface ICloudSave
    {
        bool Available { get; }
        int MaxBytes { get; }
        void Push(string json, Action<bool> onDone);
        void Pull(Action<string> onDone);
        void Delete(Action<bool> onDone);
    }

    public sealed class LocalOnlyCloudSave : ICloudSave
    {
        public bool Available => false;
        public int MaxBytes => 0;

        public void Push(string json, Action<bool> onDone) => onDone?.Invoke(false);

        public void Pull(Action<string> onDone) => onDone?.Invoke(null);

        public void Delete(Action<bool> onDone) => onDone?.Invoke(true);
    }

    public struct SaveSummary
    {
        public int GardenLevel;
        public int Rabbits;
        public int Kinds;
        public double PlaySeconds;
        public long LastSeenUnixSeconds;
        public string Code;

        public static SaveSummary Of(GameState s) => new SaveSummary
        {
            GardenLevel = Garden.Level(s), Rabbits = s.Rabbits.Count, Kinds = s.SeenKinds.Count, PlaySeconds = s.PlaySeconds,
            LastSeenUnixSeconds = s.LastSeenUnixSeconds, Code = s.Profile.Code
        };
    }

    public enum SaveChoice
    {
        Same,
        KeepLocal,
        KeepRemote,
        Ask
    }

    public static class SaveSync
    {
        public static SaveChoice Choose(SaveSummary local, SaveSummary remote)
        {
            bool sameTime = local.LastSeenUnixSeconds == remote.LastSeenUnixSeconds;
            if (sameTime && local.GardenLevel == remote.GardenLevel && local.Rabbits == remote.Rabbits) return SaveChoice.Same;
            bool localAhead = local.GardenLevel >= remote.GardenLevel && local.Rabbits >= remote.Rabbits && local.LastSeenUnixSeconds >= remote.LastSeenUnixSeconds;
            bool remoteAhead = remote.GardenLevel >= local.GardenLevel && remote.Rabbits >= local.Rabbits && remote.LastSeenUnixSeconds >= local.LastSeenUnixSeconds;
            if (localAhead && !remoteAhead) return SaveChoice.KeepLocal;
            if (remoteAhead && !localAhead) return SaveChoice.KeepRemote;
            return SaveChoice.Ask;
        }
    }

    public sealed class LocalOnlyAccount : IAccountService
    {
        public AccountProvider Provider => AccountProvider.None;
        public BackupState State => BackupState.LocalOnly;
        public bool CanLink => false;
        public string LinkedName => "";
        public event Action Changed { add { } remove { } }

        public void Link(Action<bool> onDone) => onDone?.Invoke(false);

        public void Unlink() { }
    }
}
