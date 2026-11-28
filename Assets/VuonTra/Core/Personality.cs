using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum Personality
    {
        Sleepy,
        Curious,
        SweetTooth,
        Shy,
        Singer,
        Tidy,
        Dreamy,
        Hungry
    }

    public struct Liking
    {
        public string Kind;
        public Personality Personality;
        public string Favourite;
    }

    public sealed class PersonalityBook
    {
        readonly Dictionary<string, Liking> _byKind = new Dictionary<string, Liking>();
        readonly List<string> _repeated = new List<string>();

        public PersonalityBook(IEnumerable<Liking> likings)
        {
            if (likings == null) return;
            foreach (Liking like in likings)
            {
                if (string.IsNullOrEmpty(like.Kind)) continue;
                if (_byKind.ContainsKey(like.Kind)) _repeated.Add(like.Kind);
                else _byKind[like.Kind] = like;
            }
        }

        public static readonly PersonalityBook Empty = new PersonalityBook(null);

        public int Count => _byKind.Count;

        public bool TryGet(string kindId, out Liking like)
        {
            if (kindId != null && _byKind.TryGetValue(kindId, out like)) return true;
            like = default;
            return false;
        }

        public bool LovesStation(IReadOnlyList<StationDef> defs, Rabbit r)
        {
            if (r == null || defs == null || r.Station < 0 || r.Station >= defs.Count) return false;
            return TryGet(Rabbits.KindOf(r), out Liking like) && like.Favourite == defs[r.Station].Id;
        }

        public List<string> Problems(IEnumerable<RabbitKind> kinds, IEnumerable<StationDef> defs)
        {
            var brews = new HashSet<string>();
            if (defs != null)
                foreach (StationDef d in defs)
                    if (d.Kind == StationKind.Brew) brews.Add(d.Id);
            var known = new HashSet<string>();
            var problems = new List<string>();
            foreach (string kind in _repeated) problems.Add(kind + " has more than one personality");
            foreach (RabbitKind kind in kinds)
            {
                known.Add(kind.Id);
                if (!_byKind.TryGetValue(kind.Id, out Liking like)) { problems.Add(kind.Id + " has no personality"); continue; }
                if (!Enum.IsDefined(typeof(Personality), like.Personality)) problems.Add(kind.Id + " has an unknown personality " + (int)like.Personality);
                if (!brews.Contains(like.Favourite ?? "")) problems.Add(kind.Id + " loves " + (like.Favourite ?? "nothing") + ", which is not a brewing station");
            }
            foreach (string kind in _byKind.Keys)
                if (!known.Contains(kind)) problems.Add(kind + " is not a rabbit kind");
            return problems;
        }
    }

    public static class Personalities
    {
        public const int CommonLines = 3;
        public const int FavouritePoints = 2;
        public const int OwnLineFromTier = 2;

        public static IEnumerable<Personality> All => (Personality[])Enum.GetValues(typeof(Personality));

        public static string NameKey(Personality p) => "temper." + p;

        public static string CommonKey(Personality p, int i) => "chatter.temper." + p + "." + i;

        public static string HeartKey(Personality p) => "chatter.temper." + p + ".heart";

        public static string KindKey(string kindId) => "chatter.kind." + kindId;

        public static bool HasOwnLine(RabbitKind kind) => kind != null && kind.Tier >= OwnLineFromTier;

        public static bool HasOwnLine(string kindId) => HasOwnLine(RabbitKinds.Find(kindId));

        public static void AddLines(List<string> keys, Liking like, int hearts)
        {
            if (keys == null || string.IsNullOrEmpty(like.Kind)) return;
            for (int i = 0; i < CommonLines; i++) keys.Add(CommonKey(like.Personality, i));
            if (hearts >= Rabbits.LineHearts) keys.Add(HeartKey(like.Personality));
            if (HasOwnLine(like.Kind)) keys.Add(KindKey(like.Kind));
        }

        public static IEnumerable<string> AllKeys
        {
            get
            {
                foreach (Personality p in All)
                {
                    for (int i = 0; i < CommonLines; i++) yield return CommonKey(p, i);
                    yield return HeartKey(p);
                }
                foreach (RabbitKind kind in RabbitKinds.All)
                    if (HasOwnLine(kind)) yield return KindKey(kind.Id);
            }
        }

        public static PetResult Pet(IReadOnlyList<StationDef> defs, PersonalityBook book, GameState s, Rabbit r, long day, bool wake = false)
        {
            PetResult result = Rabbits.Pet(s, r, day, wake);
            if (!result.FirstToday || book == null || !book.LovesStation(defs, r)) return result;
            BondStep extra = Rabbits.AddBond(s, r, FavouritePoints);
            result.LineUnlocked |= extra.LineUnlocked;
            result.GiftGiven |= extra.GiftGiven;
            result.TenHearts |= extra.TenHearts;
            return result;
        }
    }
}
