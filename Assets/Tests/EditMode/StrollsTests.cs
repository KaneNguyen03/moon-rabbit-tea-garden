using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>Resting rabbits stroll to empty spots of the lawn (#55): where they may go, how many at once, what they do there.</summary>
    public class StrollsTests
    {
        static Vec2 At(float x, float y) => new Vec2(x, y);

        readonly List<Vec2> spots = new List<Vec2> { At(100, 0), At(300, 0), At(0, 400), At(900, 900), At(-250, 0) };

        [Test]
        public void A_Spot_Is_Picked_Within_The_Radius_And_Never_Beside_A_Blocked_Point()
        {
            var blocked = new List<Vec2> { At(300, 120) }; // on top of the second spot
            for (int k = 0; k < 20; k++)
            {
                int pick = Strolls.PickSpot(spots, blocked, At(0, 0), 450f, k / 20.0);
                Assert.That(pick, Is.EqualTo(0).Or.EqualTo(2).Or.EqualTo(4), "roll " + k + " picked " + pick);
            }
        }

        [Test]
        public void The_Roll_Decides_And_The_Same_Roll_Picks_The_Same_Spot()
        {
            var none = new List<Vec2>();
            Assert.AreEqual(Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.37), Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.37));
            // Three spots are within reach: the roll walks through them in order.
            Assert.AreEqual(0, Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.0));
            Assert.AreEqual(2, Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.5));
            Assert.AreEqual(4, Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.999));
            Assert.AreEqual(4, Strolls.PickSpot(spots, none, At(0, 0), 450f, 1.0), "a roll of one still picks the last");
        }

        [Test]
        public void No_Spot_In_Reach_Means_No_Pick()
        {
            Assert.AreEqual(-1, Strolls.PickSpot(spots, new List<Vec2>(), At(0, 0), 50f, 0.5), "all too far");
            var onEach = new List<Vec2> { At(150, 0), At(0, 400), At(-250, 0) }; // within the clearance of every spot in reach
            Assert.AreEqual(-1, Strolls.PickSpot(spots, onEach, At(0, 0), 450f, 0.5), "every near spot stands within the clearance of a blocked point");
            Assert.AreEqual(-1, Strolls.PickSpot(new List<Vec2>(), new List<Vec2>(), At(0, 0), 450f, 0.5), "no spots at all");
        }

        [Test]
        public void The_Path_To_A_Spot_Keeps_Clear_Of_Blocked_Points()
        {
            // The spot at (0, 400) is clear itself, but a station stands halfway along the way to it.
            var blocked = new List<Vec2> { At(40, 200) };
            for (int k = 0; k < 20; k++)
                Assert.AreNotEqual(2, Strolls.PickSpot(spots, blocked, At(0, 0), 450f, k / 20.0), "the way is blocked");
            Assert.AreEqual(0, Strolls.PickSpot(new List<Vec2> { At(0, 400) }, new List<Vec2> { At(0, 0) }, At(0, 0), 450f, 0.5),
                "a blocked point at the start itself does not count against the way out");
        }

        [Test]
        public void At_Most_One_Rabbit_Out_For_Every_Three_Empty_Spots()
        {
            Assert.AreEqual(0, Strolls.MaxAtOnce(0));
            Assert.AreEqual(0, Strolls.MaxAtOnce(2));
            Assert.AreEqual(1, Strolls.MaxAtOnce(3));
            Assert.AreEqual(1, Strolls.MaxAtOnce(5));
            Assert.AreEqual(3, Strolls.MaxAtOnce(11));
        }

        [Test]
        public void Fireflies_Are_Chased_Only_At_Night_And_Every_Activity_Comes_Up()
        {
            var day = new HashSet<StrollActivity>();
            var night = new HashSet<StrollActivity>();
            for (int k = 0; k < 30; k++)
            {
                day.Add(Strolls.ActivityFor(k / 30.0, false));
                night.Add(Strolls.ActivityFor(k / 30.0, true));
            }
            Assert.IsFalse(day.Contains(StrollActivity.Firefly), "no fireflies by day");
            Assert.IsTrue(day.Contains(StrollActivity.Tea) && day.Contains(StrollActivity.Doze));
            Assert.IsTrue(night.Contains(StrollActivity.Firefly) && night.Contains(StrollActivity.Tea) && night.Contains(StrollActivity.Doze));
            Assert.AreEqual(Strolls.ActivityFor(0.2, true), Strolls.ActivityFor(0.2, true), "the same roll, the same thing");
        }

        [Test]
        public void A_Roll_Is_The_Same_For_The_Same_Rabbit_And_Stroll_And_Differs_Between_Them()
        {
            double a = Strolls.Roll(0, 3, 0);
            Assert.AreEqual(a, Strolls.Roll(0, 3, 0));
            Assert.GreaterOrEqual(a, 0d); Assert.Less(a, 1d);
            Assert.AreNotEqual(a, Strolls.Roll(0, 4, 0), "the next stroll rolls again");
            Assert.AreNotEqual(a, Strolls.Roll(1, 3, 0), "and so does the next rabbit");
            Assert.AreNotEqual(a, Strolls.Roll(0, 3, 1), "and the next question");
        }

        [Test]
        public void The_Rest_At_The_Spot_Lasts_Four_To_Six_Seconds()
        {
            Assert.AreEqual(4f, Strolls.LingerFor(0.0), 1e-5);
            Assert.AreEqual(6f, Strolls.LingerFor(1.0), 1e-5);
            Assert.AreEqual(5f, Strolls.LingerFor(0.5), 1e-5);
        }
    }
}
