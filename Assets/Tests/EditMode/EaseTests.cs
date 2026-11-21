using NUnit.Framework;
using VuonTra.Game.UI;

namespace VuonTra.Core.Tests
{
    public class EaseTests
    {
        [Test]
        public void Curves_Start_At_Zero_And_End_At_One()
        {
            foreach (System.Func<float, float> curve in new System.Func<float, float>[] { Ease.OutCubic, Ease.OutBack, Ease.OutElastic })
            {
                Assert.AreEqual(0f, curve(0f), 1e-4f);
                Assert.AreEqual(1f, curve(1f), 1e-4f);
            }
            Assert.Greater(Ease.OutBack(0.7f), 1f, "OutBack overshoots");
        }

        [Test]
        public void Tween_Runs_After_Its_Delay_And_Finishes_Exactly()
        {
            var tweens = new Tweens();
            float value = -1f;
            bool done = false;
            tweens.Add(1f, t => value = t, Ease.Linear, delay: 0.5f, done: () => done = true);

            tweens.Update(0.4f);
            Assert.AreEqual(-1f, value, "still waiting");
            tweens.Update(0.2f);
            tweens.Update(0.5f);
            Assert.AreEqual(0.5f, value, 1e-4f);
            tweens.Update(5f);
            Assert.AreEqual(1f, value);
            Assert.IsTrue(done);
            Assert.AreEqual(0, tweens.Count);
        }

        [Test]
        public void A_Tween_May_Start_Another_When_It_Finishes()
        {
            var tweens = new Tweens();
            float second = 0f;
            tweens.Add(0.1f, _ => { }, done: () => tweens.Add(0.1f, t => second = t, Ease.Linear));
            tweens.Update(1f);
            tweens.Update(1f);
            Assert.AreEqual(1f, second);
        }
    }
}
