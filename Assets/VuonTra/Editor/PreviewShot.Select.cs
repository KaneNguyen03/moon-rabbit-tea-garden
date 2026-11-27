using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Object = UnityEngine.Object;

namespace VuonTra.EditorTools
{
    public static partial class PreviewShot
    {
        public const string ShotFolder = "Logs/shot";
        public const string ShotNamesFile = "names.txt";
        public const string ShotRequest = "VUONTRA_SHOTS";
        public const string ShotNamesOnly = "VUONTRA_SHOT_NAMES_ONLY";

        delegate void SharedScene(GameRunner runner, GardenScreen screen);

        sealed class Group
        {
            public readonly string Name;
            public readonly SharedScene Shared;
            public readonly Action Alone;
            public bool StartsOwnGame;
            public bool OnlyByName;
            public bool Standalone;

            public Group(string name, SharedScene shared) { Name = name; Shared = shared; }
            public Group(string name, Action alone) { Name = name; Alone = alone; }
        }

        static readonly string[] BothLocales = { "vi", "en" };

        static readonly Group[] Groups =
        {
            new Group("main", RenderMain) { StartsOwnGame = true },
            new Group("guidance", RenderGuidance),
            new Group("land-waves", RenderLandWaves),
            new Group("photo-ask", RenderPhotoAsk),
            new Group("rest-share", RenderRestShare),
            new Group("chatter", RenderChatter),
            new Group("album-sets", RenderAlbumSets),
            new Group("moon-jars", RenderMoonJars),
            new Group("gift-codes", RenderGiftCodes),
            new Group("four-stations", RenderFourStations),
            new Group("ad-seams", RenderAdSeams),
            new Group("moon-pass", RenderMoonPass),
            new Group("forest-rings", RenderForestRings),
            new Group("map-edges", RenderMapEdges) { Standalone = true },
            new Group("layout", RenderLayout),
            new Group("pond", RenderPond),
            new Group("recipes", RenderRecipes),
            new Group("aquarium", RenderAquarium),
            new Group("recipe-book", RenderRecipeBook),
            new Group("newcomer", RenderNewcomer),
            new Group("offers", RenderOffers),
            new Group("quest-chests", RenderQuestChests) { Standalone = true },
            new Group("achievement-tiers", RenderAchievementTiers),
            new Group("month-pass", RenderMonthPass) { Standalone = true },
            new Group("welcome-back", RenderWelcomeBack) { Standalone = true },
            new Group("fair", RenderFair),
            new Group("trunk", RenderTrunk) { Standalone = true },
            new Group("juice", RenderJuice) { Standalone = true },
            new Group("intros", RenderIntros),
            new Group("first-session", RenderFirstSession) { Standalone = true },
            new Group("round-two-large-text", RenderRoundTwoLargeText) { Standalone = true },
            new Group("qa-nits-2", RenderQaNits2),
            new Group("reserved-props", RenderReservedProps),
            new Group("guest-by-pond", RenderGuestByPond),
            new Group("ngang-nits", RenderNgangNits),
            new Group("station-spacing", RenderStationSpacing),
            new Group("empty-spot-hold", () => { foreach (string locale in BothLocales) RenderEmptySpotHold(locale); }),
            new Group("basket-follows", () => { foreach (string locale in BothLocales) RenderBasketFollows(locale); }),
            new Group("view-edges", RenderViewEdges),
            new Group("away-count", () => { foreach (string locale in BothLocales) RenderAwayCount(locale); }),
            new Group("monetization-off", RenderMonetizationOffOnly),
            new Group("garden-25d", RenderGarden25D) { OnlyByName = true },
            new Group("feel-lane", RenderFeelLane) { Standalone = true },
            new Group("overnight", RenderOvernight) { Standalone = true },
        };

        static string _shotFolder;
        static string _group;
        static string _asked;
        static List<string> _seen;
        static HashSet<string> _only;
        static bool _namesOnly;

        static string OutFolder(string usual) => _shotFolder ?? usual;

        static bool Wanted(string name)
        {
            if (_seen == null) return true;
            if (_asked != null && _group != _asked) return false;
            _seen.Add(_group + "\t" + name);
            return !_namesOnly && (_only == null || _only.Contains(name));
        }

        static void RenderGroups(Predicate<Group> wanted)
        {
            GameRunner runner = null;
            GardenScreen screen = null;
            foreach (Group group in Groups)
            {
                if (!wanted(group)) continue;
                _group = group.Name;
                if (group.Alone != null)
                {
                    group.Alone();
                    screen = null;
                    continue;
                }
                if (screen == null)
                {
                    EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
                    L10n.SetLocale("vi");
                    Features.Monetization = true;
                    runner = Object.FindAnyObjectByType<GameRunner>();
                    screen = Object.FindAnyObjectByType<GardenScreen>();
                    if (!group.StartsOwnGame)
                    {
                        runner.Init(GameState.NewGame(runner.Catalog.BuildDefs()));
                        screen.Build();
                        screen.ClockHours = () => 22.0;
                    }
                }
                group.Shared(runner, screen);
            }
        }

        public static void RenderSelected()
        {
            string request = Environment.GetEnvironmentVariable(ShotRequest) ?? "";
            _namesOnly = Environment.GetEnvironmentVariable(ShotNamesOnly) == "1";
            _shotFolder = ShotFolder;
            _seen = new List<string>();
            Directory.CreateDirectory(ShotFolder);
            try
            {
                string[] parts = request.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 && !_namesOnly) throw new ArgumentException("Preview: " + ShotRequest + " names no group");
                if (parts.Length == 0) RenderGroups(group => true);
                foreach (string part in parts)
                {
                    string[] pieces = part.Split(':');
                    int asked = Array.FindIndex(Groups, group => group.Name == pieces[0]);
                    if (asked < 0) throw new ArgumentException("Preview: no group named " + pieces[0]);
                    bool replay = Groups[asked].Shared != null && !Groups[asked].Standalone;
                    _only = pieces.Length > 1 ? new HashSet<string>(pieces[1].Split(',')) : null;
                    _asked = pieces[0];
                    RenderGroups(group => group == Groups[asked] || (replay && group.Shared != null && Array.IndexOf(Groups, group) < asked));
                }
                File.WriteAllLines(Path.Combine(ShotFolder, ShotNamesFile), _seen);
                Debug.Log("[VuonTra] Shots written to " + ShotFolder);
            }
            finally
            {
                _shotFolder = null;
                _seen = null;
                _asked = null;
                _only = null;
                _namesOnly = false;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
