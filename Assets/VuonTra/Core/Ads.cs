using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public interface IRewardedAds
    {
        bool Ready(string placement);
        void Show(string placement, Action<bool> done);
    }

    public sealed class FakeRewardedAds : IRewardedAds
    {
        sealed class Video { public double Left; public bool Result; public Action<bool> Done; }

        public const double DefaultSeconds = 1d;
        public double Seconds = DefaultSeconds;
        public bool IsReady = true;
        public bool? NextResult;
        public readonly List<string> Shown = new List<string>();
        readonly List<Video> _playing = new List<Video>();

        public bool Playing => _playing.Count > 0;

        public bool Ready(string placement) => IsReady;

        public void Show(string placement, Action<bool> done)
        {
            if (!IsReady) { done?.Invoke(false); return; }
            Shown.Add(placement);
            bool result = NextResult ?? true;
            NextResult = null;
            if (Seconds <= 0d) { done?.Invoke(result); return; }
            _playing.Add(new Video { Left = Seconds, Result = result, Done = done });
        }

        public void Advance(double dt)
        {
            for (int i = 0; i < _playing.Count; i++)
            {
                Video p = _playing[i];
                p.Left -= dt;
                if (p.Left > 0d) continue;
                _playing.RemoveAt(i--);
                p.Done?.Invoke(p.Result);
            }
        }

        public void Finish() => Advance(double.MaxValue);
    }

    public sealed class NoAds : IRewardedAds
    {
        public bool Ready(string placement) => false;
        public void Show(string placement, Action<bool> done) => done?.Invoke(false);
    }

    public enum AdOffer { None, Watch, Instant }

    public sealed class AdsRecord
    {
        public long Day = -1;
        public Dictionary<string, int> Used = new Dictionary<string, int>();
    }

    public static class Ads
    {
        public const string AwayDouble = "away_double", ExtraRound = "extra_round", ShopGolden = "shop_golden";
        public static readonly string[] Placements = { AwayDouble, ExtraRound, ShopGolden };
        public const string GamePounding = "pounding", GameLeaves = "leaves", GameFireflies = "fireflies";
        public static readonly string[] Games = { GamePounding, GameLeaves, GameFireflies };

        public const int AwayDoublePerDay = 1, ExtraRoundPerDay = 1, ShopGoldenPerDay = 2;
        public const int ShopGoldenReward = 1;
        public const int PassDays = 30;

        public static int Cap(string placement)
        {
            switch (placement)
            {
                case AwayDouble: return AwayDoublePerDay;
                case ExtraRound: return ExtraRoundPerDay;
                case ShopGolden: return ShopGoldenPerDay;
                default: return 0;
            }
        }

        public static string Key(string placement, string game = null) => placement == ExtraRound ? placement + "." + game : placement;

        public static IEnumerable<string> Keys()
        {
            yield return AwayDouble;
            yield return ShopGolden;
            foreach (string game in Games) yield return Key(ExtraRound, game);
        }

        public static int CapOfKey(string key)
        {
            if (key == AwayDouble || key == ShopGolden) return Cap(key);
            foreach (string game in Games) if (key == Key(ExtraRound, game)) return Cap(ExtraRound);
            return 0;
        }

        public static int Left(GameState s, long day, string placement, string game = null)
        {
            int cap = Cap(placement);
            if (placement == ExtraRound && Array.IndexOf(Games, game) < 0) return 0;
            if (day > s.AdsToday.Day) return cap;
            s.AdsToday.Used.TryGetValue(Key(placement, game), out int used);
            return Math.Max(0, cap - used);
        }

        static void Use(GameState s, long day, string placement, string game)
        {
            if (day > s.AdsToday.Day)
            {
                s.AdsToday.Day = day;
                s.AdsToday.Used.Clear();
            }
            string key = Key(placement, game);
            s.AdsToday.Used.TryGetValue(key, out int used);
            s.AdsToday.Used[key] = used + 1;
        }

        public static bool Eligible(GameState s, long day, string placement, string game = null)
        {
            switch (placement)
            {
                case AwayDouble: return !Away.DoubleIsFree(s, day);
                case ExtraRound:
                    switch (game)
                    {
                        case GamePounding: return Pounding.Unlocked(s) && Pounding.PlaysLeft(s, day) <= 0;
                        case GameLeaves: return Leaves.Unlocked(s) && Leaves.PlaysLeft(s, day) <= 0;
                        case GameFireflies: return Fireflies.Unlocked(s) && Fireflies.PlaysLeft(s, day) <= 0;
                        default: return false;
                    }
                case ShopGolden: return true;
                default: return false;
            }
        }

        public static AdOffer Offer(GameState s, long day, IRewardedAds ads, string placement, string game = null)
        {
            if (Left(s, day, placement, game) <= 0 || !Eligible(s, day, placement, game)) return AdOffer.None;
            if (HasPass(s, day)) return AdOffer.Instant;
            return ads != null && ads.Ready(placement) ? AdOffer.Watch : AdOffer.None;
        }

        public static bool ClaimAwayDouble(GameState s, long day, BigNum paid)
        {
            if (paid < BigNum.One || Left(s, day, AwayDouble) <= 0 || !Eligible(s, day, AwayDouble)) return false;
            Use(s, day, AwayDouble, null);
            s.Coins += paid;
            return true;
        }

        public static bool ClaimExtraRound(GameState s, long day, string game)
        {
            if (Left(s, day, ExtraRound, game) <= 0 || !Eligible(s, day, ExtraRound, game)) return false;
            switch (game)
            {
                case GamePounding: s.PoundingPlays = Math.Max(0, s.PoundingPlays - 1); break;
                case GameLeaves: s.Leaves.Plays = Math.Max(0, s.Leaves.Plays - 1); break;
                case GameFireflies: s.Fireflies.Plays = Math.Max(0, s.Fireflies.Plays - 1); break;
            }
            Use(s, day, ExtraRound, game);
            return true;
        }

        public static bool ClaimShopGolden(GameState s, long day)
        {
            if (Left(s, day, ShopGolden) <= 0) return false;
            Use(s, day, ShopGolden, null);
            s.GoldenCarrots += ShopGoldenReward;
            return true;
        }

        public static bool HasPass(GameState s, long day) => day < s.NoAdsUntilDay;

        public static int PassDaysLeft(GameState s, long day) => (int)Math.Max(0L, s.NoAdsUntilDay - day);

        public static void GrantPass(GameState s, long day)
        {
            long from = Math.Max(Math.Max(0L, day), s.NoAdsUntilDay);
            s.NoAdsUntilDay = from + PassDays;
        }
    }
}
