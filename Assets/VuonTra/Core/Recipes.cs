using System;

namespace VuonTra.Core
{
    public static class Recipes
    {
        public const int Dishes = 3;
        public const int MaxLevel = 10;
        public const int CostPerLevelSquared = 5;
        public const double TopLevelMultiplier = 2d;
        public const double ExtraDishMultiplier = 1.25d;

        public const double RestSecondsPerLeaf = 600d;
        public const int LeavesPerOrder = 2;
        public const int LeavesPerFish = 1;
        public const int LeafFishFrom = 2;

        public static int UnlockLevel => NewMoon.FirstLevel;

        public static bool Unlocked(GameState s) => s != null && (s.Season > 0 || Garden.Level(s) >= UnlockLevel);

        public static double MaxStationMultiplier => TopLevelMultiplier * Math.Pow(ExtraDishMultiplier, Dishes - 1);

        public static int CostOfNext(int level) => level >= 1 && level < MaxLevel ? CostPerLevelSquared * level * level : 0;

        public static double LevelMultiplier(int level)
        {
            int clamped = Math.Max(1, Math.Min(MaxLevel, level));
            return 1d + (TopLevelMultiplier - 1d) * (clamped - 1) / (MaxLevel - 1);
        }

        public static int DishesOpen(GameState s, int station) => 1;

        public static int CurrentDish(GameState s, int station) => DishesOpen(s, station) - 1;

        public static int DishLevel(GameState s, int station, int dish)
        {
            if (s?.DishLevels == null || station < 0 || dish < 0 || dish >= Dishes) return 1;
            int at = station * Dishes + dish;
            return at < s.DishLevels.Length ? Math.Max(1, Math.Min(MaxLevel, s.DishLevels[at])) : 1;
        }

        public static bool AnyRaised(GameState s)
        {
            if (s?.DishLevels == null) return false;
            for (int i = 0; i < s.DishLevels.Length; i++) if (s.DishLevels[i] > 1) return true;
            return false;
        }

        public static int Level(GameState s, int station) => DishLevel(s, station, CurrentDish(s, station));

        public static double StationMultiplier(GameState s, int station)
            => LevelMultiplier(Level(s, station)) * Math.Pow(ExtraDishMultiplier, DishesOpen(s, station) - 1);

        public static int RaiseCost(GameState s, int station) => CostOfNext(Level(s, station));

        public static bool CanRaise(GameState s, int station)
        {
            if (!Unlocked(s) || station < 0 || station >= s.Levels.Length || s.Levels[station] <= 0) return false;
            int cost = RaiseCost(s, station);
            return cost > 0 && s.RecipeLeaves >= cost;
        }

        public static bool Raise(GameState s, int station)
        {
            if (!CanRaise(s, station)) return false;
            int level = Level(s, station);
            s.RecipeLeaves -= CostOfNext(level);
            Fit(s, s.Levels.Length);
            s.DishLevels[station * Dishes + CurrentDish(s, station)] = level + 1;
            return true;
        }

        public static int[] NewLevels(int stations)
        {
            var levels = new int[Math.Max(0, stations) * Dishes];
            for (int i = 0; i < levels.Length; i++) levels[i] = 1;
            return levels;
        }

        public static void SetLevel(GameState s, int station, int dish, int level)
        {
            if (station < 0 || dish < 0 || dish >= Dishes) return;
            Fit(s, station + 1);
            s.DishLevels[station * Dishes + dish] = Math.Max(1, Math.Min(MaxLevel, level));
        }

        static void Fit(GameState s, int stations)
        {
            int had = s.DishLevels?.Length ?? 0;
            if (had >= stations * Dishes) return;
            int[] levels = NewLevels(stations);
            if (had > 0) Array.Copy(s.DishLevels, levels, had);
            s.DishLevels = levels;
        }

        public static int Rest(GameState s, double seconds)
        {
            if (!(seconds > 0d) || !Unlocked(s)) return 0;
            int seated = RestSpots.SeatedCount(s);
            if (seated <= 0) return 0;
            double work = Math.Max(0d, s.RestLeafSeconds) + seconds * seated;
            double made = Math.Floor(work / RestSecondsPerLeaf);
            s.RestLeafSeconds = work - made * RestSecondsPerLeaf;
            int leaves = (int)Math.Min(int.MaxValue, made);
            Earn(s, leaves);
            return leaves;
        }

        public static void Earn(GameState s, int leaves)
        {
            if (leaves > 0 && Unlocked(s)) s.RecipeLeaves = (int)Math.Min(int.MaxValue, (long)Math.Max(0, s.RecipeLeaves) + leaves);
        }
    }
}
