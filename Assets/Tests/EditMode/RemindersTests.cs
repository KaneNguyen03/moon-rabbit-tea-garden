using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// The device reminders (#65): what is planned when the game goes to the background. Every test passes the local
    /// time and its offset, so none depends on the zone of the machine running it.
    /// </summary>
    public class RemindersTests
    {
        const int VietNam = 7 * 3600;
        const int NewYork = -5 * 3600;

        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        static long At(int y, int mo, int d, int h, int mi = 0, int offset = VietNam) =>
            new DateTimeOffset(y, mo, d, h, mi, 0, TimeSpan.FromSeconds(offset)).ToUnixTimeSeconds();

        static long LocalDay(long unix, int offset) => (long)Math.Floor((unix + offset) / 86400d);

        List<Reminder> Plan(long now, int offset = VietNam, bool on = true, Modifiers? mods = null) =>
            Reminders.Plan(s, now, offset, mods ?? Modifiers.None, on);

        static Reminder Only(List<Reminder> list, ReminderKind kind)
        {
            Reminder found = default;
            int n = 0;
            foreach (Reminder r in list) if (r.Kind == kind) { found = r; n++; }
            Assert.AreEqual(1, n, "one " + kind + " reminder in " + Describe(list));
            return found;
        }

        static bool Has(List<Reminder> list, ReminderKind kind)
        {
            foreach (Reminder r in list) if (r.Kind == kind) return true;
            return false;
        }

        static string Describe(List<Reminder> list)
        {
            var parts = new List<string>();
            foreach (Reminder r in list) parts.Add(r.Kind + "@" + DateTimeOffset.FromUnixTimeSeconds(r.AtUnixSeconds).ToOffset(TimeSpan.FromHours(7)).ToString("MM-dd HH:mm"));
            return "[" + string.Join(", ", parts) + "]";
        }

        [Test]
        public void A_New_Save_Gets_The_Tray_The_Gift_The_Miss_You_And_The_Next_Season()
        {
            long now = At(2026, 5, 5, 10);
            s.LastSeenUnixSeconds = now;
            List<Reminder> list = Plan(now);

            Assert.AreEqual(4, list.Count, Describe(list));
            Assert.AreEqual(At(2026, 5, 5, 13), Only(list, ReminderKind.TrayFull).AtUnixSeconds, "three hours on: the away cap");
            Assert.AreEqual(At(2026, 5, 6, 9), Only(list, ReminderKind.DailyGift).AtUnixSeconds, "nine the next morning");
            Assert.AreEqual(At(2026, 5, 8, 10), Only(list, ReminderKind.MissYou).AtUnixSeconds, "three days on");
            Reminder season = Only(list, ReminderKind.SeasonBegins);
            Assert.AreEqual(At(2026, 9, 1, 9), season.AtUnixSeconds, "Mid-Autumn's first day at nine");
            Assert.AreEqual(Season.MidAutumn, season.Season);
            for (int i = 1; i < list.Count; i++) Assert.Less(list[i - 1].AtUnixSeconds, list[i].AtUnixSeconds, "in time order");
        }

        [Test]
        public void Each_Reminder_Has_Its_Own_Text_Key()
        {
            long now = At(2026, 5, 5, 10);
            s.LastSeenUnixSeconds = now;
            var keys = new HashSet<string>();
            foreach (Reminder r in Plan(now)) keys.Add(r.Key);
            CollectionAssert.AreEquivalent(new[] { "remind.tray", "remind.gift", "remind.season", "remind.missyou" }, keys);
        }

        [Test]
        public void A_Save_That_Has_Hit_The_Away_Cap_Gets_No_Tray_Reminder()
        {
            long now = At(2026, 5, 5, 10);
            s.LastSeenUnixSeconds = now - 4 * 3600; // last seen at six: the three hours ran out at nine
            List<Reminder> list = Plan(now);

            Assert.IsFalse(Has(list, ReminderKind.TrayFull), "the tray is full already: " + Describe(list));
            Assert.AreEqual(At(2026, 5, 6, 9), Only(list, ReminderKind.DailyGift).AtUnixSeconds);
            Assert.AreEqual(At(2026, 5, 8, 9), Only(list, ReminderKind.MissYou).AtUnixSeconds, "three days from the last visit, not from now");
            foreach (Reminder r in list) Assert.Greater(r.AtUnixSeconds, now, "nothing in the past");
        }

        [Test]
        public void The_Long_Sleep_Charm_Moves_The_Tray_Reminder()
        {
            long now = At(2026, 5, 5, 10);
            s.LastSeenUnixSeconds = now;
            Modifiers mods = Modifiers.None;
            mods.AwayCapSeconds += 2 * 3600; // one level of the charm
            Assert.AreEqual(At(2026, 5, 5, 15), Only(Plan(now, mods: mods), ReminderKind.TrayFull).AtUnixSeconds);
        }

        [Test]
        public void A_Reminder_That_Falls_In_The_Night_Waits_For_Nine()
        {
            long now = At(2026, 5, 5, 20);
            s.LastSeenUnixSeconds = now;
            s.GiftDay = LocalDay(now, VietNam); // today's gift taken, so the gift is tomorrow at nine too
            List<Reminder> list = Plan(now);
            // The tray fills at 23:00; quiet hours move it to 9:00, where the gift is: the gift wins.
            Assert.IsFalse(Has(list, ReminderKind.TrayFull), Describe(list));
            Assert.AreEqual(At(2026, 5, 6, 9), Only(list, ReminderKind.DailyGift).AtUnixSeconds);

            now = At(2026, 5, 5, 15);
            s.LastSeenUnixSeconds = now;
            Assert.AreEqual(At(2026, 5, 5, 18), Only(Plan(now), ReminderKind.TrayFull).AtUnixSeconds, "in the day it keeps its hour");
        }

        [Test]
        public void The_Gift_Is_This_Morning_When_The_Player_Leaves_Before_Nine_Without_Taking_It()
        {
            long now = At(2026, 5, 5, 6);
            s.LastSeenUnixSeconds = now;
            Assert.AreEqual(At(2026, 5, 5, 9), Only(Plan(now), ReminderKind.DailyGift).AtUnixSeconds);

            s.GiftDay = LocalDay(now, VietNam);
            Assert.AreEqual(At(2026, 5, 6, 9), Only(Plan(now), ReminderKind.DailyGift).AtUnixSeconds, "taken already: tomorrow");
        }

        [Test]
        public void Nine_Is_Nine_On_The_Players_Own_Clock()
        {
            long now = At(2026, 5, 5, 10, 0, NewYork);
            s.LastSeenUnixSeconds = now;
            Assert.AreEqual(At(2026, 5, 6, 9, 0, NewYork), Only(Plan(now, NewYork), ReminderKind.DailyGift).AtUnixSeconds);
        }

        [Test]
        public void On_The_Eve_Of_A_Season_Its_First_Morning_Wins_Over_The_Gift_And_The_Tray()
        {
            long now = At(2026, 8, 31, 20);
            s.LastSeenUnixSeconds = now;
            List<Reminder> list = Plan(now);
            Reminder season = Only(list, ReminderKind.SeasonBegins);
            Assert.AreEqual(At(2026, 9, 1, 9), season.AtUnixSeconds);
            Assert.AreEqual(Season.MidAutumn, season.Season);
            Assert.IsFalse(Has(list, ReminderKind.DailyGift), "the gift's 9:00 is the season's: " + Describe(list));
            Assert.IsFalse(Has(list, ReminderKind.TrayFull), "the tray waited for 9:00 too");
            Assert.AreEqual(At(2026, 9, 3, 20), Only(list, ReminderKind.MissYou).AtUnixSeconds);
        }

        [Test]
        public void On_A_Seasons_First_Day_Before_Nine_The_Reminder_Is_This_Morning()
        {
            long now = At(2026, 12, 1, 7);
            s.LastSeenUnixSeconds = now;
            Reminder season = Only(Plan(now), ReminderKind.SeasonBegins);
            Assert.AreEqual(At(2026, 12, 1, 9), season.AtUnixSeconds);
            Assert.AreEqual(Season.Christmas, season.Season);

            // Greeted already this morning: the next one is Tết's, on 15 January.
            s.SeasonGreetedDay = LocalDay(now, VietNam);
            season = Only(Plan(now), ReminderKind.SeasonBegins);
            Assert.AreEqual(At(2027, 1, 15, 9), season.AtUnixSeconds);
            Assert.AreEqual(Season.Tet, season.Season);
        }

        [Test]
        public void No_Two_Reminders_Come_Within_Four_Hours_And_None_Is_In_The_Past()
        {
            foreach (int offset in new[] { VietNam, NewYork, 0 })
            {
                long start = At(2026, 1, 1, 0, 0, offset);
                for (long now = start; now < start + 366L * 86400; now += 3600 * 5 + 1234)
                {
                    s = GameState.NewGame(defs);
                    s.LastSeenUnixSeconds = now - (now % 7) * 3600; // sometimes just now, sometimes hours ago
                    if (now % 3 == 0) s.GiftDay = LocalDay(now, offset);
                    List<Reminder> list = Plan(now, offset);
                    Assert.LessOrEqual(list.Count, Reminders.MaxCount);
                    Assert.Greater(list.Count, 0, "something is always ahead");
                    for (int i = 0; i < list.Count; i++)
                    {
                        Assert.Greater(list[i].AtUnixSeconds, now);
                        for (int j = i + 1; j < list.Count; j++)
                            Assert.GreaterOrEqual(Math.Abs(list[j].AtUnixSeconds - list[i].AtUnixSeconds), Reminders.MinGapSeconds,
                                "offset " + offset + " now " + now + ": " + Describe(list));
                    }
                }
            }
        }

        [Test]
        public void A_Clash_Keeps_The_More_Important_Reminder()
        {
            // Leaves at 7:00 on the first of September: the season at 9:00, the tray at 10:00, the gift at 9:00.
            long now = At(2026, 9, 1, 7);
            s.LastSeenUnixSeconds = now;
            List<Reminder> list = Plan(now);
            Assert.AreEqual(ReminderKind.SeasonBegins, list[0].Kind, Describe(list));
            Assert.IsFalse(Has(list, ReminderKind.DailyGift));
            Assert.IsFalse(Has(list, ReminderKind.TrayFull));
            Assert.IsTrue(Has(list, ReminderKind.MissYou));
            Assert.Less((int)ReminderKind.SeasonBegins, (int)ReminderKind.DailyGift, "importance: season, gift, tray, miss-you");
            Assert.Less((int)ReminderKind.DailyGift, (int)ReminderKind.TrayFull);
            Assert.Less((int)ReminderKind.TrayFull, (int)ReminderKind.MissYou);
        }

        [Test]
        public void Switched_Off_The_List_Is_Empty()
        {
            long now = At(2026, 8, 31, 10);
            s.LastSeenUnixSeconds = now;
            Assert.IsEmpty(Plan(now, on: false));
            Assert.IsEmpty(Reminders.Plan(null, now, VietNam, Modifiers.None, true), "no garden, nothing to say");
        }

        [Test]
        public void A_Clock_Set_Back_Plans_From_The_Day_The_Game_Believes_In()
        {
            long now = At(2026, 5, 5, 10);
            s.LastSeenUnixSeconds = now;
            s.MaxDaySeen = LocalDay(At(2026, 5, 10, 10), VietNam);
            s.GiftDay = s.MaxDaySeen;
            Assert.AreEqual(At(2026, 5, 11, 9), Only(Plan(now), ReminderKind.DailyGift).AtUnixSeconds, "the gift a clock set back can actually get");
        }
    }
}
