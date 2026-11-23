using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// The seam to an analytics and crash-reporting service (#100), shaped like the rewarded-ad seam (#66). The game
    /// says what happened; the provider decides where it goes. Nothing is wired in yet: every build runs on
    /// <see cref="NoAnalytics"/>, which sends nothing anywhere.
    /// </summary>
    public interface IAnalytics
    {
        /// <summary>Something happened: a name from <see cref="AnalyticsEvents"/> and a few parameters.</summary>
        void Log(string name, params AnalyticsParam[] parameters);
        /// <summary>A fact about the player that later events are grouped by (the moon they are on, say).</summary>
        void SetUserProperty(string name, string value);
        /// <summary>An exception the game caught and lived through (a save that could not be written): reported, not thrown.</summary>
        void RecordException(Exception e);
    }

    /// <summary>One parameter of an event: a name and a text, a whole number or a real number.</summary>
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

    /// <summary>No provider: every call does nothing, nothing leaves the device. What every build runs on today.</summary>
    public sealed class NoAnalytics : IAnalytics
    {
        public void Log(string name, params AnalyticsParam[] parameters) { }
        public void SetUserProperty(string name, string value) { }
        public void RecordException(Exception e) { }
    }

    /// <summary>A provider in memory for the tests: keeps every event, user property and exception it is given.</summary>
    public sealed class MemoryAnalytics : IAnalytics
    {
        public sealed class Event
        {
            public string Name;
            public AnalyticsParam[] Params;

            /// <summary>The parameter of that name; throws when the event has none, so a test names what is missing.</summary>
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

        /// <summary>How many events of this name were logged.</summary>
        public int Count(string name)
        {
            int n = 0;
            foreach (Event e in Events) if (e.Name == name) n++;
            return n;
        }

        /// <summary>The events of this name, oldest first.</summary>
        public List<Event> All(string name) => Events.FindAll(e => e.Name == name);

        /// <summary>The latest event of this name, or null.</summary>
        public Event Last(string name) => Events.FindLast(e => e.Name == name);

        public void Clear()
        {
            Events.Clear();
            UserProperties.Clear();
            Exceptions.Clear();
        }
    }

    /// <summary>
    /// The events' and parameters' names (#100): stable snake_case, short enough for Firebase (40 characters), and none
    /// of Firebase's reserved names (session_start, first_open, ad_reward, error ...), so a provider can pass them on as
    /// they are. Changing one breaks every report built on it: add, do not rename.
    /// </summary>
    public static class AnalyticsEvents
    {
        // ----- events -----
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

        public static readonly string[] All =
        {
            GameOpen, SessionStart, SessionEnd, TutorialStep, TutorialComplete, TutorialSkip, GardenLevelUp, StationUnlocked,
            SegmentOpened, RabbitCalled, NewMoon, GoldenPacksOpened, PurchaseSuccess, PurchaseFailed, RewardedAd,
        };

        // ----- parameters -----
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

        // ----- parameter values -----
        public const string ViaClover = "clover", ViaGolden = "golden";
        public const string FromShop = "shop";
        public const string AdWatched = "watched", AdClosed = "closed", AdPass = "pass";

        // ----- user properties -----
        public const string UserMoon = "moon";
        public const string UserGardenLevel = "garden_level";

        /// <summary>The tutorial's steps by name, in the order of <see cref="Tutorial"/>.</summary>
        public static readonly string[] StepNames = { "tap_rabbit", "raise_teapot", "open_second", "collect_quest", "call_rabbit", "place_rabbit" };

        public static string StepNameOf(int step) => step >= 0 && step < StepNames.Length ? StepNames[step] : "step_" + step;

        /// <summary>Why a purchase did not pay, as a parameter value.</summary>
        public static string ReasonOf(PurchaseOutcome outcome)
        {
            switch (outcome)
            {
                case PurchaseOutcome.Cancelled: return "cancelled";
                case PurchaseOutcome.Failed: return "failed";
                case PurchaseOutcome.Unavailable: return "unavailable";
                case PurchaseOutcome.Duplicate: return "duplicate";
                default: return "paid";
            }
        }
    }

    /// <summary>The events with their parameters, one call each, so a hook in the game stays one line.</summary>
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

        /// <summary>
        /// A purchase came to an end. Only the product and the price as the store words it go out (#100): never an
        /// amount of our own. <paramref name="late"/>: the store handed it over by itself (one left unfinished before).
        /// </summary>
        public static void LogPurchase(this IAnalytics a, PurchaseResult r, string price, bool late = false)
        {
            if (r.Outcome == PurchaseOutcome.Paid)
                a.Log(AnalyticsEvents.PurchaseSuccess, new AnalyticsParam(AnalyticsEvents.ProductId, r.PackId), new AnalyticsParam(AnalyticsEvents.Price, price ?? ""), new AnalyticsParam(AnalyticsEvents.Late, late));
            else
                a.Log(AnalyticsEvents.PurchaseFailed, new AnalyticsParam(AnalyticsEvents.ProductId, r.PackId), new AnalyticsParam(AnalyticsEvents.Reason, AnalyticsEvents.ReasonOf(r.Outcome)));
        }

        public static void LogRewardedAd(this IAnalytics a, string placement, string game, string result) =>
            a.Log(AnalyticsEvents.RewardedAd, new AnalyticsParam(AnalyticsEvents.Placement, placement), new AnalyticsParam(AnalyticsEvents.Game, game ?? ""), new AnalyticsParam(AnalyticsEvents.Result, result));
    }

    /// <summary>
    /// Watches the garden between two looks and logs what moved forward (#100): tutorial steps, garden levels, stations
    /// opened and mist segments cleared. The runner looks after every change, so each of these is logged once, at the
    /// change that made it, however it came about (a call, a quest, a level bought, "upgrade all"). Going back (a new
    /// moon, a new game) logs nothing; going forward again logs again, with the moon it happened on.
    /// </summary>
    public sealed class AnalyticsWatch
    {
        int _step, _level, _segments;
        bool[] _open = Array.Empty<bool>();

        /// <summary>Takes the garden as it is now as the starting point, logging nothing (a save just loaded or restored).</summary>
        public void Reset(GameState s)
        {
            _step = s.TutorialStep;
            _level = Garden.Level(s);
            _segments = Expansion.Segments(s);
            if (_open.Length != s.Levels.Length) _open = new bool[s.Levels.Length];
            for (int i = 0; i < _open.Length; i++) _open[i] = s.Levels[i] > 0;
        }

        /// <summary>Logs every step forward since the last look, then takes the garden as it is now.</summary>
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

        /// <summary>The player skips the guidance: logged as one skip at the step they were on, not as the steps left.</summary>
        public void SkipTutorial(GameState s, IAnalytics a)
        {
            if (!Tutorial.IsDone(s)) a.LogTutorialSkip(s.TutorialStep);
            Tutorial.Skip(s);
            _step = s.TutorialStep;
        }
    }
}
