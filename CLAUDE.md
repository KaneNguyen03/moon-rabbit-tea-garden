# Vườn Trà Thỏ Trăng

## Code Comments
Never add comments to code. No `//`, `/* */`, `///` XML docs, `#` notes or commented-out code in any file you write or edit. Names and structure carry the meaning. Leave existing comments as they are unless the task is to remove them.

## Enforced Rules
`tools/conventions.py` holds the rules a machine checks: no comments, no procedural animation math or scene searches in `Game`, no new per-frame `Update()`, and no quick-fix workarounds (a deferred frame, `Invoke` by name, `SendMessage`, a swallowed `catch {}`, a disabled warning). It counts them per file, with strings and comments told apart by a small C# lexer, against `tools/conventions.baseline`, which only goes down (`python3 tools/conventions.py --lower`). A Claude hook checks every edit, `.githooks/pre-commit` checks the staged snapshot (`git config core.hooksPath .githooks`), `tools/verify.sh` runs it first, and CI runs it with the Core tests (`tools/ci/core-tests.sh`, 701 tests in seconds without Unity). When a check fails, fix the cause; a workaround copied once becomes the pattern everyone copies.

## Unity Anti-Slop Rules
1. **No Procedural Animation Math:** Never animate character limbs, props, or poses via `Mathf.Sin`/`Lerp` in C#. Use Unity `Animator`, `.anim` clips, or `2D Bone Rigging`.
2. **Data-Driven Modularity:** Use `ScriptableObject` for all items, characters, and station stats. For 2D character customization/skins, use `SpriteLibrary` + `SpriteResolver` + `SortingGroup`, never duplicate prefabs per skin.
3. **Native VFX Only:** Never bake sparkles, dust, steam, or falling leaves into character/prop PNGs. Use Unity `ParticleSystem` (Shuriken).
4. **Clean Lifecycle:** Keep `Update()` empty unless handling per-frame input/physics. Use C# events and `DOTween` for UI transitions and tactile "juice" feedback.
5. **Visual Proof Required:** Before marking any visual/UI/animation task complete, generate a composite preview image or GameView screenshot and verify alignment, palette consistency, and clean alpha edges.

## Project Skills (`.claude/skills/`)
| Skill | Use for |
|---|---|
| `unity-developer` | C# architecture: no `GameObject.Find`/`FindObjectOfType` in `Update()`, UI split from gameplay, ScriptableObject data, SO event channels, object pooling |
| `unity-mcp-orchestrator` | Driving the Unity Editor over MCP (`CoplayDev/unity-mcp`): read Hierarchy, inspect components, wire `[SerializeField]`, run PlayMode tests, capture GameView |
| `create-game-assets` | Lock an art-direction brief (palette hex, line weight, light direction) before generating art; asset QA and preview sheets |
| `shader-programming` | URP 2D shader effects: watercolor paper grain, wind sway, rim light |
| `img2game2d` | Post-processing art: background removal, defringing, layer split on one canvas size, atlas packing |
| `game-ui-ux` | 9-slice frames, warm palette, World Space vs Screen Space, `RenderTexture` for characters inside UI cards |
| `unity-2d-native-rigging` | Character animation with `SpriteLibrary`/`SpriteResolver`/`SortingGroup`/`Animator`, never transform code |
| `game-feel-juice` | DOTween punch/bounce, random-pitch SFX, Shuriken particles for every interaction |
| `visual-critic-gate` | Mandatory QA sheet or GameView capture graded by a vision model; only `APPROVED` closes a visual task |
| `cozy-watercolor-art` | House art style for this game |

@.claude/pstack-models.md
