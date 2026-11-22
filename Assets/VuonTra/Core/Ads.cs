using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// The seam to a rewarded-video provider (#66). The game asks whether a video is ready for a place and asks to
    /// show it; the provider answers later whether it was watched to the end. Rewards are paid by <see cref="Ads"/>,
    /// never here. No real provider is wired in: the owner has not decided whether the game has ads at all.
    /// </summary>
    public interface IRewardedAds
    {
        /// <summary>A video can be shown for this place right now. An ad button is shown only while this is true.</summary>
        bool Ready(string placement);
        /// <summary>Shows a video; <paramref name="done"/> gets true when it was watched to the end.</summary>
        void Show(string placement, Action<bool> done);
    }

    /// <summary>
    /// A provider in memory for the editor and the tests: always ready, and every video "plays" for
    /// <see cref="Seconds"/> of <see cref="Advance"/> calls, then reports it was watched (or <see cref="NextResult"/>).
    /// </summary>
    public sealed class FakeRewardedAds : IRewardedAds
    {
        sealed class Video { public double Left; public bool Result; public Action<bool> Done; }

        public const double DefaultSeconds = 1d;
        public double Seconds = DefaultSeconds;
        public bool IsReady = true;
        /// <summary>The next video ends this way instead of being watched (false: closed before the end).</summary>
        public bool? NextResult;
        /// <summary>The places videos were shown for, in order.</summary>
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

        /// <summary>The clock: videos whose time is up report back.</summary>
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

        /// <summary>Ends every video now: the tests use it.</summary>
        public void Finish() => Advance(double.MaxValue);
    }

    /// <summary>No provider: never ready, so no ad button is ever shown. What every player build runs on today.</summary>
    public sealed class NoAds : IRewardedAds
    {
        public bool Ready(string placement) => false;
        public void Show(string placement, Action<bool> done) => done?.Invoke(false);
    }

    /// <summary>What an ad button offers right now: nothing (hidden), a video to watch, or (with the pass) the reward at once.</summary>
    public enum AdOffer { None, Watch, Instant }

    /// <summary>Today's claims at each place (#66), keyed by <see cref="Ads.Key"/>; a new day starts them over.</summary>
    public sealed class AdsRecord
    {
        public long Day = -1;
        public Dictionary<string, int> Used = new Dictionary<string, int>();
    }

    /// <summary>
    /// The three places a rewarded video may be offered (#66), and nowhere else: a second doubling of the time away
    /// once the free one is spent, one more round of each little game once its rounds are spent, and a golden carrot
    /// at the shop. Each has a cap per day by the clamped day, so a clock turned back brings nothing more. The no-ads
    /// pass turns every watch into an instant claim, with the same caps.
    /// </summary>
    public static class Ads
    {
        public const string AwayDouble = "away_double", ExtraRound = "extra_round", ShopGolden = "shop_golden";
        public static readonly string[] Placements = { AwayDouble, ExtraRound, ShopGolden };
        /// <summary>The little games an extra round is counted for, one cap each.</summary>
        public const string GamePounding = "pounding", GameLeaves = "leaves", GameFireflies = "fireflies";
        public static readonly string[] Games = { GamePounding, GameLeaves, GameFireflies };

        public const int AwayDoublePerDay = 1, ExtraRoundPerDay = 1, ShopGoldenPerDay = 2;
        /// <summary>What the shop's video pays.</summary>
        public const int ShopGoldenReward = 1;
        /// <summary>How long one no-ads pass lasts, in days of the clamped day.</summary>
        public const int PassDays = 30;

        /// <summary>Claims a place allows a day; 0 for anything that is not one of the three.</summary>
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

        /// <summary>The count a claim goes under: the place, and for an extra round the game too.</summary>
        public static string Key(string placement, string game = null) => placement == ExtraRound ? placement + "." + game : placement;

        /// <summary>Every key the save may hold: the save keeps only these.</summary>
        public static IEnumerable<string> Keys()
        {
            yield return AwayDouble;
            yield return ShopGolden;
            foreach (string game in Games) yield return Key(ExtraRound, game);
        }

        /// <summary>The cap that goes with a saved key.</summary>
        public static int CapOfKey(string key)
        {
            if (key == AwayDouble || key == ShopGolden) return Cap(key);
            foreach (string game in Games) if (key == Key(ExtraRound, game)) return Cap(ExtraRound);
            return 0;
        }

        /// <summary>Claims left today at a place. A day before the one stored is no fresh day.</summary>
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

        /// <summary>
        /// Whether the place has something to give right now, cap aside: the away doubling only once the day's free one
        /// is spent; an extra round only for an open game whose rounds are all spent; the shop's carrot always.
        /// </summary>
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

        /// <summary>What the button at a place offers now: hidden unless a claim is left and the place has something to give; then instant with the pass, a video when the provider is ready, else hidden.</summary>
        public static AdOffer Offer(GameState s, long day, IRewardedAds ads, string placement, string game = null)
        {
            if (Left(s, day, placement, game) <= 0 || !Eligible(s, day, placement, game)) return AdOffer.None;
            if (HasPass(s, day)) return AdOffer.Instant;
            return ads != null && ads.Ready(placement) ? AdOffer.Watch : AdOffer.None;
        }

        /// <summary>Pays the time away once more, for a watched video (or the pass): only once the free doubling is spent, once a day. A gift, so not counted as carrots earned.</summary>
        public static bool ClaimAwayDouble(GameState s, long day, BigNum paid)
        {
            if (paid < BigNum.One || Left(s, day, AwayDouble) <= 0 || !Eligible(s, day, AwayDouble)) return false;
            Use(s, day, AwayDouble, null);
            s.Coins += paid;
            return true;
        }

        /// <summary>Gives back one of today's rounds of a game whose rounds are spent, once per game per day; the round is then started as any other.</summary>
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

        /// <summary>The shop's golden carrot for a watched video, twice a day.</summary>
        public static bool ClaimShopGolden(GameState s, long day)
        {
            if (Left(s, day, ShopGolden) <= 0) return false;
            Use(s, day, ShopGolden, null);
            s.GoldenCarrots += ShopGoldenReward;
            return true;
        }

        // ----- the no-ads pass -----

        /// <summary>The pass runs from the day it was bought up to, not including, <see cref="GameState.NoAdsUntilDay"/>.</summary>
        public static bool HasPass(GameState s, long day) => day < s.NoAdsUntilDay;

        public static int PassDaysLeft(GameState s, long day) => (int)Math.Max(0L, s.NoAdsUntilDay - day);

        /// <summary>Thirty days from today, or thirty more after a pass still running. By the clamped day, so a clock turned back never stretches it.</summary>
        public static void GrantPass(GameState s, long day)
        {
            long from = Math.Max(Math.Max(0L, day), s.NoAdsUntilDay);
            s.NoAdsUntilDay = from + PassDays;
        }
    }
}
