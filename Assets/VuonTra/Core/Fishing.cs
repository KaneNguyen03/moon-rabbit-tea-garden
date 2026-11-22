using System;

namespace VuonTra.Core
{
    /// <summary>Where one cast stands: the float bobbing, the float sinking (the moment to tap), or over.</summary>
    public enum CastPhase { Bobbing, Sinking, Caught, Lost }

    /// <summary>What one tap on the pond did.</summary>
    public enum CastTap
    {
        /// <summary>The cast was already over: nothing happened.</summary>
        None,
        /// <summary>Too early: the float comes up and bobs again, the bait still on the hook.</summary>
        Early,
        /// <summary>In the window: a fish.</summary>
        Caught
    }

    /// <summary>
    /// One cast at the moon pond (#58): the float bobs for two to six seconds, then sinks for half a second. A tap
    /// while it sinks lands the fish; a tap before that brings the float back up to bob again; no tap loses the bait.
    /// Pure: the view only feeds it time and taps. The bait was paid when the cast began (<see cref="Fishing.Cast"/>),
    /// and which fish bites is settled then too, so the result never depends on frame timing.
    /// </summary>
    public sealed class FishingCast
    {
        readonly Func<double> _roll;
        public CastPhase Phase { get; private set; } = CastPhase.Bobbing;
        /// <summary>Seconds since the float last came up.</summary>
        public double Elapsed { get; private set; }
        /// <summary>How long the float bobs this time before it sinks.</summary>
        public double BobSeconds { get; private set; }
        /// <summary>The fish on the line (an index into <see cref="Fishing.Points"/>), known from the start.</summary>
        public int Fish { get; }
        /// <summary>Taps that came too early.</summary>
        public int EarlyTaps { get; private set; }
        /// <summary>The caught fish went into the basket (<see cref="Fishing.Land"/>), so it is never counted twice.</summary>
        public bool Landed { get; internal set; }
        public bool Over => Phase == CastPhase.Caught || Phase == CastPhase.Lost;

        /// <param name="roll">Numbers in [0, 1): the first picks the fish, each later one a bobbing time.</param>
        public FishingCast(Func<double> roll)
        {
            _roll = roll ?? (() => 0d);
            Fish = Fishing.RollFish(_roll());
            BobSeconds = Fishing.BobFor(_roll());
        }

        /// <summary>Time passed: the float sinks once it has bobbed its time and the bait is lost once the window closes.</summary>
        public CastPhase Advance(double dt)
        {
            if (Over || !(dt > 0d)) return Phase; // also rejects NaN
            Elapsed += dt;
            if (Elapsed >= BobSeconds + Fishing.SinkSeconds) Phase = CastPhase.Lost;
            else if (Elapsed >= BobSeconds) Phase = CastPhase.Sinking;
            return Phase;
        }

        /// <summary>The player tapped the pond.</summary>
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

    /// <summary>What feeding a fish to a rabbit came to.</summary>
    public struct FeedResult
    {
        /// <summary>False when nothing was fed: no such rabbit, no such fish in the basket, or the bond already full.</summary>
        public bool Fed;
        public int Points;
        public int HeartsBefore, HeartsAfter;
        public BondStep Bond;
    }

    /// <summary>
    /// The moon pond (#58): five baits that come back one every 24 minutes (all five in two hours), six fish worth
    /// 1 to 30 heart points, a basket that keeps them, and feeding them to a rabbit for its bond. Bait time runs on
    /// the real clock: a clock turned back brings no bait, and the minutes it took away are not given back either.
    /// </summary>
    public static class Fishing
    {
        public const int BaitCap = 5;
        /// <summary>Seconds for one bait to come back.</summary>
        public const long BaitSeconds = 24 * 60;
        /// <summary>The float bobs between these many seconds, then sinks for <see cref="SinkSeconds"/>.</summary>
        public const double BobMin = 2d, BobMax = 6d, SinkSeconds = 0.5d;
        /// <summary>The garden level the pond opens at.</summary>
        public const int UnlockLevel = 1;

        /// <summary>The six fish, commonest first: heart points each is worth, and the chance of each on a cast (they add up to 1).</summary>
        public static readonly int[] Points = { 1, 3, 5, 8, 12, 30 };
        public static readonly double[] Odds = { 0.45, 0.25, 0.15, 0.09, 0.05, 0.01 };
        public static int Kinds => Points.Length;

