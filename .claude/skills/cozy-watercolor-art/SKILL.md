---
name: cozy-watercolor-art
description: Use when drawing, generating, importing or placing 2D art or HUD for Vườn Trà Thỏ Trăng (the bunny tea & mochi idle garden) — keeps new art in the hand-drawn watercolor/gouache storybook look, on the art bible's colours, grounded in the scene, and imported the same way as the art already in the game.
---

# Cozy watercolor storybook art

The colours come from `docs/art-bible.md` and `Assets/VuonTra/Game/UI/Palette.cs` and nowhere else. If this file and the bible disagree, the bible wins; change colours there (and in `Palette.cs`), never only here.

## 1. Look

- Hand-drawn gouache and watercolor storybook illustration: soft edges, chubby rounded shapes, gentle painterly shading, warm moonlit night mood.
- Outlines: thin organic **purple ink** (`Ink #3A2A4D`) with slight width variation. Never pure black.
- Colours (bible names):
  - Ground / grass: `Grass #3D8369`; positive/progress: `Moss #6FBF8A`. Never cold neon green.
  - Sky and dark overlays: `Night #1B1E45`.
  - UI cards: `Palette.Card` (paper mixed toward white) with a `CardLip` edge — cream paper cards with a thick candy-button lip.
  - Lantern / furnace glow: `Lantern #FFC866` into `Carrot #FF9B4A`.
  - Locked land: **no fog** (owner, 25 Nov: it hurt the eyes). The land just lies in night shade: one even, soft layer of `Night #1B1E45` mixed a quarter toward `Grass #3D8369`, feathered edges, no cloud shapes, no light patches, no outline. Never white, cream or light grey.
  - Accent: `Blush #F6A5B8`; anything tappable: `Carrot #FF9B4A`.

## 2. Grounding in the garden scene (URP 2D renderer)

- Every station (mortar, teapot, furnace, pot), tree and lamp post gets a soft elliptical contact shadow at its base: `Ink` at 25–30% opacity, blurred edges.
- Each interactive station sits on its own painted pad (dirt patch, wooden deck or wildflower tuft) so it separates from the grass.
- A seamless paper-grain overlay in `Ink` only, at 6–8%. uGUI has no Multiply blend, so a cream branch lays milk over the whole scene.
- At most two full-screen overlays (vignette, grain). Disable any overlay whose alpha is 0. Solid things (stones, paths, locked props) use pre-mixed solid colours, never alpha.
- Warm 2D point lights on lanterns and the furnace; light particles only (steam over teapot/pots, a few fireflies).

## 3. HUD

- No dashed wireframe placeholders (e.g. the "Xếp thỏ +" slot): use a woven mat, a wooden pedestal or a soft glowing ground ring.
- Top-HUD icons fill 65–70% of their container height, stroke weight matching the `Ink` outlines.
- Text and colour changes in the HUD still follow the existing layout tests (both languages, large text).

## 4. Making and importing art

1. Draw with Antigravity as in `docs/anh/VE-BANG-ANTIGRAVITY.md` (sheet on flat `#2E5E49`, cut with `docs/anh/po-do-vat/cat-luoi.py`), with `docs/anh/po-do-vat/_tong-hop.png` as style reference. Add to the prompt: `hand-drawn watercolor and gouache storybook game prop, soft painterly shading, thin purple ink outlines (#3A2A4D), subtle paper grain, no text, no ground shadow`.
2. Check the cut PNG for coloured fringe from the background along the edges (defringe) before importing.
3. PNG sides in multiples of 4 px. Import as Sprite, `Filter Mode = Bilinear` (as the art in `Assets/VuonTra/Art/Final` is), compression High Quality (or ASTC 6x6 per platform override).
4. Pivot: **new** world props that stand on the ground use Bottom Center. Existing sprites use Center; don't re-pivot one without checking every place it is positioned.
