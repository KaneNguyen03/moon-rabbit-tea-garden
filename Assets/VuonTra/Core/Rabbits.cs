using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class Rabbit
    {
        public long Id;
        public int Tier;
        public string Kind;
        public bool FirstOfKind;
        public string Name;
        public int Level = 1;
        public int Station = -1;
        public int Affection;
        public long PetDay = -1;
        public int Dye = -1;
    }

    public struct PetResult
    {
        public bool Found;
        public bool FirstToday;
        public double Energy;
        public bool LineUnlocked;
        public bool GiftGiven;
        public bool TenHearts;
    }

    public struct BondStep
    {
        public bool LineUnlocked;
        public bool GiftGiven;
        public bool TenHearts;
    }

    public static class Rabbits
    {
        public const int TierCount = Rarity.Count;
        public const int SummonCost = 1;
        public const int MergeCount = 3;

        public static readonly double[] Multipliers = { 1.1, 1.25, 1.5, 2, 3, 5 };
        public static readonly int[] LevelCaps = { 10, 20, 30, 40, 50, 60 };
        public const double LevelBonus = 0.05;

        static int ClampTier(int tier) => Math.Max(0, Math.Min(TierCount - 1, tier));

        public static double Multiplier(GameState s, Rabbit r)
        {
            double fromLevel = Multipliers[ClampTier(r.Tier)] * (1d + LevelBonus * (Math.Max(1, r.Level) - 1));
            return fromLevel * (1d + Items.StationBonus(s, r.Id)) * HeartMultiplier(r);
        }

        public const double TradeBonus = 1.5;

        public static RabbitTrade TradeOf(Rabbit r) => RabbitKinds.Find(KindOf(r)).Trade;

        public static bool InTrade(IReadOnlyList<StationDef> defs, Rabbit r) => r.Station >= 0 && Suits(defs, r, r.Station);

        public static bool Suits(IReadOnlyList<StationDef> defs, Rabbit r, int station)
        {
            if (defs == null || station < 0 || station >= defs.Count) return false;
            RabbitTrade trade = defs[station].Trade;
            return trade != RabbitTrade.None && trade == TradeOf(r);
        }

        public static List<int> SuitedStations(IReadOnlyList<StationDef> defs, GameState s, Rabbit r)
        {
            var list = new List<int>();
            for (int i = 0; i < defs.Count; i++)
                if (s.Levels[i] > 0 && Suits(defs, r, i) && !Layout.IsStored(defs, s, i)) list.Add(i);
            return list;
        }

        public static double MultiplierAt(IReadOnlyList<StationDef> defs, GameState s, Rabbit r, int station)
        {
            return Multiplier(s, r) * (Suits(defs, r, station) ? TradeBonus : 1d);
        }

        public static double WorkMultiplier(IReadOnlyList<StationDef> defs, GameState s, Rabbit r) => MultiplierAt(defs, s, r, r.Station);

        public const int HeartMax = 10;
        public static readonly int[] HeartPoints = { 0, 10, 30, 80, 180, 380, 880, 1880, 3880, 8880, 18880 };
        public const int AffectionMax = 18880;
        public static readonly double[] HeartMultipliers = { 1d, 1.05, 1.1, 1.2, 1.3, 1.45, 1.6, 1.8, 2d, 2.3, 2.6 };
        public const int LineHearts = 3;
        public const int GiftHearts = 5;
        public const int AffectionGiftClovers = 1;
        public const int TenHeartGolden = 3;
        public const int PetPoints = 10;
        public const double PetEnergy = 5d;
        public const double DozeEnergy = 8d;

        public static int HeartsOf(int points)
        {
            int hearts = 0;
            while (hearts < HeartMax && points >= HeartPoints[hearts + 1]) hearts++;
            return hearts;
        }

        public static int HeartsOf(Rabbit r) => r == null ? 0 : HeartsOf(r.Affection);

        public static int PointsFor(int hearts) => HeartPoints[Math.Max(0, Math.Min(HeartMax, hearts))];

        public static int PointsToNext(Rabbit r)
        {
            int hearts = HeartsOf(r);
            return hearts >= HeartMax ? 0 : HeartPoints[hearts + 1] - Math.Max(0, r.Affection);
        }

        public static double HeartMultiplier(Rabbit r) => HeartMultipliers[HeartsOf(r)];

        public static bool FullHearts(Rabbit r) => HeartsOf(r) >= HeartMax;

        public static BondStep AddBond(GameState s, Rabbit r, int points)
        {
            var step = new BondStep();
            if (r == null || points <= 0) return step;
            int before = HeartsOf(r);
            r.Affection = Math.Min(AffectionMax, Math.Max(0, r.Affection) + points);
            int after = HeartsOf(r);
            step.LineUnlocked = before < LineHearts && after >= LineHearts;
            step.GiftGiven = before < GiftHearts && after >= GiftHearts;
            step.TenHearts = before < HeartMax && after >= HeartMax;
            if (step.GiftGiven) s.Clovers += AffectionGiftClovers;
            if (step.TenHearts) s.GoldenCarrots += TenHeartGolden;
            return step;
        }

        public static PetResult Pet(GameState s, Rabbit r, long day, bool wake = false)
        {
            var result = new PetResult();
            if (r == null) return result;
            result.Found = true;
            result.FirstToday = day > r.PetDay;
            if (result.FirstToday)
            {
                r.PetDay = day;
                s.Pets++;
                BondStep step = AddBond(s, r, PetPoints);
                result.LineUnlocked = step.LineUnlocked;
                result.GiftGiven = step.GiftGiven;
                result.TenHearts = step.TenHearts;
            }
            result.Energy = wake ? DozeEnergy : (result.FirstToday ? PetEnergy : 0d);
            if (result.Energy > 0d) s.Energy = Math.Min(Energy.Max, Math.Max(0d, s.Energy) + result.Energy);
            return result;
        }

        public static string Hearts(int points)
        {
            int full = HeartsOf(points);
            return new string('♥', full) + new string('♡', HeartMax - full);
        }

        public const int MaxNameLength = 12;

        public static string CleanName(string raw)
        {
            if (raw == null) return null;
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw)
                sb.Append(char.IsControl(c) ? ' ' : c);
            string name = sb.ToString().Trim();
            while (name.Contains("  ")) name = name.Replace("  ", " ");
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength).TrimEnd();
            return name.Length == 0 ? null : name;
        }

        public static bool Rename(GameState s, long id, string raw)
        {
            Rabbit r = Find(s, id);
            string name = CleanName(raw);
            if (r == null || name == null) return false;
            r.Name = name;
            return true;
        }

        public static bool See(GameState s, string kindId)
        {
            bool first = RabbitKinds.Find(kindId) != null && s.SeenKinds.Add(kindId);
            if (first) s.FreshKinds.Add(kindId);
            return first;
        }

        public static bool LookAt(GameState s, string kindId) => s.FreshKinds.Remove(kindId);

        public static string KindOf(Rabbit r)
        {
            RabbitKind kind = RabbitKinds.Find(r.Kind);
            return kind != null && kind.Tier == ClampTier(r.Tier) ? kind.Id : RabbitKinds.DefaultFor(r.Tier);
        }

        public static bool IsMaxLevel(Rabbit r) => r.Level >= LevelCaps[ClampTier(r.Tier)];

        public const int LevelsPerStar = 10;
        public static int Stars(int level) => Math.Max(0, level) / LevelsPerStar;
        public static int Stars(Rabbit r) => Stars(r.Level);
        public static int StarsEarned(int before, int after) => Math.Max(0, Stars(after) - Stars(before));

        public static BigNum LevelCost(Rabbit r)
        {
            return new BigNum(1.3).Pow(Math.Max(1, r.Level) - 1) * (100d * Math.Pow(2d, ClampTier(r.Tier)));
        }

        public static bool TryLevelUp(GameState s, long id)
        {
            Rabbit r = Find(s, id);
            if (r == null || IsMaxLevel(r)) return false;
            BigNum cost = LevelCost(r);
            if (s.Coins < cost) return false;
            s.Coins -= cost;
            r.Level++;
            return true;
        }

        public static int LevelsAffordable(Rabbit r, BigNum budget, out BigNum cost)
        {
            cost = BigNum.Zero;
            int level = Math.Max(1, r.Level), cap = LevelCaps[ClampTier(r.Tier)], n = 0;
            var probe = new Rabbit { Tier = r.Tier, Level = level };
            while (probe.Level < cap)
            {
                BigNum next = LevelCost(probe);
                if (cost + next > budget) break;
                cost += next;
                probe.Level++;
                n++;
            }
            return n;
        }

        public static int LevelUpMax(GameState s, long id, BigNum budget)
        {
            Rabbit r = Find(s, id);
            if (r == null) return 0;
            int n = LevelsAffordable(r, budget, out BigNum cost);
            if (n <= 0 || s.Coins < cost) return 0;
            s.Coins -= cost;
            r.Level += n;
            return n;
        }

        public static readonly double[] Odds = { 0.60, 0.25, 0.10, 0.04, 0.009, 0.001 };

        public static int RollTier(double roll) => RollTier(roll, Odds);

        public static int RollTier(double roll, double[] odds)
        {
            if (double.IsNaN(roll) || roll < 0d) roll = 0d;
            double edge = 0d;
            for (int tier = 0; tier < TierCount && tier < odds.Length; tier++)
            {
                edge += odds[tier];
                if (roll < edge) return tier;
            }
            return 0;
        }

        public static Rabbit Summon(GameState s, double roll, string name, double kindRoll = 0d)
        {
            if (s.Clovers < SummonCost) return null;
            s.Clovers -= SummonCost;
            s.CloverCalls++;
            return Arrive(s, RollTier(roll), name, kindRoll);
        }

        public static Rabbit Arrive(GameState s, int tier, string name, double kindRoll)
        {
            tier = ClampTier(tier);
            return ArriveKind(s, RabbitKinds.Pick(tier, kindRoll), name);
        }

        public static Rabbit ArriveKind(GameState s, string kindId, string name)
        {
            RabbitKind kind = RabbitKinds.Find(kindId);
            int tier = kind != null ? kind.Tier : 0;
            var rabbit = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Kind = kind != null ? kind.Id : RabbitKinds.DefaultFor(tier), Name = name };
            rabbit.FirstOfKind = See(s, rabbit.Kind);
            s.Rabbits.Add(rabbit);
            Garden.AddXp(s, Garden.XpSummon + (rabbit.FirstOfKind ? Garden.XpNewKind : 0));
            return rabbit;
        }

        public const string FirstKind = "tho-bong";

        public static Rabbit GiftFirst(GameState s, string name)
        {
            if (s.Rabbits.Count > 0) return null;
            var rabbit = new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Kind = FirstKind, Name = name, Station = s.Levels.Length > 0 && s.Levels[0] > 0 ? 0 : -1 };
            See(s, rabbit.Kind);
            s.Rabbits.Add(rabbit);
            return rabbit;
        }

        public static bool Mergeable(Rabbit r)
        {
            RabbitKind kind = RabbitKinds.Find(r.Kind);
            return kind == null || !kind.NewMoonOnly;
        }

        public static int CountOfTier(GameState s, int tier)
        {
            int n = 0;
            for (int i = 0; i < s.Rabbits.Count; i++)
                if (s.Rabbits[i].Tier == tier && Mergeable(s.Rabbits[i])) n++;
            return n;
        }

        public static bool CanMerge(GameState s, int tier)
        {
            return tier >= 0 && tier < TierCount - 1 && CountOfTier(s, tier) >= MergeCount;
        }

        public static Rabbit Merge(GameState s, int tier, double kindRoll = 0d)
        {
            if (!CanMerge(s, tier)) return null;
            var used = new List<Rabbit>(MergeCount);
            for (int pass = 0; pass < 2 && used.Count < MergeCount; pass++)
                for (int i = 0; i < s.Rabbits.Count && used.Count < MergeCount; i++)
                {
                    Rabbit r = s.Rabbits[i];
                    bool resting = r.Station < 0;
                    if (r.Tier == tier && resting == (pass == 0) && Mergeable(r)) used.Add(r);
                }

            int station = -1, level = 1, affection = 0;
            long petDay = -1;
            foreach (Rabbit r in used)
            {
                if (station < 0 && r.Station >= 0) station = r.Station;
                if (r.Level > level) level = r.Level;
                if (r.Affection > affection) affection = r.Affection;
                if (r.PetDay > petDay) petDay = r.PetDay;
                s.Rabbits.Remove(r);
            }
            var merged = new Rabbit { Id = ++s.NextRabbitId, Tier = tier + 1, Kind = RabbitKinds.Pick(tier + 1, kindRoll), Name = used[0].Name, Station = station, Level = level, Affection = affection, PetDay = petDay };
            merged.FirstOfKind = See(s, merged.Kind);
            s.Rabbits.Add(merged);
            s.Merges++;
            if (merged.FirstOfKind) Garden.AddXp(s, Garden.XpNewKind);
            foreach (Rabbit r in used)
                for (int i = 0; i < s.Items.Count; i++)
                {
                    Item item = s.Items[i];
                    if (item.WornBy != r.Id) continue;
                    item.WornBy = Items.Worn(s, merged.Id, item.Slot) == null ? merged.Id : 0;
                }
            return merged;
        }

        public static Rabbit Find(GameState s, long id)
        {
            for (int i = 0; i < s.Rabbits.Count; i++)
                if (s.Rabbits[i].Id == id) return s.Rabbits[i];
            return null;
        }

        public static Rabbit AtStation(GameState s, int station)
        {
            if (station < 0) return null;
            for (int i = 0; i < s.Rabbits.Count; i++)
                if (s.Rabbits[i].Station == station) return s.Rabbits[i];
            return null;
        }

        public static int StationsWithoutWorker(GameState s)
        {
            int n = 0;
            for (int i = 0; i < s.Levels.Length; i++)
                if (s.Levels[i] > 0 && AtStation(s, i) == null) n++;
            return n;
        }

        public static int StationsWithoutWorker(IReadOnlyList<StationDef> defs, GameState s)
        {
            int n = 0;
            for (int i = 0; i < s.Levels.Length; i++)
                if (s.Levels[i] > 0 && AtStation(s, i) == null && !Layout.IsStored(defs, s, i)) n++;
            return n;
        }

        public static double StationMultiplier(GameState s, int station)
        {
            Rabbit r = AtStation(s, station);
            return r == null ? 1d : Multiplier(s, r);
        }

        public static double StationMultiplier(IReadOnlyList<StationDef> defs, GameState s, int station)
        {
            Rabbit r = AtStation(s, station);
            return r == null ? 1d : MultiplierAt(defs, s, r, station);
        }

        public static bool Assign(GameState s, long id, int station)
        {
            Rabbit rabbit = Find(s, id);
            if (rabbit == null) return false;
            if (station < 0)
            {
                rabbit.Station = -1;
                return true;
            }
            if (station >= s.Levels.Length || s.Levels[station] <= 0) return false;
            Rabbit there = AtStation(s, station);
            if (there != null && there != rabbit) there.Station = -1;
            rabbit.Station = station;
            return true;
        }

        public static int BestStationFor(IReadOnlyList<StationDef> defs, GameState s, Rabbit rabbit)
        {
            List<int> order = StationsByWorth(defs, s);
            int empty = -1;
            foreach (int i in order)
            {
                if (AtStation(s, i) != null) continue;
                if (Suits(defs, rabbit, i)) return i;
                if (empty < 0) empty = i;
            }
            if (empty >= 0) return empty;

            int best = -1;
            double bestGain = 1e-9;
            foreach (int i in order)
            {
                Rabbit there = AtStation(s, i);
                if (there == rabbit) continue;
                double gain = MultiplierAt(defs, s, rabbit, i) - MultiplierAt(defs, s, there, i);
                if (gain > bestGain)
                {
                    bestGain = gain;
                    best = i;
                }
            }
            return best;
        }

        public static void AutoAssign(IReadOnlyList<StationDef> defs, GameState s)
        {
            foreach (Rabbit r in s.Rabbits) r.Station = -1;
            var free = new List<Rabbit>(s.Rabbits);
            foreach (int station in StationsByWorth(defs, s))
            {
                Rabbit pick = null;
                double worth = 0d;
                foreach (Rabbit r in free)
                {
                    double here = MultiplierAt(defs, s, r, station);
                    bool better = pick == null || here > worth + 1e-12
                        || (Math.Abs(here - worth) <= 1e-12 && (r.Tier > pick.Tier || (r.Tier == pick.Tier && r.Id < pick.Id)));
                    if (better) { pick = r; worth = here; }
                }
                if (pick == null) break;
                pick.Station = station;
                free.Remove(pick);
            }
        }

        static List<int> StationsByWorth(IReadOnlyList<StationDef> defs, GameState s)
        {
            var open = new List<int>();
            var worth = new Dictionary<int, double>();
            for (int i = 0; i < defs.Count; i++)
            {
                if (s.Levels[i] <= 0 || Layout.IsStored(defs, s, i)) continue;
                open.Add(i);
                StationDef d = defs[i];
                worth[i] = d.Kind == StationKind.Brew && d.CycleSeconds > 0d
                    ? Math.Log10(Math.Max(1e-9, d.BaseIncome * s.Levels[i] * Economy.MilestoneMultiplier(s.Levels[i]) / d.CycleSeconds))
                    : -1e9 + i;
            }
            open.Sort((a, b) => worth[b].CompareTo(worth[a]));
            return open;
        }
    }
}
