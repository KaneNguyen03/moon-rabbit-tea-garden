using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>How close a tap landed to its beat.</summary>
    public enum Hit { Perfect, Good, Miss }

    /// <summary>What a finished game paid out.</summary>
    public sealed class PoundingResult
    {
        public int Score, Stars;
        public BigNum Carrots;
        public int Clovers, Golden;
        public bool HelperJoined;
    }

    /// <summary>
    /// One game of pounding the mooncake: thirty beats in about twenty-five seconds, a tap scored against
    /// each beat's fixed time. The beat schedule is a function of the beat index, never of frames.
    /// </summary>
    public sealed class PoundingRun
    {
        public int Beat { get; private set; }
        public int Score { get; private set; }
        public int PerfectStreak { get; private set; }
        /// <summary>"Dẻo!": five perfect beats in a row; every beat after that earns four until a miss.</summary>
        public bool Chewy { get; private set; }
        public int Perfects { get; private set; }
        public int Goods { get; private set; }
        public int Misses { get; private set; }
        public Hit Last { get; private set; } = Hit.Good;
        public bool Finished => Beat >= Pounding.Beats;

        /// <summary>A tap at <paramref name="now"/> seconds into the game: scored against the beat that is due.</summary>
        public Hit Tap(double now)
        {
            if (Finished) return Hit.Miss;
            Hit hit = Pounding.Judge(now, Beat);
            Apply(hit);
            Beat++;
            return hit;
        }

        /// <summary>Time passed: every beat whose window has closed without a tap is a miss. How many.</summary>
        public int Advance(double now)
        {
            int missed = 0;
            while (!Finished && now > Pounding.BeatTime(Beat) + Pounding.GoodWindow)
            {
                Apply(Hit.Miss);
                Beat++;
                missed++;
            }
            return missed;
        }

        void Apply(Hit hit)
        {
            Last = hit;
            switch (hit)
            {
                case Hit.Perfect:
                    Perfects++;
                    PerfectStreak++;
                    if (Chewy) Score += Pounding.ChewyPoints;
                    else
                    {
                        Score += Pounding.PerfectPoints;
                        if (PerfectStreak >= Pounding.ChewyAfter)
                        {
                            Chewy = true;
                            Score += Pounding.ChewyBonus;
                        }
                    }
                    break;
                case Hit.Good:
                    Goods++;
                    PerfectStreak = 0;
                    Score += Pounding.GoodPoints;
                    break;
                default:
                    Misses++;
                    PerfectStreak = 0;
                    Chewy = false;
                    break;
            }
        }
    }

    public static class Pounding
    {
        public const int Beats = 30;
        /// <summary>Seconds before the first beat, so the player sees the ring come in.</summary>
        public const double Lead = 1.5;
        public const double FirstInterval = 1.0;
        /// <summary>Every five beats the gap between beats shrinks by a tenth.</summary>
        public const int FasterEvery = 5;
        public const double Faster = 0.9;
        public const double PerfectWindow = 0.08, GoodWindow = 0.16;
        public const int PerfectPoints = 3, GoodPoints = 1, ChewyPoints = 4, ChewyAfter = 5, ChewyBonus = 5;
        /// <summary>Thirty perfects: five at three, the Dẻo bonus, twenty-five at four.</summary>
        public const int MaxScore = 120;
        public const int OneStar = 40, TwoStars = 75, ThreeStars = 105;
        public const int PlaysPerDay = 3;
        public const int UnlockLevel = 3;
        /// <summary>A full-score game pays this many seconds of income.</summary>
        public const double RewardSeconds = 60d;
        public const double HelperBonus = Rabbits.TradeBonus;

        public static double Interval(int beat) => FirstInterval * Math.Pow(Faster, beat / FasterEvery);

        /// <summary>When beat <paramref name="beat"/> (0-based) falls, in seconds from the start.</summary>
        public static double BeatTime(int beat)
        {
            double t = Lead;
            for (int b = 0; b < beat; b++) t += Interval(b);
            return t;
        }

        /// <summary>How long a game runs: the last beat's window, plus a breath.</summary>
        public static double Length => BeatTime(Beats - 1) + GoodWindow + 0.5d;

        public static Hit Judge(double tapTime, int beat)
        {
            double off = Math.Abs(tapTime - BeatTime(beat));
            if (off <= PerfectWindow) return Hit.Perfect;
            if (off <= GoodWindow) return Hit.Good;
            return Hit.Miss;
        }

        public static int StarsFor(int score) => score >= ThreeStars ? 3 : (score >= TwoStars ? 2 : (score >= OneStar ? 1 : 0));

        public static bool Unlocked(GameState s) => Garden.Level(s) >= UnlockLevel;

        /// <summary>Games left today. A clock turned back does not bring plays back.</summary>
        public static int PlaysLeft(GameState s, long day) => day > s.PoundingDay ? PlaysPerDay : Math.Max(0, PlaysPerDay - s.PoundingPlays);

        public static bool CanPlay(GameState s, long day) => Unlocked(s) && PlaysLeft(s, day) > 0;

        /// <summary>Spends one of today's plays. False when none is left or the mortar is not open yet.</summary>
        public static bool Start(GameState s, long day)
        {
            if (!CanPlay(s, day)) return false;
            if (day > s.PoundingDay)
            {
                s.PoundingDay = day;
                s.PoundingPlays = 0;
                s.PoundingStarToday = false;
                s.PoundingThreeToday = false;
            }
            s.PoundingPlays++;
            return true;
        }

        /// <summary>The baker at work who comes to pound along: the highest-level one, or null.</summary>
        public static Rabbit Helper(GameState s)
        {
            Rabbit best = null;
            foreach (Rabbit r in s.Rabbits)
                if (r.Station >= 0 && Rabbits.TradeOf(r) == RabbitTrade.Baker && (best == null || r.Level > best.Level)) best = r;
            return best;
        }

        /// <summary>Carrots for a score: a minute of income at full score, by the same rule quests use, half again with a helper.</summary>
        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int score, bool helper)
        {
            double share = Math.Max(0, Math.Min(MaxScore, score)) / (double)MaxScore;
            return Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds * share * (helper ? HelperBonus : 1d));
        }

        /// <summary>Pays a finished game and keeps the day's and the garden's counts.</summary>
        public static PoundingResult Finish(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, PoundingRun run)
        {
            bool helper = Helper(s) != null;
            var result = new PoundingResult { Score = run.Score, Stars = StarsFor(run.Score), HelperJoined = helper };
            result.Carrots = Reward(defs, s, m, run.Score, helper);
            s.Coins += result.Carrots;
            if (result.Stars >= 1 && !s.PoundingStarToday)
            {
                s.PoundingStarToday = true;
                s.Clovers += 1;
                result.Clovers = 1;
            }
            if (result.Stars >= 3 && !s.PoundingThreeToday)
            {
                s.PoundingThreeToday = true;
                s.GoldenCarrots += 1;
                result.Golden = 1;
            }
            s.PoundingGames++;
            if (result.Stars > s.PoundingBestStars) s.PoundingBestStars = result.Stars;
            return result;
        }
    }
}
