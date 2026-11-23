---
name: unity-2d-native-rigging
description: Use when animating, posing, skinning or customizing a 2D character or prop in Unity (rabbits, hats, outfits, work loops). Forces Unity's native 2D Animation tools - SpriteLibrary, SpriteResolver, SortingGroup, Sprite Skin bones and Animator Controller - and forbids moving limbs from C#.
---

# Unity 2D Native Rigging

## Forbidden
- C# that writes `transform.localPosition`, `localRotation`, `localScale` or `eulerAngles` to animate limbs, heads, ears, props or poses.
- `Mathf.Sin`, `Mathf.Cos`, `Mathf.PingPong`, `Lerp`, `Slerp` or coroutines used as an animation timeline.
- DOTween on character body parts. DOTween is for UI and feedback only.
- One prefab per skin, hat or outfit.
- Pasting a worn item at a fixed offset shared by every head shape.

## Required
1. **Rig:** Import the character as a layered PSB or a sprite set. Bones via Sprite Editor > Skinning Editor; each part gets a `SpriteSkin`.
2. **Skins:** One `SpriteLibraryAsset` per character family. Categories = body slots (`Head`, `Ears`, `Hat`, `Body`, `HandL`, `HandR`, `Tool`). Labels = variants. A skin change is `SpriteResolver.SetCategoryAndLabel`, driven by data in a `ScriptableObject`.
3. **Variants:** Skin variants are `SpriteLibraryAsset` variants (main library override), never prefab copies.
4. **Ordering:** `SortingGroup` on the character root. Part order lives in each `SpriteRenderer.sortingOrder` inside the group.
5. **Attachments:** Hats and tools sit on a bone or an anchor child authored per head kind, so each head keeps its own fit.
6. **Motion:** Every motion is an `.anim` clip keyed on bones or `SpriteResolver` labels. States and transitions live in an `Animator Controller`; variants share it through `AnimatorOverrideController`.
7. **Code's only job:** set Animator parameters (`SetTrigger`, `SetBool`, `SetFloat`) and resolver labels in response to events.

## Done Check
- `grep -rnE "local(Position|Rotation|Scale)|eulerAngles|Mathf\.(Sin|Cos|PingPong)" Assets/Scripts` shows no animation use on character parts.
- Every new skin is a library label, not a prefab.
- `visual-critic-gate` passed on a sheet showing every skin in every state.
