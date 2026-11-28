using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class PersonalityTests
    {
        const string AssetPath = "Assets/VuonTra/Content/Rabbits/Personalities.asset";
        const string StationFolder = "Assets/VuonTra/Content/Stations";

        static string ProjectRoot()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                    if (File.Exists(Path.Combine(dir.FullName, AssetPath))) return dir.FullName;
            Assert.Fail("no checkout above " + Directory.GetCurrentDirectory() + " holds " + AssetPath);
            return null;
        }

        static readonly Regex EntryLine = new Regex(@"^\s*-?\s*(Kind|Temper|Favourite):\s*(.*?)\s*$");

        static List<Liking> ReadAsset(string text)
        {
            var likings = new List<Liking>();
            Liking current = default;
            bool open = false;
            foreach (string line in text.Split('\n'))
            {
                Match m = EntryLine.Match(line);
                if (!m.Success) continue;
                string field = m.Groups[1].Value, value = m.Groups[2].Value;
                if (field == "Kind")
                {
                    if (open) likings.Add(current);
                    current = new Liking { Kind = value };
                    open = true;
                }
                else if (field == "Temper") current.Personality = (Personality)int.Parse(value);
                else current.Favourite = value;
            }
            if (open) likings.Add(current);
            return likings;
        }

        static readonly Regex StationField = new Regex(@"^\s*(Id|Kind):\s*(\S+)\s*$");

        static List<StationDef> ReadStations(string root)
        {
            var defs = new List<StationDef>();
            foreach (string file in Directory.GetFiles(Path.Combine(root, StationFolder), "*.asset"))
            {
                var def = new StationDef();
                foreach (string line in File.ReadAllLines(file))
                {
                    Match m = StationField.Match(line);
                    if (!m.Success) continue;
                    if (m.Groups[1].Value == "Id") def.Id = m.Groups[2].Value;
                    else def.Kind = (StationKind)int.Parse(m.Groups[2].Value);
                }
                defs.Add(def);
            }
            return defs;
        }

        static List<Liking> AssetLikings() => ReadAsset(File.ReadAllText(Path.Combine(ProjectRoot(), AssetPath)));

        [Test]
        public void Every_Kind_Has_Exactly_One_Personality_And_Loves_A_Real_Brewing_Station()
        {
            string root = ProjectRoot();
            List<StationDef> stations = ReadStations(root);
            Assert.GreaterOrEqual(stations.Count(d => d.Kind == StationKind.Brew), 9, "the brewing stations were read");
            List<Liking> likings = AssetLikings();
            Assert.AreEqual(RabbitKinds.All.Count, likings.Count, "one row per kind");
            List<string> problems = new PersonalityBook(likings).Problems(RabbitKinds.All, stations);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            foreach (Personality p in Personalities.All)
                Assert.IsTrue(likings.Any(l => l.Personality == p), p + " belongs to at least one kind");
        }

        [Test]
        public void A_Kind_Missing_From_The_Asset_Or_Listed_Twice_Or_Loving_No_Brewing_Station_Turns_The_Check_Red()
        {
            List<StationDef> stations = ReadStations(ProjectRoot());
            List<Liking> likings = AssetLikings();

            var missing = likings.Where(l => l.Kind != Rabbits.FirstKind).ToList();
            CollectionAssert.Contains(new PersonalityBook(missing).Problems(RabbitKinds.All, stations), Rabbits.FirstKind + " has no personality");

            var twice = new List<Liking>(likings) { new Liking { Kind = Rabbits.FirstKind, Personality = Personality.Shy, Favourite = "am-tra" } };
            CollectionAssert.Contains(new PersonalityBook(twice).Problems(RabbitKinds.All, stations), Rabbits.FirstKind + " has more than one personality");

            var wrong = likings.Select(l => l.Kind == Rabbits.FirstKind ? new Liking { Kind = l.Kind, Personality = l.Personality, Favourite = "gio-la" } : l).ToList();
            CollectionAssert.Contains(new PersonalityBook(wrong).Problems(RabbitKinds.All, stations), Rabbits.FirstKind + " loves gio-la, which is not a brewing station");

            var stranger = new List<Liking>(likings) { new Liking { Kind = "tho-khong-co", Personality = Personality.Shy, Favourite = "am-tra" } };
            CollectionAssert.Contains(new PersonalityBook(stranger).Problems(RabbitKinds.All, stations), "tho-khong-co is not a rabbit kind");
        }

        static Liking LikingOf(string kind, Personality p, string favourite = "tea") => new Liking { Kind = kind, Personality = p, Favourite = favourite };

        [Test]
        public void A_Working_And_A_Resting_Rabbit_Both_Have_Their_Personality_Lines_And_The_Trade_Lines_Stay()
        {
            Liking like = LikingOf("tho-bong", Personality.Sleepy);
            string[] common = { "chatter.temper.Sleepy.0", "chatter.temper.Sleepy.1", "chatter.temper.Sleepy.2" };
            foreach (bool resting in new[] { false, true })
            {
                List<string> keys = Chatter.KeysFor(RabbitTrade.Brewer, resting, DayPhase.Day, Season.None, false, false, like, 0);
                foreach (string key in common) CollectionAssert.Contains(keys, key, resting ? "resting" : "working");
                Assert.AreEqual(Personalities.CommonLines, keys.Count(k => k.StartsWith("chatter.temper.")), "only its own personality's lines");
                Assert.AreEqual(resting ? 0 : Chatter.TradeLines, keys.Count(Chatter.IsTradeLine), "the trade lines stay for work");
                CollectionAssert.Contains(keys, "chatter.any.0");
            }
            List<string> plain = Chatter.KeysFor(RabbitTrade.Brewer, false, DayPhase.Day, Season.None, false, false);
            Assert.IsFalse(plain.Any(k => k.StartsWith("chatter.temper.") || k.StartsWith("chatter.kind.")), "no personality, no personality lines");
        }

        [Test]
        public void The_Three_Heart_Line_Joins_Only_From_Three_Hearts()
        {
            Liking like = LikingOf("tho-me", Personality.Hungry);
            for (int hearts = 0; hearts <= Rabbits.HeartMax; hearts++)
            foreach (bool resting in new[] { false, true })
            {
                List<string> keys = Chatter.KeysFor(RabbitTrade.Baker, resting, DayPhase.Night, Season.None, false, false, like, hearts);
                Assert.AreEqual(hearts >= Rabbits.LineHearts, keys.Contains("chatter.temper.Hungry.heart"), hearts + " hearts, resting " + resting);
            }
            Assert.AreEqual(3, Rabbits.LineHearts);
        }

        [Test]
        public void Kinds_Of_Three_Stars_And_Up_Have_One_Line_Of_Their_Own()
        {
            int own = 0;
            foreach (RabbitKind kind in RabbitKinds.All)
            {
                List<string> keys = Chatter.KeysFor(kind.Trade, false, DayPhase.Day, Season.None, false, false, LikingOf(kind.Id, Personality.Dreamy), 0);
                bool has = keys.Contains("chatter.kind." + kind.Id);
                Assert.AreEqual(kind.Tier >= Personalities.OwnLineFromTier, has, kind.Id);
                Assert.AreEqual(has ? 1 : 0, keys.Count(k => k.StartsWith("chatter.kind.")), kind.Id);
                if (has) own++;
            }
            Assert.AreEqual(49, own);
            Assert.AreEqual(8 * (Personalities.CommonLines + 1) + own, Personalities.AllKeys.Count());
            Assert.AreEqual(Personalities.AllKeys.Count(), Personalities.AllKeys.Distinct().Count());
        }

        static (GameState, Rabbit, List<StationDef>, PersonalityBook) Garden(string favourite)
        {
            List<StationDef> defs = Fixture.TradeDefs();
            GameState s = GameState.NewGame(defs);
            s.Levels[0] = s.Levels[2] = 1;
            var r = new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 };
            s.Rabbits.Add(r);
            var book = new PersonalityBook(new[] { LikingOf("tho-bong", Personality.Sleepy, favourite) });
            return (s, r, defs, book);
        }

        [Test]
        public void The_First_Stroke_Of_The_Day_At_The_Favourite_Station_Gives_Two_More_Bond_Points()
        {
            var (s, r, defs, book) = Garden("tea");
            Assert.IsTrue(book.LovesStation(defs, r));
            PetResult first = Personalities.Pet(defs, book, s, r, 1);
            Assert.IsTrue(first.FirstToday);
            Assert.AreEqual(Rabbits.PetPoints + 2, r.Affection, "PetPoints + 2 at the station it loves");
            Assert.AreEqual(2, Personalities.FavouritePoints);
            Assert.AreEqual(Rabbits.PetEnergy, first.Energy, "tea power is the same as anywhere");
            Personalities.Pet(defs, book, s, r, 1);
            Assert.AreEqual(Rabbits.PetPoints + 2, r.Affection, "the second stroke of the day gives nothing more");
            Personalities.Pet(defs, book, s, r, 2);
            Assert.AreEqual(2 * (Rabbits.PetPoints + 2), r.Affection, "and the next day again");
        }

        [Test]
        public void A_Stroke_At_Another_Station_Or_Resting_Gives_The_Usual_Points()
        {
            var (s, r, defs, book) = Garden("jam");
            Assert.IsFalse(book.LovesStation(defs, r));
            Personalities.Pet(defs, book, s, r, 1);
            Assert.AreEqual(Rabbits.PetPoints, r.Affection, "PetPoints at a station it does not love");

            var (s2, resting, defs2, book2) = Garden("tea");
            resting.Station = -1;
            Personalities.Pet(defs2, book2, s2, resting, 1);
            Assert.AreEqual(Rabbits.PetPoints, resting.Affection, "PetPoints while resting");

            var (s3, nobody, defs3, _) = Garden("tea");
            Personalities.Pet(defs3, PersonalityBook.Empty, s3, nobody, 1);
            Assert.AreEqual(Rabbits.PetPoints, nobody.Affection, "PetPoints when no personality is known");
        }

        [Test]
        public void The_Two_Extra_Points_Still_Cross_A_Heart_Once()
        {
            var (s, r, defs, book) = Garden("tea");
            r.Affection = Rabbits.PointsFor(Rabbits.LineHearts) - Rabbits.PetPoints - 1;
            PetResult pet = Personalities.Pet(defs, book, s, r, 1);
            Assert.IsTrue(pet.LineUnlocked, "the two extra points carry it over three hearts");
            Assert.AreEqual(Rabbits.LineHearts, Rabbits.HeartsOf(r));
            r.Affection = Rabbits.PointsFor(Rabbits.GiftHearts) - Rabbits.PetPoints - 1;
            int clovers = s.Clovers;
            Assert.IsTrue(Personalities.Pet(defs, book, s, r, 2).GiftGiven);
            Assert.AreEqual(clovers + Rabbits.AffectionGiftClovers, s.Clovers, "one gift, not two");
        }
    }
}
