---
name: game-design-review
description: Use when researching, reviewing or changing how Vườn Trà Thỏ Trăng looks, reads or moves - visual hierarchy, haze and overlays, HUD and in-world labels, typography, tap targets, feedback, motion timing, art direction and taste. The design session's workflow: research a standard, critique a real screenshot against it, fix with file:line, prove it with a before/after sheet.
---

# Game design review

The design standard for this game is `docs/nghien-cuu-thiet-ke-do-hoa.md` (principles N1-N12 with sources, findings, roadmap). Colours and art rules come from `docs/art-bible.md`, which wins over every skill. This skill is the workflow and the checklist; pair it with `cozy-watercolor-art` (drawing), `game-ui-ux` (layout rules), `game-feel-juice` (feedback), `unity-2d-native-rigging` (character motion) and `visual-critic-gate` (closing a task).

## 1. Workflow

1. **Look first.** Start from a real capture: `docs/anh/po-*/` or a fresh `PreviewShot` render. Never critique code you have not seen on screen.
2. **Name the principle.** Every finding cites one standard (N1-N12, or a new one with a source URL added to the report) and one observation in the image.
3. **Find the cause.** Tie it to `file:line`. Check tests that pin the behaviour (`grep` the test folder for the object name) before changing it.
4. **Fix the smallest layer that causes it.** A colour value before a new system; a new system before new art. Mark whether a fix needs art (Antigravity, `docs/anh/VE-BANG-ANTIGRAVITY.md`).
5. **Prove it.** Render before and after from the same camera, put them side by side, run `visual-critic-gate`. Record new standards in the report and the art bible, not only in code.
6. **Research when the standard is missing.** Web research on principles from polished cozy/idle mobile games and UI guidelines (Material motion, WCAG contrast, platform touch targets). Study principles only; never copy a specific game's look, characters or assets.

## 2. Checklist for any screen

| Check | Pass |
|---|---|
| Value hierarchy (N1) | Greyscale at 25%: the brightest shape is the thing to look at (rabbit at work, teapot) |
| Haze (N4) | At most 2 full-screen overlays enabled; overlays at alpha 0 are disabled; no solid object drawn with alpha |
| Locked land | No fog: an even night shade darker than the grass, no cloud shapes or light patches |
| In-world text | 4 labels or fewer on the garden view; each at least 22 units tall on the 1080 canvas |
| UI text contrast (N6) | 4.5:1 body, 3:1 for text 24+ units or bold 19+ |
| Tap targets (N8) | `UiKit.MinTouch` 88 units, 8 apart |
| Feedback (N9) | Every action: visual + varied-pitch sound + particle (Shuriken) |
| Motion (N10-N11) | Enter 250-400 ms ease-out, exit 150-200 ms ease-in, press 80-120 ms, bounce 180-250 ms OutBack, stagger 50 ms; linear only for bars and clocks |
| Characters (N12) | Animator, `.anim` clips, SpriteLibrary rig; never `Mathf.Sin` on limbs or tools in C# |
| Locked states | Deliberate silhouette with a lock or "?", text stays readable (Ink/InkSoft), not a faded alpha |
| Fonts | One bundled brand font; the OS-font fallback is a known gap (report V13) |

## 3. Taste

A moonlit watercolour storybook page: a deep, quiet night ground; warm lantern light where the eye rests; everything worth touching chubby, crisp and a little bouncy when touched. Five words: warm, calm, chubby, clear, softly alive. Texture lives inside shapes; silhouettes stay sharp with `Ink` outlines; every grounded object has an `Ink` 25-30% contact shadow.

## 4. Working in this repo

- Design work runs on its own branch and worktree beside the main checkout (`../VuonTraNam-<name>`, never under `.claude/worktrees`, which Unity ignores). Ask session B to merge ("xin gộp") when done.
- Unity runs through `/tmp/unity-gate.sh <priority> <command>`; one batch at a time while the machine charges slowly.
- `tools/conventions.py` must pass after every edit: no comments, no new procedural animation math, no new `Update()`.
