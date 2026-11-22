using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class DaylightTests
    {
        [TestCase(0.0, DayPhase.Night)]
        [TestCase(4.99, DayPhase.Night)]
        [TestCase(5.0, DayPhase.Dawn)]
        [TestCase(6.5, DayPhase.Dawn)]
        [TestCase(7.0, DayPhase.Day)]
        [TestCase(13.0, DayPhase.Day)]
        [TestCase(17.0, DayPhase.Dusk)]
        [TestCase(18.9, DayPhase.Dusk)]
        [TestCase(19.0, DayPhase.Night)]
        [TestCase(23.5, DayPhase.Night)]
        [TestCase(25.0, DayPhase.Night)]
        [TestCase(-1.0, DayPhase.Night)]
        public void The_Hour_Names_The_Phase(double hour, DayPhase phase)
        {
            Assert.AreEqual(phase, Daylight.PhaseAt(hour));
        }

        [Test]
        public void Night_Is_Full_At_Night_Never_Below_The_Floor_And_Ramps_Through_Dusk_And_Dawn()
        {
            Assert.AreEqual(1d, Daylight.NightAt(22), 1e-9);
            Assert.AreEqual(Daylight.DayFloor, Daylight.NightAt(12), 1e-9, "a moon garden still, even at noon");
            Assert.AreEqual(Daylight.DayFloor, Daylight.NightAt(17), 1e-9);
            Assert.AreEqual((Daylight.DayFloor + 1d) / 2d, Daylight.NightAt(18), 1e-9, "halfway through dusk");
            Assert.AreEqual(1d, Daylight.NightAt(5), 1e-9);
            Assert.AreEqual((Daylight.DayFloor + 1d) / 2d, Daylight.NightAt(6), 1e-9, "halfway through dawn");
            for (double h = 0; h < 24; h += 0.25)
            {
                Assert.GreaterOrEqual(Daylight.NightAt(h), Daylight.DayFloor - 1e-9, "hour " + h);
                Assert.LessOrEqual(Daylight.NightAt(h), 1d + 1e-9, "hour " + h);
            }
        }

        [Test]
        public void Fireflies_Are_All_Out_At_Night_Half_In_The_Half_Light_And_A_Few_By_Day()
        {
            Assert.AreEqual(22, Daylight.FirefliesAt(23, 22));
            Assert.AreEqual(11, Daylight.FirefliesAt(18, 22));
            Assert.AreEqual(11, Daylight.FirefliesAt(6, 22));
            Assert.AreEqual(3, Daylight.FirefliesAt(12, 22));
            Assert.AreEqual(2, Daylight.FirefliesAt(12, 4), "never fewer than two");
        }
    }
}
