---
name: game-feel-juice
description: Use when adding or changing any player interaction in Unity - button taps, purchases, station upgrades, harvests, coins, customers arriving - or any ambient effect (dust, steam, sparkles, falling leaves). Every interaction gets tactile feedback with DOTween, varied-pitch audio and Shuriken particles.
---

# Game Feel Juice

## Every Interaction Gets All Three
| Layer | Rule |
|---|---|
| Motion | DOTween on the UI element or prop root: `DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.6f)` for taps, `DOScale` with `Ease.OutBack` for appear, a short bounce for upgrades. |
| Sound | Play through one shared feedback player that sets `pitch = Random.Range(0.95f, 1.05f)` per play, so repeated taps never sound identical. |
| Particles | A pooled Unity `ParticleSystem` (Shuriken) burst at the interaction point. |

## Rules
- Kill or complete the previous tween before starting a new one on the same target: `transform.DOKill(true)`, or `SetLink(gameObject)` so it dies with the object.
- Tween the visual child, never the `RectTransform` a layout group controls.
- Feedback listens to C# events or SO event channels raised by gameplay. Gameplay never calls feedback directly.
- Keep `Update()` empty; no hand-rolled timers for feedback.
- Never bake sparkles, dust, steam, petals or leaves into PNGs. Ambient effects are looping `ParticleSystem`s with sprite sheets via Texture Sheet Animation, in the art-bible palette.
- Pool particle systems and floating coin/text objects with `UnityEngine.Pool.ObjectPool<T>`.
- Respect the reduced-motion setting: skip scale punches, keep sound and a short fade.

## Setup Note
DOTween 1.3.045 (free) is installed as a vendored plugin in `Assets/Plugins/Demigiant/DOTween`, not through `Packages/manifest.json`. `DOTween.Modules.asmdef`, `Assets/Resources/DOTweenSettings.asset` and the `DOTWEEN` scripting define are committed, and `VuonTra.Game` references `DOTween.Modules`, so nothing has to be run in `Tools > Demigiant > DOTween Utility Panel`. Do not edit the files in that folder: the DOTween licence allows redistributing verbatim copies only.

## Done Check
- Tap a button 10 times fast: no stacked scale drift, pitch varies, particles recycle.
- `visual-critic-gate` passed on a GameView capture mid-effect.
