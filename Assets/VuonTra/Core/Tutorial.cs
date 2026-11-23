using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Tutorial
    {
        public const int Steps = 6;
        public const int TapRabbit = 0, RaiseTeapot = 1, OpenSecond = 2, CollectQuest = 3, CallRabbit = 4, PlaceRabbit = 5;

        public static bool IsDone(GameState s) => s.TutorialStep >= Steps;

        public static void Skip(GameState s) => s.TutorialStep = Steps;

        public static bool Met(GameState s, int step)
        {
            switch (step)
            {
                case TapRabbit: return s.TotalTaps >= 1;
                case RaiseTeapot: return s.Levels.Length > 0 && s.Levels[0] >= 2;
                case OpenSecond: return s.Levels.Length > 1 && s.Levels[1] >= 1;
                case CollectQuest: return s.Claimed.Count >= 1;
                case CallRabbit: return s.NextRabbitId >= 2;
                case PlaceRabbit:
                {
                    int working = 0;
                    foreach (Rabbit r in s.Rabbits) if (r.Station >= 0) working++;
                    return working >= 2;
                }
                default: return true;
            }
        }

        public static int Advance(GameState s)
        {
            int before = s.TutorialStep;
            while (!IsDone(s) && Met(s, s.TutorialStep)) s.TutorialStep++;
            return s.TutorialStep - before;
        }

        public static bool GrantCallClover(GameState s)
        {
            if (s.TutorialStep != CallRabbit || s.TutorialCloverGiven || s.Clovers >= Rabbits.SummonCost) return false;
            s.TutorialCloverGiven = true;
            s.Clovers += Rabbits.SummonCost;
            return true;
        }
    }
}
