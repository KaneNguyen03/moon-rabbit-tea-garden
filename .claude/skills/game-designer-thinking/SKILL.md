---
name: game-designer-thinking
description: Use before proposing, judging or changing any feature, screen, reward, progression step or visual for Vườn Trà Thỏ Trăng, and whenever the owner says something "feels wrong", "looks off" or "hurts the eyes". Makes the agent think as the game's designer - from the player's feeling and the core loop outward to the code - instead of as an implementer of a ticket.
---

# Thinking as the game designer

Code answers "how". This skill answers "should we, and what should it feel like". Run it first; then hand the how to `game-design-review` (visual checks), `game-ui-ux`, `game-feel-juice`, `unity-2d-native-rigging` and `cozy-watercolor-art`.

## 1. The game in one breath

A cozy idle garden at night: bunnies make tea and mochi, the garden grows, the player drops in for short calm visits. Player goals, in order: **calm** (nothing hurts the eyes or nags), **ownership** (my garden, my bunnies), **visible progress** (something new each visit), **gentle collection** (bunnies, dishes, decor). Business goal: `docs/phat-hanh/LO-TRINH-KIEM-TIEN.md`. Market research: `docs/nghien-cuu-cats-and-soup-vong-*.md`. Learn principles from other games; never copy a specific game's look, characters, names or assets.

## 2. Five questions before any change

1. **Who and when:** which player (day 1, day 7, day 30), at which moment of a visit (open, collect, spend, leave)?
2. **Feeling:** what should they feel in the first second? Name one word (calm, curious, proud, delighted). If the change does not serve that word, stop.
3. **Loop:** which loop does it feed - the 30-second loop (tap, collect, upgrade), the visit loop (return, harvest, new thing), or the long loop (expand, collect all)? A feature that feeds none is decoration; price it as such.
4. **Cost to the player:** what does it take from them - screen space, attention, a tap, waiting, money? Every label, popup and effect spends attention. Spend it only where the payoff is.
5. **Proof:** how will we know it works - a screenshot that passes the 25 % greyscale test, a test, a number from the economy sim, or an owner "đẹp"?

Write the answers in one or two lines in the issue comment or report before coding.

## 3. Lenses for judging what is on screen

- **Five-second test:** a new player sees the garden for five seconds. Can they say what to tap, what is theirs and what is still to unlock? If not, hierarchy is wrong (N1 in `docs/nghien-cuu-thiet-ke-do-hoa.md`).
- **Squint test:** blur or greyscale the capture. The eye should land on the rabbit at work or the teapot, never on a label, an overlay or a locked area.
- **Comfort test:** 10 minutes of play without eye strain - no large bright or milky areas, no flicker, no constant pulsing. When the owner says "đau mắt", remove before you tune.
- **Delete test:** for every element ask "what breaks if it goes?". If nothing, delete it. Subtract before adding.
- **Juice budget:** every action gets feedback (visual, sound, particle), but idle ambience stays quiet; the screen is calm until the player acts.
- **Locked versus empty:** locked things must look intentionally dark and inviting (a sign, a silhouette), not broken, faded or foggy.

## 4. Progression and rewards

- Something new each visit; a bigger reveal every few days; the long goal always visible but never shouting.
- Rewards arrive as moments: a short reveal (600-800 ms), a sound, then out of the way.
- Respect the player: no fake urgency, no punishing absence, no paywall on calm. Ads and purchases are opt-in and offer comfort or cosmetics (`LO-TRINH-KIEM-TIEN.md`).
- Tuning goes through the Core economy sims (`tools/ci/core-tests.sh`), not by feel.

## 5. How to decide and record

- Small visual or UX calls inside the art bible: decide, prove with before/after, report.
- Calls that change the art bible, a past owner decision (an issue number), the economy or monetization: write the options with a recommendation and ask the owner; record the answer in `docs/art-bible.md` or the issue.
- When the owner's taste disagrees with a principle, the owner wins; write the new rule down so the next agent follows it.
