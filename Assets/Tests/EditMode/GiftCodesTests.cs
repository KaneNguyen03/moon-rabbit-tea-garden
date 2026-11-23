using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class GiftCodesTests
    {
        const string TestKey = "test-key-68";
        List<StationDef> defs;
        List<ItemDef> items;
        GameState s;
        long today;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            items = new List<ItemDef>
            {
                new ItemDef { Id = "hat-no-trang", Name = "Nơ trăng", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 40, Tier = 2 },
                new ItemDef { Id = "hat-non-la", Name = "Nón lá", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 20 }
            };
            today = GiftCodes.Day(2026, 11, 23);
            GiftCodes.SetKey(TestKey);
        }

        [TearDown]
        public void TearDown() => GiftCodes.SetKey(null);

        [Test]
        public void The_Playtest_Code_Pays_Five_Golden_Carrots_And_Two_Clovers_Once()
        {
            GiftCodes.Redeemed first = GiftCodes.Redeem(s, "TRATHO-2026", today, items);
            Assert.AreEqual(GiftCodes.Result.Ok, first.Result);
            Assert.AreEqual(5, first.Golden);
            Assert.AreEqual(2, first.Clovers);
            Assert.AreEqual(5, s.GoldenCarrots);
            Assert.AreEqual(2, s.Clovers);

            GiftCodes.Redeemed again = GiftCodes.Redeem(s, "TRATHO-2026", today + 1, items);
            Assert.AreEqual(GiftCodes.Result.Used, again.Result);
            Assert.AreEqual(5, s.GoldenCarrots, "a code pays a garden once");
            Assert.AreEqual(2, s.Clovers);
        }

        [TestCase(" tratho-2026 ")]
        [TestCase("tratho 2026")]
        [TestCase("TRATHO2026")]
        public void A_Code_Is_Read_Without_Case_Spaces_Or_Dashes(string typed)
        {
            Assert.AreEqual(GiftCodes.Result.Ok, GiftCodes.Redeem(s, typed, today, items).Result);
            Assert.AreEqual(GiftCodes.Result.Used, GiftCodes.Redeem(s, "TRATHO-2026", today, items).Result, "however it was typed, it is the same code");
        }

        [Test]
        public void The_Thank_You_Code_Brings_The_Moon_Bow_Hat_And_Its_Price_When_The_Garden_Has_One()
        {
            GiftCodes.Redeemed r = GiftCodes.Redeem(s, "camon-playtest", today, items);
            Assert.AreEqual(GiftCodes.Result.Ok, r.Result);
            Assert.IsNotNull(r.Hat);
            Assert.AreEqual("hat-no-trang", r.Hat.DefId);
            Assert.IsTrue(Items.Owns(s, "hat-no-trang"));
            Assert.AreEqual(0, s.GoldenCarrots);

            GameState other = GameState.NewGame(defs);
            Items.Grant(other, items[0], 2);
            GiftCodes.Redeemed owned = GiftCodes.Redeem(other, "CAMON-PLAYTEST", today, items);
            Assert.AreEqual(GiftCodes.Result.Ok, owned.Result);
            Assert.IsNull(owned.Hat, "one of each hat");
            Assert.AreEqual(40, owned.Golden, "its shop price instead");
            Assert.AreEqual(40, other.GoldenCarrots);
        }

        [Test]
        public void An_Unknown_Code_Pays_Nothing()
        {
            foreach (string code in new[] { "", "   ", null, "TRATHO", "THO-7K3M9Q", "HELLO-WORLD", "ABCD-EFG" })
                Assert.AreEqual(GiftCodes.Result.Invalid, GiftCodes.Redeem(s, code, today, items).Result, code ?? "null");
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(0, s.Clovers);
            Assert.AreEqual(0, s.CodesUsed.Count, "a refused code is not remembered");
        }

        [Test]
        public void A_Table_Code_Past_Its_Last_Day_Is_Refused()
        {
            long last = GiftCodes.Find("TRATHO-2026").LastDay;
            Assert.Greater(last, today, "the playtest codes work now");
            Assert.AreEqual(GiftCodes.Result.Expired, GiftCodes.Redeem(s, "TRATHO-2026", last + 1, items).Result);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(GiftCodes.Result.Ok, GiftCodes.Redeem(s, "TRATHO-2026", last, items).Result, "the last day itself still counts");
        }

        [Test]
        public void A_Signed_Code_Pays_What_Its_Kind_Says_Once()
        {
            string code = GiftCodes.Make(GiftCodes.SignedKind.Golden10, today + 30);
            StringAssert.IsMatch("^[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}$", code);
            GiftCodes.Redeemed r = GiftCodes.Redeem(s, code, today, items);
            Assert.AreEqual(GiftCodes.Result.Ok, r.Result);
            Assert.AreEqual(10, s.GoldenCarrots);
            Assert.AreEqual(GiftCodes.Result.Used, GiftCodes.Redeem(s, code.Replace("-", "").ToLowerInvariant(), today, items).Result);
            Assert.AreEqual(10, s.GoldenCarrots);
        }

        [Test]
        public void Every_Signed_Kind_Pays_Its_Gift()
        {
            var want = new Dictionary<GiftCodes.SignedKind, (int golden, int clovers)>
            {
                { GiftCodes.SignedKind.Golden5, (5, 0) }, { GiftCodes.SignedKind.Golden10, (10, 0) },
                { GiftCodes.SignedKind.Clovers2, (0, 2) }, { GiftCodes.SignedKind.Bundle, (5, 2) }
            };
            foreach (var pair in want)
            {
                GameState g = GameState.NewGame(defs);
                GiftCodes.Redeemed r = GiftCodes.Redeem(g, GiftCodes.Make(pair.Key, today), today, items);
                Assert.AreEqual(GiftCodes.Result.Ok, r.Result, pair.Key.ToString());
                Assert.AreEqual(pair.Value.golden, g.GoldenCarrots, pair.Key.ToString());
                Assert.AreEqual(pair.Value.clovers, g.Clovers, pair.Key.ToString());
            }
        }

        [Test]
        public void A_Signed_Code_With_A_Wrong_Signature_Is_Refused()
        {
            string code = GiftCodes.Make(GiftCodes.SignedKind.Bundle, today + 30);
            char last = code[code.Length - 1];
            string forged = code.Substring(0, code.Length - 1) + (last == 'Z' ? 'Y' : 'Z');
            Assert.AreEqual(GiftCodes.Result.Invalid, GiftCodes.Redeem(s, forged, today, items).Result);
            string later = code.Substring(0, 3) + (code[3] == 'Z' ? '2' : 'Z') + code.Substring(4);
            Assert.AreEqual(GiftCodes.Result.Invalid, GiftCodes.Redeem(s, later, today, items).Result);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(0, s.Clovers);
        }

        [Test]
        public void A_Code_Signed_With_Another_Key_Is_Refused()
        {
            GiftCodes.SetKey("someone-else");
            string code = GiftCodes.Make(GiftCodes.SignedKind.Golden10, today + 30);
            GiftCodes.SetKey(TestKey);
            Assert.AreEqual(GiftCodes.Result.Invalid, GiftCodes.Redeem(s, code, today, items).Result);
        }

        [Test]
        public void A_Signed_Code_Past_Its_Last_Day_Is_Refused()
        {
            string code = GiftCodes.Make(GiftCodes.SignedKind.Golden5, today);
            Assert.AreEqual(GiftCodes.Result.Expired, GiftCodes.Redeem(s, code, today + 1, items).Result);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(GiftCodes.Result.Ok, GiftCodes.Redeem(s, code, today, items).Result);
        }

        [Test]
        public void Without_A_Key_Signed_Codes_Are_Refused_But_The_Table_Still_Works()
        {
            string code = GiftCodes.Make(GiftCodes.SignedKind.Golden10, today + 30);
            GiftCodes.SetKey(null);
            Assert.IsFalse(GiftCodes.HasKey);
            Assert.AreEqual(GiftCodes.Result.Invalid, GiftCodes.Redeem(s, code, today, items).Result);
            Assert.AreEqual(GiftCodes.Result.Ok, GiftCodes.Redeem(s, "TRATHO-2026", today, items).Result);
            GiftCodes.SetKey("");
            Assert.IsFalse(GiftCodes.HasKey, "an empty key is no key");
        }

        [TestCase("32F9-Z9Z5", 2027, 2, 28, 10, 0)]
        [TestCase("5222-QFRM", 2026, 1, 1, 5, 2)]
        public void Codes_From_The_Shell_Tool_Pass(string code, int y, int m, int d, int golden, int clovers)
        {
            long last = GiftCodes.Day(y, m, d);
            Assert.AreEqual(code, GiftCodes.Make(golden == 10 ? GiftCodes.SignedKind.Golden10 : GiftCodes.SignedKind.Bundle, last));
            Assert.AreEqual(GiftCodes.Result.Ok, GiftCodes.Redeem(s, code, last, items).Result);
            Assert.AreEqual(golden, s.GoldenCarrots);
            Assert.AreEqual(clovers, s.Clovers);
        }

        [Test]
        public void The_Key_File_Is_Read_For_Its_Gift_Key()
        {
            Assert.AreEqual("abc123", GiftCodes.KeyFromJson("{\"giftKey\":\"abc123\"}"));
            Assert.IsNull(GiftCodes.KeyFromJson("{\"other\":1}"));
            Assert.IsNull(GiftCodes.KeyFromJson("not json"));
            Assert.IsNull(GiftCodes.KeyFromJson(null));
        }

        [Test]
        public void Used_Codes_Are_Kept_In_The_Save()
        {
            GiftCodes.Redeem(s, "TRATHO-2026", today, items);
            GiftCodes.Redeem(s, GiftCodes.Make(GiftCodes.SignedKind.Clovers2, today), today, items);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(2, r.CodesUsed.Count);
            Assert.AreEqual(GiftCodes.Result.Used, GiftCodes.Redeem(r, "tratho-2026", today, items).Result);
            Assert.AreEqual(5, r.GoldenCarrots);
            Assert.AreEqual(4, r.Clovers);
        }

        [Test]
        public void An_Old_Save_Without_Codes_Or_Mail_Loads_With_None()
        {
            GameState old = GameState.NewGame(defs);
            old.GoldenCarrots = 9;
            old.MaxDaySeen = today - 3;
            string json = SaveCodec.Encode(defs, old);
            json = System.Text.RegularExpressions.Regex.Replace(json, ",\"codesUsed\":\\[[^\\]]*\\]", "");
            json = System.Text.RegularExpressions.Regex.Replace(json, ",\"mail\":\\{.*\\}\\}$", "}");
            StringAssert.DoesNotContain("codesUsed", json);
            StringAssert.DoesNotContain("\"mail\"", json);
            LoadResult loaded = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            Assert.AreEqual(14, SaveCodec.CurrentVersion, "the save stays version 14: the new fields are additive");
            Assert.AreEqual(9, loaded.State.GoldenCarrots);
            Assert.AreEqual(0, loaded.State.CodesUsed.Count);
            Assert.AreEqual(0, loaded.State.Letters.Count);
            Assert.AreEqual(today - 3, loaded.State.MailDay, "the mailbox looks back no further than the save's own last day");
        }
    }
}
