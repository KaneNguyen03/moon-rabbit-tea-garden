using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// The first minute: six things to do, one at a time, each read off the garden rather than off a button,
    /// so a player who does it their own way moves on just the same. Skipping is always allowed, and a
    /// garden that has played before never sees it.
    /// </summary>
    public static class Tutorial
    {
        /// <summary>Tap the rabbit, raise the teapot, open the second station, collect a quest, call a rabbit, put it to work.</summary>
        public const int Steps = 6;
        public const int TapRabbit = 0, RaiseTeapot = 1, OpenSecond = 2, CollectQuest = 3, CallRabbit = 4, PlaceRabbit = 5;

        public static bool IsDone(GameState s) => s.TutorialStep >= Steps;

        public static void Skip(GameState s) => s.TutorialStep = Steps;

        /// <summary>True when the garden shows that step <paramref name="step"/> has been done.</summary>
        public static bool Met(GameState s, int step)
        {
            switch (step)
            {
                case TapRabbit: return s.TotalTaps >= 1;
                case RaiseTeapot: return s.Levels.Length > 0 && s.Levels[0] >= 2;
                case OpenSecond: return s.Levels.Length > 1 && s.Levels[1] >= 1;
                case CollectQuest: return s.Claimed.Count >= 1;
                case CallRabbit: return s.NextRabbitId >= 2; // the first rabbit was a gift; the next one was called
                case PlaceRabbit:
                {
                    int working = 0;
                    foreach (Rabbit r in s.Rabbits) if (r.Station >= 0) working++;
                    return working >= 2;
                }
                default: return true;
            }
        }

        /// <summary>Moves past every step the garden already shows done. How many steps it moved.</summary>
        public static int Advance(GameState s)
        {
            int before = s.TutorialStep;
            while (!IsDone(s) && Met(s, s.TutorialStep)) s.TutorialStep++;
            return s.TutorialStep - before;
        }

        /// <summary>
        /// The call step needs a clover, and a new garden may have none yet: the step brings one, once.
        /// True when it did.
        /// </summary>
        public static bool GrantCallClover(GameState s)
        {
            if (s.TutorialStep != CallRabbit || s.TutorialCloverGiven || s.Clovers >= Rabbits.SummonCost) return false;
            s.TutorialCloverGiven = true;
            s.Clovers += Rabbits.SummonCost;
            return true;
        }
    }
}
