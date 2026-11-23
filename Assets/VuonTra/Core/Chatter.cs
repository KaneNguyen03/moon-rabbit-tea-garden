using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Chatter
    {
        public const int TradeLines = 4, AnyLines = 4, PhaseLines = 2;
        public const string BoostKey = "chatter.boost";
        public const string DressedKey = "chatter.dressed";
        public const float Seconds = 2.5f;
        public const float WaitLeast = 40f, WaitMost = 70f;

        static readonly RabbitTrade[] Trades = { RabbitTrade.Brewer, RabbitTrade.Baker, RabbitTrade.Gatherer, RabbitTrade.Stargazer };
        static readonly DayPhase[] Phases = { DayPhase.Dusk, DayPhase.Night, DayPhase.Dawn, DayPhase.Day };
        static readonly Season[] Festivals = { Season.MidAutumn, Season.Tet, Season.Christmas };

        public static float Wait(double roll) => WaitLeast + (WaitMost - WaitLeast) * (float)Math.Max(0d, Math.Min(1d, roll));

        public static bool IsTradeLine(string key) => key != null && key.StartsWith("chatter.trade.", StringComparison.Ordinal);

        public static List<string> KeysFor(RabbitTrade trade, bool resting, DayPhase phase, Season season, bool boosted, bool dressed)
        {
            var keys = new List<string>(16);
            if (!resting && trade != RabbitTrade.None)
                for (int i = 0; i < TradeLines; i++) keys.Add("chatter.trade." + trade + "." + i);
            for (int i = 0; i < AnyLines; i++) keys.Add("chatter.any." + i);
            for (int i = 0; i < PhaseLines; i++) keys.Add("chatter.phase." + phase + "." + i);
            if (resting) return keys;
            if (season != Season.None) keys.Add("chatter.season." + season);
            if (boosted) keys.Add(BoostKey);
            if (dressed) keys.Add(DressedKey);
            return keys;
        }

        public static string Pick(IReadOnlyList<string> keys, double roll)
        {
            if (keys == null || keys.Count == 0) return null;
            int i = (int)Math.Floor(Math.Max(0d, roll) * keys.Count);
            return keys[Math.Min(keys.Count - 1, i)];
        }

        public static int BoosterOf(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (index < 0 || index >= defs.Count || defs[index].Kind != StationKind.Brew) return -1;
            string id = defs[index].Id;
            for (int i = 0; i < defs.Count && i < s.Levels.Length; i++)
                if (defs[i].Kind == StationKind.Ingredient && s.Levels[i] > 0 && defs[i].Boosts(id)) return i;
            return -1;
        }

        public static IEnumerable<string> AllKeys
        {
            get
            {
                foreach (RabbitTrade t in Trades)
                    for (int i = 0; i < TradeLines; i++) yield return "chatter.trade." + t + "." + i;
                for (int i = 0; i < AnyLines; i++) yield return "chatter.any." + i;
                foreach (DayPhase p in Phases)
                    for (int i = 0; i < PhaseLines; i++) yield return "chatter.phase." + p + "." + i;
                foreach (Season s in Festivals) yield return "chatter.season." + s;
                yield return BoostKey;
                yield return DressedKey;
            }
        }
    }
}
