using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace VuonTra.Core.Tests
{
    public class DOTweenInstallTests
    {
        [Test]
        public void A_Manual_Fade_From_The_UI_Module_Reaches_Half_Then_Its_End()
        {
            var faded = new GameObject("faded").AddComponent<CanvasGroup>();
            try
            {
                Tween fade = faded.DOFade(0f, 1f).SetEase(DG.Tweening.Ease.Linear).SetUpdate(UpdateType.Manual);

                DOTween.ManualUpdate(0.5f, 0.5f);
                Assert.AreEqual(0.5f, faded.alpha, 1e-3f);
                Assert.IsTrue(fade.IsActive(), "still running at the halfway point");

                DOTween.ManualUpdate(0.5f, 0.5f);
                Assert.AreEqual(0f, faded.alpha, 1e-3f);
                Assert.IsFalse(fade.IsActive(), "a finished tween is killed");
            }
            finally
            {
                DOTween.Kill(faded);
                Object.DestroyImmediate(faded.gameObject);
            }
        }
    }
}
