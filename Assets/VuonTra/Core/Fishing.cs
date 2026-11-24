using System;

namespace VuonTra.Core
{
    public enum CastPhase { Bobbing, Sinking, Caught, Lost }

    public enum CastTap
    {
        None,
        Early,
        Caught
    }

    public sealed class FishingCast
    {
        readonly Func<double> _roll;
        public CastPhase Phase { get; private set; } = CastPhase.Bobbing;
        public double Elapsed { get; private set; }
        public double BobSeconds { get; private set; }
        public int Fish { get; }
        public int EarlyTaps { get; private set; }
        public bool Landed { get; internal set; }
        public bool Over => Phase == CastPhase.Caught || Phase == CastPhase.Lost;

        public FishingCast(Func<double> roll)
        {
            _roll = roll ?? (() => 0d);
            Fish = Fishing.RollFish(_roll());
            BobSeconds = Fishing.BobFor(_roll());
        }

        public CastPhase Advance(double dt)
        {
            if (Over || !(dt > 0d)) return Phase;
            Elapsed += dt;
            if (Elapsed >= BobSeconds + Fishing.SinkSeconds) Phase = CastPhase.Lost;
            else if (Elapsed >= BobSeconds) Phase = CastPhase.Sinking;
            return Phase;
        }

        public CastTap Tap()
        {
            if (Over) return CastTap.None;
            if (Phase == CastPhase.Sinking)
            {
                Phase = CastPhase.Caught;
                return CastTap.Caught;
            }
            EarlyTaps++;
            Elapsed = 0d;
            BobSeconds = Fishing.BobFor(_roll());
            return CastTap.Early;
        }
    }

    public struct FeedResult
    {
        public bool Fed;
        public int Points;
        public int HeartsBefore, HeartsAfter;
        public int Leaves;
        public BondStep Bond;
    }

    public static class Fishing
    {
        public const int BaitCap = 5;
        public const long BaitSeconds = 24 * 60;
        public const double BobMin = 2d, BobMax = 6d, SinkSeconds = 0.5d;
        public const int UnlockLevel = 1;

        public static readonly int[] Points = { 1, 3, 5, 8, 12, 30 };
        public static readonly double[] Odds = { 0.45, 0.25, 0.15, 0.09, 0.05, 0.01 };
        public static int Kinds => Points.Length;

        public static bool Unlocked(GameState s) => Garden.Level(s) >= UnlockLevel;

        public static int RollFish(double roll)
        {
            if (double.IsNaN(roll) || roll < 0d) roll = 0d;
            double edge = 0d;
            for (int i = 0; i < Odds.Length; i++)
            {
                edge += Odds[i];
                if (roll < edge) return i;
            }
            return 0;
        }

        public static double BobFor(double roll)
        {
            if (double.IsNaN(roll)) roll = 0d;
            return BobMin + (BobMax - BobMin) * Math.Max(0d, Math.Min(1d, roll));
        }

        public static int Baits(GameState s, long now)
        {
            if (s.BaitAt < 0) return BaitCap;
            int kept = Math.Max(0, Math.Min(BaitCap, s.Baits));
            if (now <= s.BaitAt) return kept;
            long back = (now - s.BaitAt) / BaitSeconds;
            return (int)Math.Min(BaitCap, kept + back);
        }

        public static long NextBaitIn(GameState s, long now)
        {
            if (Baits(s, now) >= BaitCap) return 0;
            if (now < s.BaitAt) return BaitSeconds + (s.BaitAt - now);
            return BaitSeconds - (now - s.BaitAt) % BaitSeconds;
        }

        public static long FullIn(GameState s, long now)
        {
            int missing = BaitCap - Baits(s, now);
            return missing <= 0 ? 0 : NextBaitIn(s, now) + (missing - 1) * BaitSeconds;
        }

        public static FishingCast Cast(GameState s, long now, Func<double> roll)
        {
            if (!Unlocked(s)) return null;
            int have = Baits(s, now);
            if (have <= 0) return null;
            if (have >= BaitCap || s.BaitAt < 0) s.BaitAt = Math.Max(now, s.BaitAt);
            else if (now > s.BaitAt) s.BaitAt += (have - Math.Max(0, Math.Min(BaitCap, s.Baits))) * BaitSeconds;
            s.Baits = have - 1;
            s.Casts++;
            return new FishingCast(roll);
        }

        public static bool Land(GameState s, FishingCast cast)
        {
            if (cast == null || cast.Phase != CastPhase.Caught || cast.Landed) return false;
            cast.Landed = true;
            int i = Math.Max(0, Math.Min(Kinds - 1, cast.Fish));
            s.Fish[i]++;
            s.FishCaught++;
            return true;
        }

        public static int InBasket(GameState s)
        {
            int n = 0;
            for (int i = 0; i < s.Fish.Length; i++) n += Math.Max(0, s.Fish[i]);
            return n;
        }

        public static FeedResult Feed(GameState s, Rabbit r, int fish)
        {
            var result = new FeedResult();
            if (r == null || fish < 0 || fish >= Kinds || s.Fish[fish] <= 0) return result;
            bool full = r.Affection >= Rabbits.AffectionMax;
            if (full && !(MakesLeaf(fish) && Recipes.Unlocked(s))) return result;
            s.Fish[fish]--;
            s.FishFed++;
            result.Fed = true;
            result.HeartsBefore = Rabbits.HeartsOf(r);
            if (full)
            {
                result.Leaves = Recipes.LeavesPerFish;
                result.HeartsAfter = result.HeartsBefore;
                Recipes.Earn(s, result.Leaves);
                return result;
            }
            result.Bond = Rabbits.AddBond(s, r, Points[fish]);
            result.Points = Points[fish];
            result.HeartsAfter = Rabbits.HeartsOf(r);
            return result;
        }

        public static bool MakesLeaf(int fish) => fish >= Recipes.LeafFishFrom && fish < Kinds;

        public static bool CanFeed(GameState s, Rabbit r, int fish)
        {
            if (r == null || fish < 0 || fish >= Kinds || s.Fish[fish] <= 0) return false;
            return r.Affection < Rabbits.AffectionMax || (MakesLeaf(fish) && Recipes.Unlocked(s));
        }

        public static int BestInBasket(GameState s)
        {
            for (int i = Kinds - 1; i >= 0; i--) if (s.Fish[i] > 0) return i;
            return -1;
        }
    }
}
