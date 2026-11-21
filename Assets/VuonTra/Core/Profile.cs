using System;

namespace VuonTra.Core
{
    /// <summary>Who the player is to the game: the garden's name, the rabbit that stands for it, and a code to quote.</summary>
    public sealed class PlayerProfile
    {
        /// <summary>Empty until the player names the garden; the screen shows a stand-in then.</summary>
        public string GardenName = "";
        /// <summary>The rabbit whose picture stands for the player, or 0 for "the first one".</summary>
        public long AvatarRabbitId;
        /// <summary>"THO-" and six characters, given once and never changed; what a player quotes to support or a friend.</summary>
        public string Code = "";
    }

    public static class Profile
    {
        public const int MaxNameLength = 16;
        public const string CodePrefix = "THO-";
        public const int CodeLength = 6;
        /// <summary>No 0, O, 1 or I: a code read aloud or typed from a photo is never ambiguous.</summary>
        public const string CodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

        /// <summary>A fresh code, from the same randomness a GUID has.</summary>
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

        /// <summary>Trims, collapses spaces and caps the length, like a rabbit's name. Null when nothing is left.</summary>
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

        /// <summary>Names the garden. An empty or all-space name is refused and the old one stays.</summary>
        public static bool Rename(GameState s, string raw)
        {
            string name = CleanName(raw);
            if (name == null) return false;
            s.Profile.GardenName = name;
            return true;
        }

        /// <summary>The rabbit that stands for the player: the chosen one if it is still here, else the first, else null.</summary>
        public static Rabbit Avatar(GameState s)
        {
            Rabbit chosen = s.Profile.AvatarRabbitId > 0 ? Rabbits.Find(s, s.Profile.AvatarRabbitId) : null;
            if (chosen != null) return chosen;
            return s.Rabbits.Count > 0 ? s.Rabbits[0] : null;
        }

        /// <summary>Chooses the rabbit after the current avatar, round and round; null when there are no rabbits.</summary>
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

    /// <summary>Who signs the player in. None while the garden lives only on this device.</summary>
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
        /// <summary>The save is on this device and nowhere else.</summary>
        LocalOnly,
        Linking,
        Linked,
        Failed
    }

    /// <summary>
    /// The seam the profile screen talks to. The game ships with the local-only service; a store's sign-in
    /// (#39, #40) and the cloud save core (#38) plug in here without the screen changing.
    /// </summary>
    public interface IAccountService
    {
        AccountProvider Provider { get; }
        BackupState State { get; }
        /// <summary>True when a link button should do something; false while no provider is built in.</summary>
        bool CanLink { get; }
        /// <summary>The name the provider knows the player by, or empty.</summary>
        string LinkedName { get; }
        /// <summary>Starts a link; <paramref name="onDone"/> gets whether it worked, maybe much later.</summary>
        void Link(Action<bool> onDone);
        void Unlink();
        event Action Changed;
    }

    /// <summary>No provider at all: the honest answer until a store sign-in is wired in.</summary>
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
