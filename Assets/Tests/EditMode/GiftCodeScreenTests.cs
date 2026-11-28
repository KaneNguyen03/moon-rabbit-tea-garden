using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class GiftCodeScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;

        public static readonly long PinnedDay = GiftCodes.Day(2026, 11, 23);

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.DayClock = () => PinnedDay;
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Refresh();
            GiftCodes.SetKey("screen-test-key");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            UiKit.SetLargeText(false);
            Prefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            GiftCodes.SetKey(null);
            L10n.SetLocale("vi");
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        Text T(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Text>();
        }

        void OpenProfile()
        {
            screen.Shell.StartGame();
            screen.Shell.OpenPause();
            Find("Canvas/Pause/Card/Profile").onClick.Invoke();
            Assert.IsTrue(screen.Profile.IsOpen);
        }

        [Test]
        public void The_Profile_Has_A_Gift_Code_Field_Under_The_Player_Code()
        {
            OpenProfile();
            var field = (RectTransform)root.transform.Find("Canvas/Profile/Card/GiftCode");
            Assert.IsNotNull(field.GetComponent<InputField>());
            var code = (RectTransform)root.transform.Find("Canvas/Profile/Card/CodeRow");
            Assert.Less(field.anchoredPosition.y, code.anchoredPosition.y, "under the player code");
            var backup = (RectTransform)root.transform.Find("Canvas/Profile/Card/BackupTitle");
            Assert.Greater(field.anchoredPosition.y - field.sizeDelta.y / 2f, backup.anchoredPosition.y + backup.sizeDelta.y / 2f, "above the backup rows, not over them");
            Assert.AreEqual(L10n.T("gift.hint"), T("Canvas/Profile/Card/GiftCode/Hint").text);
            Assert.AreEqual(L10n.T("gift.redeem"), T("Canvas/Profile/Card/Redeem/Face/Text").text);
        }

        [Test]
        public void A_Right_Code_Pays_Closes_The_Cards_And_Cannot_Pay_Again()
        {
            OpenProfile();
            screen.Profile.GiftInput.text = "tratho-2026";
            Find("Canvas/Profile/Card/Redeem").onClick.Invoke();
            Assert.AreEqual(5, runner.State.GoldenCarrots);
            Assert.AreEqual(2, runner.State.Clovers);
            Assert.IsFalse(screen.Profile.IsOpen, "the cards step aside so the gift is seen flying to the purse");
            Assert.AreEqual(ShellState.Playing, screen.Shell.State);
            Assert.AreEqual(0, screen.Profile.Refusals);

            OpenProfile();
            Assert.AreEqual("", screen.Profile.GiftInput.text, "the field starts empty");
            screen.Profile.GiftInput.text = "TRATHO-2026";
            Find("Canvas/Profile/Card/Redeem").onClick.Invoke();
            Assert.AreEqual(5, runner.State.GoldenCarrots, "once per garden");
            Assert.IsTrue(screen.Profile.IsOpen);
            Assert.AreEqual(L10n.T("gift.used"), T("Canvas/Profile/Card/GiftNote").text);
            Assert.AreEqual(1, screen.Profile.Refusals);
        }

        [Test]
        public void A_Wrong_Code_Shakes_The_Field_Says_So_And_Pays_Nothing()
        {
            OpenProfile();
            screen.Profile.GiftInput.text = "KHONG-PHAI-MA";
            Find("Canvas/Profile/Card/Redeem").onClick.Invoke();
            Assert.AreEqual(1, screen.Profile.Refusals, "the field shook");
            Assert.AreEqual(L10n.T("gift.invalid"), T("Canvas/Profile/Card/GiftNote").text);
            Assert.AreEqual(0, runner.State.GoldenCarrots);
            Assert.AreEqual(0, runner.State.CodesUsed.Count);
            Assert.IsTrue(screen.Profile.IsOpen);
            Assert.AreEqual("KHONG-PHAI-MA", screen.Profile.GiftInput.text, "what was typed stays, to be put right");

            screen.Profile.GiftInput.text = "";
            screen.Profile.Redeem();
            string good = GiftCodes.Make(GiftCodes.SignedKind.Golden10, runner.TodayNumber + 5);
            screen.Profile.GiftInput.text = good.Substring(0, 8) + (good[8] == 'Z' ? 'Y' : 'Z');
            screen.Profile.Redeem();
            Assert.AreEqual(3, screen.Profile.Refusals);
            Assert.AreEqual(0, runner.State.GoldenCarrots);

            var field = (RectTransform)root.transform.Find("Canvas/Profile/Card/GiftCode");
            for (int i = 0; i < 30; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(-120f, field.anchoredPosition.x, 0.01f);
        }

        [Test]
        public void A_Signed_Code_And_The_Thank_You_Hat_Pay_From_The_Profile()
        {
            OpenProfile();
            screen.Profile.GiftInput.text = GiftCodes.Make(GiftCodes.SignedKind.Golden10, runner.TodayNumber + 5);
            screen.Profile.Redeem();
            Assert.AreEqual(10, runner.State.GoldenCarrots);

            OpenProfile();
            screen.Profile.GiftInput.text = "CAMON-PLAYTEST";
            screen.Profile.Redeem();
            Assert.IsTrue(Items.Owns(runner.State, "hat-no-trang"), "the Moon Bow is in the wardrobe");
        }

        [Test]
        public void The_Pause_Card_Shows_The_Mailbox_With_A_Badge_And_A_Letter_Can_Be_Claimed()
        {
            screen.Shell.StartGame();
            screen.Shell.OpenPause();
            Transform badge = root.transform.Find("Canvas/Pause/Card/Mail/Badge");
            Assert.IsFalse(badge.gameObject.activeSelf, "no letters, no badge");
            Assert.AreEqual(L10n.T("pause.mail"), T("Canvas/Pause/Card/Mail/Text").text);
            Find("Canvas/Pause/Card/Mail").onClick.Invoke();
            Assert.IsTrue(screen.Mail.IsOpen);
            Assert.IsTrue(root.transform.Find("Canvas/Mail/Card/Empty").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Mail/Card/Letter 0").gameObject.activeSelf);
            Find("Canvas/Mail/Card/Close").onClick.Invoke();
            Assert.IsFalse(screen.Mail.IsOpen);

            long today = runner.TodayNumber;
            Mailbox.Compensate(runner.State, today);
            runner.State.Letters.Add(new Letter { Id = "season-MidAutumn-2026", Kind = LetterKind.Season, Season = Season.MidAutumn, Day = today - 29 });
            screen.Shell.Resume();
            screen.Shell.OpenPause();
            Assert.IsTrue(badge.gameObject.activeSelf);
            Assert.AreEqual("2", T("Canvas/Pause/Card/Mail/Badge/Text").text);

            Find("Canvas/Pause/Card/Mail").onClick.Invoke();
            Assert.IsFalse(root.transform.Find("Canvas/Mail/Card/Empty").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("mail.makeup"), T("Canvas/Mail/Card/Letter 0/Title").text);
            Assert.AreEqual(L10n.T("mail.golden", Mailbox.MakeUpGolden) + " · " + L10n.T("mail.clovers", Mailbox.MakeUpClovers) + " · " + L10n.T("mail.days", 30),
                T("Canvas/Mail/Card/Letter 0/Sub").text);
            Assert.AreEqual(L10n.T("mail.season", L10n.Season(Season.MidAutumn)), T("Canvas/Mail/Card/Letter 1/Title").text);
            StringAssert.EndsWith(L10n.T("mail.days", 1), T("Canvas/Mail/Card/Letter 1/Sub").text);

            Find("Canvas/Mail/Card/Letter 0/Claim").onClick.Invoke();
            Assert.AreEqual(Mailbox.MakeUpGolden, runner.State.GoldenCarrots);
            Assert.AreEqual(Mailbox.MakeUpClovers, runner.State.Clovers);
            Assert.AreEqual(1, runner.State.Letters.Count);
            StringAssert.StartsWith(L10n.T("mail.claimed", ""), T("Canvas/Mail/Card/Note").text);
            Assert.AreEqual(L10n.T("mail.season", L10n.Season(Season.MidAutumn)), T("Canvas/Mail/Card/Letter 0/Title").text, "the next letter moves up");
            Assert.IsFalse(root.transform.Find("Canvas/Mail/Card/Letter 1").gameObject.activeSelf);
            Assert.AreEqual("1", T("Canvas/Pause/Card/Mail/Badge/Text").text, "the badge counts down");

            Find("Canvas/Mail/Card/Letter 0/Claim").onClick.Invoke();
            Assert.AreEqual(Mailbox.MakeUpGolden + Mailbox.SeasonGolden, runner.State.GoldenCarrots);
            Assert.IsTrue(root.transform.Find("Canvas/Mail/Card/Empty").gameObject.activeSelf);
            Assert.IsFalse(badge.gameObject.activeSelf);
        }

        [Test]
        public void The_Mail_Labels_Fit_In_Both_Languages_And_Large_Text()
        {
            foreach (string code in new[] { "vi", "en" })
                foreach (bool large in new[] { false, true })
                {
                    L10n.SetLocale(code);
                    UiKit.SetLargeText(large);
                    screen.RebuildUi();
                    long today = runner.TodayNumber;
                    runner.State.Letters.Clear();
                    for (int i = 0; i < Mailbox.MaxLetters; i++) Mailbox.Compensate(runner.State, today - i);
                    screen.Shell.SkipTitle();
                    screen.Shell.OpenPause();
                    Find("Canvas/Pause/Card/Mail").onClick.Invoke();
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform.Find("Canvas"));
                    Canvas.ForceUpdateCanvases();
                    foreach (string path in new[] { "Canvas/Pause/Card/Heading", "Canvas/Mail/Card/Title" })
                    {
                        Text text = T(path);
                        Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 1f, code + (large ? " large: " : ": ") + path + " \"" + text.text + "\"");
                    }
                    var heading = (RectTransform)root.transform.Find("Canvas/Pause/Card/Heading");
                    var mail = (RectTransform)root.transform.Find("Canvas/Pause/Card/Mail");
                    Assert.LessOrEqual(mail.anchoredPosition.x + mail.sizeDelta.x / 2f, ((RectTransform)heading.parent).rect.width / 2f + heading.anchoredPosition.x - heading.sizeDelta.x / 2f + 0.5f,
                        "the mail button stays clear of the heading");
                    screen.Mail.Close();
                    screen.Shell.Resume();
                }
        }

        [Test]
        public void A_Save_That_Cannot_Be_Read_Starts_A_New_Garden_With_A_Make_Up_Letter()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vuontra-test-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "save.json"), "{ this is not a save");
                var go = new GameObject("Broken");
                var broken = go.AddComponent<GameRunner>();
                broken.Catalog = runner.Catalog;
                broken.SaveDirectory = dir;
                broken.Init();
                Assert.AreEqual(1, broken.Letters.Count);
                Assert.AreEqual(LetterKind.MakeUp, broken.Letters[0].Kind);
                Assert.IsNotNull(broken.ClaimLetter(broken.Letters[0].Id));
                Assert.AreEqual(Mailbox.MakeUpGolden, broken.State.GoldenCarrots);
                Object.DestroyImmediate(go);

                GameState fine = GameState.NewGame(runner.Defs, runner.UpgradeDefs.Count);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "save.json"), SaveCodec.Encode(runner.Defs, fine, runner.UpgradeDefs));
                go = new GameObject("Fine");
                var loaded = go.AddComponent<GameRunner>();
                loaded.Catalog = runner.Catalog;
                loaded.SaveDirectory = dir;
                loaded.Init();
                Assert.AreEqual(0, loaded.Letters.Count);
                Object.DestroyImmediate(go);
            }
            finally
            {
                System.IO.Directory.Delete(dir, true);
            }
        }
    }
}
