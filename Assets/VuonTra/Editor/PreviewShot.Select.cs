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
            new Group("map-edges", RenderMapEdges),
            new Group("layout", RenderLayout),
            new Group("pond", RenderPond),
            new Group("recipes", RenderRecipes),
            new Group("aquarium", RenderAquarium),
            new Group("recipe-book", RenderRecipeBook),
            new Group("newcomer", RenderNewcomer),
            new Group("offers", RenderOffers),
            new Group("quest-chests", RenderQuestChests),
            new Group("achievement-tiers", RenderAchievementTiers),
            new Group("month-pass", RenderMonthPass),
            new Group("welcome-back", RenderWelcomeBack),
            new Group("fair", RenderFair),
            new Group("trunk", RenderTrunk),
            new Group("juice", RenderJuice),
            new Group("intros", RenderIntros),
            new Group("first-session", RenderFirstSession),
            new Group("round-two-large-text", RenderRoundTwoLargeText),
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
        };

        static string _shotFolder;
        static string _group;
        static List<string> _seen;
        static HashSet<string> _only;
        static bool _namesOnly;

        static string OutFolder(string usual) => _shotFolder ?? usual;

        static bool Wanted(string name)
        {
            if (_seen == null) return true;
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
                foreach (string part in request.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] pieces = part.Split(':');
                    string name = pieces[0];
                    if (!Array.Exists(Groups, group => group.Name == name)) throw new ArgumentException("Preview: no group named " + name);
                    _only = pieces.Length > 1 ? new HashSet<string>(pieces[1].Split(',')) : null;
                    RenderGroups(group => group.Name == name);
                }
                File.WriteAllLines(Path.Combine(ShotFolder, ShotNamesFile), _seen);
                Debug.Log("[VuonTra] Shots written to " + ShotFolder);
            }
            finally
            {
                _shotFolder = null;
                _seen = null;
                _only = null;
                _namesOnly = false;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