        public static bool Unlocked(GameState s) => Garden.Level(s) >= UnlockLevel;

        /// <summary>Turns a number in [0, 1) into a fish by the odds; a roll out of range gives the commonest.</summary>
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

        /// <summary>Turns a number in [0, 1) into a bobbing time between <see cref="BobMin"/> and <see cref="BobMax"/>.</summary>
        public static double BobFor(double roll)
        {
            if (double.IsNaN(roll)) roll = 0d;
            return BobMin + (BobMax - BobMin) * Math.Max(0d, Math.Min(1d, roll));
        }

        /// <summary>
        /// Baits on hand at <paramref name="now"/> (unix seconds): the ones kept, plus one for every 24 minutes since the
        /// clock started, at most five. A garden that has never fished has all five. A time before the clock's start (the
        /// clock turned back) adds none.
        /// </summary>
        public static int Baits(GameState s, long now)
        {
            if (s.BaitAt < 0) return BaitCap;
            int kept = Math.Max(0, Math.Min(BaitCap, s.Baits));
            if (now <= s.BaitAt) return kept;
            long back = (now - s.BaitAt) / BaitSeconds;
            return (int)Math.Min(BaitCap, kept + back);
        }

        /// <summary>Seconds until the next bait comes back; 0 when all five are on hand.</summary>
        public static long NextBaitIn(GameState s, long now)
        {
            if (Baits(s, now) >= BaitCap) return 0;
            if (now < s.BaitAt) return BaitSeconds + (s.BaitAt - now); // a clock turned back waits for the time it took away
            return BaitSeconds - (now - s.BaitAt) % BaitSeconds;
        }

        /// <summary>Seconds until all five are on hand again; 0 when they are.</summary>
        public static long FullIn(GameState s, long now)
        {
            int missing = BaitCap - Baits(s, now);
            return missing <= 0 ? 0 : NextBaitIn(s, now) + (missing - 1) * BaitSeconds;
        }

        /// <summary>
        /// Takes one bait and casts. Null, with nothing spent, while the pond is closed or there is no bait. The bait
        /// that comes back next counts from the last one that came, or from now when all five were on hand; the clock
        /// never moves backwards, so turning it back and forth again gains nothing.
        /// </summary>
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

        /// <summary>Puts a caught fish in the basket, once. False for a cast that caught nothing or was landed already.</summary>
        public static bool Land(GameState s, FishingCast cast)
        {
            if (cast == null || cast.Phase != CastPhase.Caught || cast.Landed) return false;
            cast.Landed = true;
            int i = Math.Max(0, Math.Min(Kinds - 1, cast.Fish));
            s.Fish[i]++;
            s.FishCaught++;
            return true;
        }

        /// <summary>Fish of every kind in the basket.</summary>
        public static int InBasket(GameState s)
        {
            int n = 0;
            for (int i = 0; i < s.Fish.Length; i++) n += Math.Max(0, s.Fish[i]);
            return n;
        }

        /// <summary>
        /// Feeds one fish of kind <paramref name="fish"/> from the basket to a rabbit: its points go to the bond, with the
        /// line, the clover and the ten-heart gift when a step is crossed (<see cref="Rabbits.AddBond"/>). Nothing is
        /// taken when the rabbit's bond is already full.
        /// </summary>
        public static FeedResult Feed(GameState s, Rabbit r, int fish)
        {
            var result = new FeedResult();
            if (r == null || fish < 0 || fish >= Kinds || s.Fish[fish] <= 0 || r.Affection >= Rabbits.AffectionMax) return result;
            s.Fish[fish]--;
            s.FishFed++;
            result.Fed = true;
            result.HeartsBefore = Rabbits.HeartsOf(r);
            result.Bond = Rabbits.AddBond(s, r, Points[fish]);
            result.Points = Points[fish];
            result.HeartsAfter = Rabbits.HeartsOf(r);
            return result;
        }

        /// <summary>The kind in the basket worth the most points, or -1 when it is empty: what a quick "feed" gives first.</summary>
        public static int BestInBasket(GameState s)
        {
            for (int i = Kinds - 1; i >= 0; i--) if (s.Fish[i] > 0) return i;
            return -1;
        }
    }
}
