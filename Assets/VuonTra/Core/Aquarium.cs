using System;

namespace VuonTra.Core
{
    public sealed class AquariumRecord
    {
        public int[] Released = new int[Fishing.Kinds];
        public int Fresh;
        public bool SetGiven;
        public int SetJarsGiven;
        public bool CardSeen = true;
    }

    public struct ReleaseResult
    {
        public bool Released;
        public int Fish;
        public bool FirstOfKind;
        public int Milestones;
        public double Bonus;
        public bool FullSet;
        public int Golden, Jars, Clovers;
    }

    public static class Aquarium
    {
        public static readonly int[] Milestones = { 1, 5, 20, 50 };
        public static readonly double[] MilestoneBonus = { 0.0025, 0.0025, 0.005, 0.01 };
        public const int SetGolden = 10, SetJars = 1;

        public static int Kinds => Fishing.Kinds;

        public static double MaxBonus => Kinds * BonusOf(int.MaxValue);

        public static int AllKinds => (1 << Kinds) - 1;

        public static int Released(GameState s, int fish)
        {
            int[] tank = s?.Aquarium?.Released;
            return tank != null && fish >= 0 && fish < tank.Length ? Math.Max(0, tank[fish]) : 0;
        }

        public static int MilestonesOf(int count)
        {
            int n = 0;
            for (int i = 0; i < Milestones.Length; i++) if (count >= Milestones[i]) n++;
            return n;
        }

        public static int NextMilestone(int count)
        {
            for (int i = 0; i < Milestones.Length; i++) if (count < Milestones[i]) return Milestones[i];
            return 0;
        }

        public static int Reached(GameState s)
        {
            int n = 0;
            for (int k = 0; k < Kinds; k++) n += MilestonesOf(Released(s, k));
            return n;
        }

        public static double BonusOf(int count)
        {
            double bonus = 0d;
            for (int i = 0; i < Milestones.Length; i++) if (count >= Milestones[i]) bonus += MilestoneBonus[i];
            return bonus;
        }

        public static double NextBonus(int count)
        {
            for (int i = 0; i < Milestones.Length; i++) if (count < Milestones[i]) return MilestoneBonus[i];
            return 0d;
        }

        public static double Bonus(GameState s)
        {
            double bonus = 0d;
            for (int k = 0; k < Kinds; k++) bonus += BonusOf(Released(s, k));
            return bonus;
        }

        public static double Multiplier(GameState s) => 1d + Bonus(s);

        public static int KindsIn(GameState s)
        {
            int n = 0;
            for (int k = 0; k < Kinds; k++) if (Released(s, k) > 0) n++;
            return n;
        }

        public static bool FullSet(GameState s) => KindsIn(s) == Kinds;

        public static bool CanRelease(GameState s, int fish) =>
            s != null && Fishing.Unlocked(s) && fish >= 0 && fish < Kinds && fish < s.Fish.Length && s.Fish[fish] > 0;

        public static int FirstReleasable(GameState s)
        {
            for (int k = 0; k < Kinds; k++) if (CanRelease(s, k)) return k;
            return -1;
        }

        public static ReleaseResult Release(GameState s, int fish)
        {
            var result = new ReleaseResult { Fish = fish };
            if (!CanRelease(s, fish)) return result;
            AquariumRecord tank = Fit(s);
            int before = tank.Released[fish];
            s.Fish[fish]--;
            tank.Released[fish] = before == int.MaxValue ? before : before + 1;
            result.Released = true;
            result.FirstOfKind = before == 0;
            result.Milestones = MilestonesOf(tank.Released[fish]) - MilestonesOf(before);
            result.Bonus = BonusOf(tank.Released[fish]) - BonusOf(before);
            if (result.FirstOfKind) tank.Fresh |= 1 << fish;
            if (!tank.SetGiven && FullSet(s)) GiveSet(s, ref result);
            return result;
        }

        static void GiveSet(GameState s, ref ReleaseResult result)
        {
            s.Aquarium.SetGiven = true;
            s.Aquarium.CardSeen = false;
            s.GoldenCarrots += SetGolden;
            result.FullSet = true;
            result.Golden = SetGolden;
            result.Jars = QuestChests.GiveJars(s, SetJars);
            s.Aquarium.SetJarsGiven = result.Jars;
            result.Clovers = SetJars - result.Jars;
        }

        static AquariumRecord Fit(GameState s)
        {
            AquariumRecord tank = s.Aquarium ?? (s.Aquarium = new AquariumRecord());
            if (tank.Released == null || tank.Released.Length < Kinds)
            {
                var released = new int[Kinds];
                if (tank.Released != null) Array.Copy(tank.Released, released, tank.Released.Length);
                tank.Released = released;
            }
            return tank;
        }

        public static bool IsFresh(GameState s, int fish) => s?.Aquarium != null && fish >= 0 && fish < Kinds && (s.Aquarium.Fresh & (1 << fish)) != 0;

        public static bool AnyFresh(GameState s) => s?.Aquarium != null && (s.Aquarium.Fresh & AllKinds) != 0;

        public static void SeeFresh(GameState s)
        {
            if (s?.Aquarium != null) s.Aquarium.Fresh = 0;
        }

        public static bool CardWaiting(GameState s) => s?.Aquarium != null && s.Aquarium.SetGiven && !s.Aquarium.CardSeen;

        public static void SeeCard(GameState s)
        {
            if (s?.Aquarium != null) s.Aquarium.CardSeen = true;
        }
    }
}
