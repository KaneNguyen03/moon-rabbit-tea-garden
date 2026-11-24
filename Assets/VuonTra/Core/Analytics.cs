using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public interface IAnalytics
    {
        void Log(string name, params AnalyticsParam[] parameters);
        void SetUserProperty(string name, string value);
        void RecordException(Exception e);
    }

    public readonly struct AnalyticsParam
    {
        public enum Kind { Text, Number, Real }

        public readonly string Name;
        public readonly Kind Type;
        public readonly string Text;
        public readonly long Number;
        public readonly double Real;

        public AnalyticsParam(string name, string text) { Name = name; Type = Kind.Text; Text = text ?? ""; Number = 0; Real = 0d; }
        public AnalyticsParam(string name, long number) { Name = name; Type = Kind.Number; Text = null; Number = number; Real = number; }
        public AnalyticsParam(string name, double real) { Name = name; Type = Kind.Real; Text = null; Number = (long)real; Real = real; }
        public AnalyticsParam(string name, bool flag) : this(name, flag ? 1L : 0L) { }

        public override string ToString() => Name + "=" + (Type == Kind.Text ? Text : Type == Kind.Number ? Number.ToString() : Real.ToString("R"));
    }

    public sealed class NoAnalytics : IAnalytics
    {
        public void Log(string name, params AnalyticsParam[] parameters) { }
        public void SetUserProperty(string name, string value) { }
        public void RecordException(Exception e) { }
    }

    public sealed class MemoryAnalytics : IAnalytics
    {
        public sealed class Event
        {
            public string Name;
            public AnalyticsParam[] Params;

            public AnalyticsParam this[string name]
            {
                get
                {
                    foreach (AnalyticsParam p in Params) if (p.Name == name) return p;
                    throw new KeyNotFoundException(Name + " has no parameter " + name);
                }
            }

            public bool Has(string name)
            {
                foreach (AnalyticsParam p in Params) if (p.Name == name) return true;
                return false;
            }

            public override string ToString() => Name + "(" + string.Join(", ", Array.ConvertAll(Params, p => p.ToString())) + ")";
        }

        public readonly List<Event> Events = new List<Event>();
        public readonly Dictionary<string, string> UserProperties = new Dictionary<string, string>();
        public readonly List<Exception> Exceptions = new List<Exception>();

        public void Log(string name, params AnalyticsParam[] parameters) =>
            Events.Add(new Event { Name = name, Params = parameters ?? Array.Empty<AnalyticsParam>() });

        public void SetUserProperty(string name, string value) => UserProperties[name] = value;

        public void RecordException(Exception e) { if (e != null) Exceptions.Add(e); }

        public int Count(string name)
        {
            int n = 0;
            foreach (Event e in Events) if (e.Name == name) n++;
            return n;
        }

        public List<Event> All(string name) => Events.FindAll(e => e.Name == name);

        public Event Last(string name) => Events.FindLast(e => e.Name == name);

        public void Clear()
        {
            Events.Clear();
            UserProperties.Clear();
            Exceptions.Clear();
        }
    }

    public static class AnalyticsEvents
    {
        public const string GameOpen = "game_open";
        public const string SessionStart = "play_session_start";
        public const string SessionEnd = "play_session_end";
        public const string TutorialStep = "tutorial_step";
        public const string TutorialComplete = "tutorial_complete";
        public const string TutorialSkip = "tutorial_skip";
        public const string GardenLevelUp = "garden_level_up";
        public const string StationUnlocked = "station_unlocked";
        public const string SegmentOpened = "segment_opened";
        public const string RabbitCalled = "rabbit_called";
        public const string NewMoon = "new_moon";
        public const string GoldenPacksOpened = "golden_packs_opened";
        public const string PurchaseSuccess = "purchase_success";
        public const string PurchaseFailed = "purchase_failed";
        public const string RewardedAd = "rewarded_ad";
        public const string NewcomerTask = "newcomer_task";
        public const string NewcomerDay = "newcomer_day";
        public const string OfferShown = "offer_shown";
        public const string OfferBought = "offer_bought";
        public const string OfferExpired = "offer_expired";
        public const string ReviewAsked = "review_asked";
        public const string ReviewLiked = "review_liked";
        public const string ReviewFeedback = "review_feedback";
        public const string MonthPassTier = "monthpass_tier";
        public const string MonthPassBought = "monthpass_bought";
        public const string PassBought = "pass_bought";
        public const string PassDayClaimed = "pass_day_claimed";

        public static readonly string[] All =
        {
            GameOpen, SessionStart, SessionEnd, TutorialStep, TutorialComplete, TutorialSkip, GardenLevelUp, StationUnlocked,
            SegmentOpened, RabbitCalled, NewMoon, GoldenPacksOpened, PurchaseSuccess, PurchaseFailed, RewardedAd,
            OfferShown, OfferBought, OfferExpired,
            ReviewAsked, ReviewLiked, ReviewFeedback,
            NewcomerTask, NewcomerDay,
            MonthPassTier, MonthPassBought,
            PassBought, PassDayClaimed,
        };

        public const string NewGarden = "new_garden";
        public const string Seconds = "seconds";
        public const string Step = "step";
        public const string StepName = "step_name";
        public const string Level = "level";
        public const string Moon = "moon";
        public const string Station = "station";
        public const string StationIndex = "station_index";
        public const string Segments = "segments";
        public const string Via = "via";
        public const string Tier = "tier";
        public const string Kind = "kind";
        public const string From = "from";
        public const string ProductId = "product_id";
        public const string Price = "price";
        public const string Late = "late";
        public const string Reason = "reason";
        public const string Placement = "placement";
        public const string Game = "game";
        public const string Result = "result";
        public const string Day = "day";
        public const string Slot = "slot";
        public const string Task = "task";
        public const string Fallback = "fallback";
        public const string Moment = "moment";
        public const string Asks = "asks";
        public const string Sent = "sent";
        public const string Chars = "chars";
        public const string Premium = "premium";
        public const string Month = "month";
        public const string Days = "days";

        public const string ViaClover = "clover", ViaGolden = "golden";
        public const string FromShop = "shop";
        public const string AdWatched = "watched", AdClosed = "closed", AdPass = "pass";

        public const string UserMoon = "moon";
        public const string UserGardenLevel = "garden_level";

        public static readonly string[] StepNames = { "tap_rabbit", "raise_teapot", "open_second", "collect_quest", "call_rabbit", "place_rabbit" };

        public static string StepNameOf(int step) => step >= 0 && step < StepNames.Length ? StepNames[step] : "step_" + step;

        public static string ReasonOf(PurchaseOutcome outcome)
        {
            switch (outcome)
            {
                case PurchaseOutcome.Cancelled: return "cancelled";
                case PurchaseOutcome.Failed: return "failed";
                case PurchaseOutcome.Unavailable: return "unavailable";
                case PurchaseOutcome.Duplicate: return "duplicate";
                case PurchaseOutcome.Pending: return "pending";
                default: return "paid";
            }
        }
    }

    public static class AnalyticsLog
    {
        public static void LogGameOpen(this IAnalytics a, bool newGarden) =>
            a.Log(AnalyticsEvents.GameOpen, new AnalyticsParam(AnalyticsEvents.NewGarden, newGarden));

        public static void LogSessionStart(this IAnalytics a, GameState s)
        {
            a.SetUserProperty(AnalyticsEvents.UserMoon, s.Season.ToString());
            a.SetUserProperty(AnalyticsEvents.UserGardenLevel, Garden.Level(s).ToString());
            a.Log(AnalyticsEvents.SessionStart, new AnalyticsParam(AnalyticsEvents.Moon, s.Season), new AnalyticsParam(AnalyticsEvents.Level, Garden.Level(s)));
        }

        public static void LogSessionEnd(this IAnalytics a, long seconds) =>
            a.Log(AnalyticsEvents.SessionEnd, new AnalyticsParam(AnalyticsEvents.Seconds, Math.Max(0L, seconds)));

        public static void LogTutorialStep(this IAnalytics a, int step) =>
            a.Log(AnalyticsEvents.TutorialStep, new AnalyticsParam(AnalyticsEvents.Step, step), new AnalyticsParam(AnalyticsEvents.StepName, AnalyticsEvents.StepNameOf(step)));

        public static void LogTutorialSkip(this IAnalytics a, int step) =>
            a.Log(AnalyticsEvents.TutorialSkip, new AnalyticsParam(AnalyticsEvents.Step, step), new AnalyticsParam(AnalyticsEvents.StepName, AnalyticsEvents.StepNameOf(step)));

        public static void LogRabbitCalled(this IAnalytics a, string via, Rabbit r) =>
            a.Log(AnalyticsEvents.RabbitCalled, new AnalyticsParam(AnalyticsEvents.Via, via), new AnalyticsParam(AnalyticsEvents.Tier, r.Tier), new AnalyticsParam(AnalyticsEvents.Kind, r.Kind));

        public static void LogNewMoon(this IAnalytics a, GameState s)
        {
            a.SetUserProperty(AnalyticsEvents.UserMoon, s.Season.ToString());
            a.Log(AnalyticsEvents.NewMoon, new AnalyticsParam(AnalyticsEvents.Moon, s.Season));
        }

        public static void LogGoldenPacksOpened(this IAnalytics a, string from) =>
            a.Log(AnalyticsEvents.GoldenPacksOpened, new AnalyticsParam(AnalyticsEvents.From, from));

        public static void LogPurchase(this IAnalytics a, PurchaseResult r, string price, bool late = false)
        {
            if (r.Outcome == PurchaseOutcome.Paid)
            {
                a.Log(AnalyticsEvents.PurchaseSuccess, new AnalyticsParam(AnalyticsEvents.ProductId, r.PackId), new AnalyticsParam(AnalyticsEvents.Price, price ?? ""), new AnalyticsParam(AnalyticsEvents.Late, late));
                if (r.PackId == MoonPass.ProductId) a.Log(AnalyticsEvents.PassBought, new AnalyticsParam(AnalyticsEvents.ProductId, r.PackId), new AnalyticsParam(AnalyticsEvents.Price, price ?? ""), new AnalyticsParam(AnalyticsEvents.Late, late));
            }
            else
                a.Log(AnalyticsEvents.PurchaseFailed, new AnalyticsParam(AnalyticsEvents.ProductId, r.PackId), new AnalyticsParam(AnalyticsEvents.Reason, AnalyticsEvents.ReasonOf(r.Outcome)));
        }

        public static void LogNewcomer(this IAnalytics a, NewcomerClaim claim, bool fallback)
        {
            if (!claim.Ok) return;
            a.Log(AnalyticsEvents.NewcomerTask, new AnalyticsParam(AnalyticsEvents.Day, claim.Task.Day), new AnalyticsParam(AnalyticsEvents.Slot, claim.Task.Slot),
                new AnalyticsParam(AnalyticsEvents.Task, fallback ? Newcomer.FallbackOf(claim.Task).Id : claim.Task.Id), new AnalyticsParam(AnalyticsEvents.Fallback, fallback));
            if (claim.DayPaid) a.Log(AnalyticsEvents.NewcomerDay, new AnalyticsParam(AnalyticsEvents.Day, claim.Task.Day));
        }

        public static void LogOffer(this IAnalytics a, string name, OfferDef offer, bool late = false)
        {
            if (offer == null) return;
            if (name == AnalyticsEvents.OfferBought)
                a.Log(name, new AnalyticsParam(AnalyticsEvents.ProductId, offer.Id), new AnalyticsParam(AnalyticsEvents.Level, offer.Level), new AnalyticsParam(AnalyticsEvents.Late, late));
            else
                a.Log(name, new AnalyticsParam(AnalyticsEvents.ProductId, offer.Id), new AnalyticsParam(AnalyticsEvents.Level, offer.Level));
        }

        public static void LogReviewAsked(this IAnalytics a, ReviewMoment moment, int asks) =>
            a.Log(AnalyticsEvents.ReviewAsked, new AnalyticsParam(AnalyticsEvents.Moment, ReviewAsk.NameOf(moment)), new AnalyticsParam(AnalyticsEvents.Asks, asks));

        public static void LogReviewLiked(this IAnalytics a, ReviewMoment moment) =>
            a.Log(AnalyticsEvents.ReviewLiked, new AnalyticsParam(AnalyticsEvents.Moment, ReviewAsk.NameOf(moment)));

        public static void LogReviewFeedback(this IAnalytics a, ReviewMoment moment, bool sent, int chars) =>
            a.Log(AnalyticsEvents.ReviewFeedback, new AnalyticsParam(AnalyticsEvents.Moment, ReviewAsk.NameOf(moment)),
                new AnalyticsParam(AnalyticsEvents.Sent, sent), new AnalyticsParam(AnalyticsEvents.Chars, chars));

        public static void LogMonthPassTier(this IAnalytics a, int tier, bool premium, int month) =>
            a.Log(AnalyticsEvents.MonthPassTier, new AnalyticsParam(AnalyticsEvents.Tier, tier), new AnalyticsParam(AnalyticsEvents.Premium, premium), new AnalyticsParam(AnalyticsEvents.Month, month));

        public static void LogMonthPassBought(this IAnalytics a, int month, int tier) =>
            a.Log(AnalyticsEvents.MonthPassBought, new AnalyticsParam(AnalyticsEvents.Month, month), new AnalyticsParam(AnalyticsEvents.Tier, tier));

        public static void LogLetterClaimed(this IAnalytics a, Letter letter)
        {
            if (letter == null || letter.Kind != LetterKind.MoonPassDay) return;
            a.Log(AnalyticsEvents.PassDayClaimed, new AnalyticsParam(AnalyticsEvents.Day, letter.PassDay), new AnalyticsParam(AnalyticsEvents.Days, letter.PassDays));
        }

        public static void LogRewardedAd(this IAnalytics a, string placement, string game, string result) =>
            a.Log(AnalyticsEvents.RewardedAd, new AnalyticsParam(AnalyticsEvents.Placement, placement), new AnalyticsParam(AnalyticsEvents.Game, game ?? ""), new AnalyticsParam(AnalyticsEvents.Result, result));
    }

    public sealed class AnalyticsWatch
    {
        int _step, _level, _segments;
        bool[] _open = Array.Empty<bool>();

        public void Reset(GameState s)
        {
            _step = s.TutorialStep;
            _level = Garden.Level(s);
            _segments = Expansion.Segments(s);
            if (_open.Length != s.Levels.Length) _open = new bool[s.Levels.Length];
            for (int i = 0; i < _open.Length; i++) _open[i] = s.Levels[i] > 0;
        }

        public void Note(IReadOnlyList<StationDef> defs, GameState s, IAnalytics a)
        {
            for (int step = Math.Max(0, _step); step < s.TutorialStep && step < Tutorial.Steps; step++) a.LogTutorialStep(step);
            if (_step < Tutorial.Steps && Tutorial.IsDone(s)) a.Log(AnalyticsEvents.TutorialComplete);
            _step = s.TutorialStep;

            int level = Garden.Level(s);
            for (int l = _level + 1; l <= level; l++)
                a.Log(AnalyticsEvents.GardenLevelUp, new AnalyticsParam(AnalyticsEvents.Level, l), new AnalyticsParam(AnalyticsEvents.Moon, s.Season));
            if (level != _level) a.SetUserProperty(AnalyticsEvents.UserGardenLevel, level.ToString());
            _level = level;

            if (_open.Length != s.Levels.Length) Array.Resize(ref _open, s.Levels.Length);
            for (int i = 0; i < s.Levels.Length; i++)
            {
                bool open = s.Levels[i] > 0;
                if (open && !_open[i])
                    a.Log(AnalyticsEvents.StationUnlocked, new AnalyticsParam(AnalyticsEvents.Station, i < defs.Count ? defs[i].Id : ""),
                        new AnalyticsParam(AnalyticsEvents.StationIndex, i), new AnalyticsParam(AnalyticsEvents.Moon, s.Season));
                _open[i] = open;
            }

            int segments = Expansion.Segments(s);
            for (int n = _segments + 1; n <= segments; n++)
                a.Log(AnalyticsEvents.SegmentOpened, new AnalyticsParam(AnalyticsEvents.Segments, n), new AnalyticsParam(AnalyticsEvents.Moon, s.Season));
            _segments = segments;
        }

        public void SkipTutorial(GameState s, IAnalytics a)
        {
            if (!Tutorial.IsDone(s)) a.LogTutorialSkip(s.TutorialStep);
            Tutorial.Skip(s);
            _step = s.TutorialStep;
        }
    }
}
