---
name: visual-critic-gate
description: Use before reporting any visual, UI, art or animation task as done. Build a QA preview sheet or capture the GameView, have a vision model grade it against a fixed checklist, and only report done when the verdict is APPROVED.
---

# Visual Critic Gate

A visual task is not done until this gate returns `APPROVED`.

## 1. Build The Evidence
Pick what fits the task:
- **Assets / animation frames:** a `preview_qa_sheet.png` made with Python Pillow: every frame or variant on a grid, each cell shown on three backgrounds (checkerboard, the scene's lawn colour, dark `#2B2420`), plus one cell at 400% nearest-neighbour zoom on the worst edge. Label each cell.
- **In-game result:** a GameView screenshot at the target resolution (via `unity-mcp-orchestrator` or `tools/` scripts), plus a 200% crop of the changed area.
- **Animation:** a GIF and a sheet of its key frames side by side.

Save under `docs/anh/qa/<task>/`.

## 2. Grade It
Send the evidence to a vision model (read the image yourself, or the Antigravity image subagent) with this checklist and nothing that hints at the expected answer:

1. Joints and seams: no gaps, overlaps or floating parts between limbs, hats, ears, tools.
2. Alignment: parts sit on their anchors in every frame and variant; no jitter between frames.
3. Palette: colours match `docs/art-bible.md`; no pure `#000000` lines; same outline colour and weight on every part.
4. Alpha: no white or grey halo, no fringe, no stray pixels or background remnants.
5. Baked effects: no sparkles, dust, steam or leaves painted into sprites.
6. Scale and grounding: size matches neighbours; contact shadow present where the object touches ground.
7. UI only: text readable, nothing clipped by safe areas, touch targets at least 44 pt.

Required reply format:
```
VERDICT: APPROVED | REJECTED
1. pass|fail - reason
...
```

## 3. Act On It
- `REJECTED`: fix every failed item, rebuild the evidence, grade again. Do not argue the verdict down.
- `APPROVED`: report done and attach the evidence image to the report.
- Never write "looks good" or "done" without an `APPROVED` verdict from this loop.
