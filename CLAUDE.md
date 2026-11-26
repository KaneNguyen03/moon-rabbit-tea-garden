# Vườn Trà Thỏ Trăng

## Code Comments
Never add comments to code. No `//`, `/* */`, `///` XML docs, `#` notes or commented-out code in any file you write or edit. Names and structure carry the meaning. Leave existing comments as they are unless the task is to remove them.

## Enforced Rules
`tools/conventions.py` holds the rules a machine checks: no comments, no procedural animation math or scene searches in `Game`, no new per-frame `Update()`, and no quick-fix workarounds (a deferred frame, `Invoke` by name, `SendMessage`, a swallowed `catch {}`, a disabled warning). It counts them per file, with strings and comments told apart by a small C# lexer, against `tools/conventions.baseline`, which only goes down (`python3 tools/conventions.py --lower`). A Claude hook checks every edit, `.githooks/pre-commit` checks the staged snapshot (`git config core.hooksPath .githooks`), `tools/verify.sh` runs it first, and CI runs it with the Core tests (`tools/ci/core-tests.sh`, the pure-C# tests without Unity; its last line prints the count). When a check fails, fix the cause; a workaround copied once becomes the pattern everyone copies.

## Checking Your Work
Measured on this Mac on 26 Nov 2026, unless a row says otherwise.

| Check | Command | Covers | Time |
|---|---|---|---|
| Rules | `python3 -I tools/conventions.py` | The enforced rules, counted against the baseline | 2–5 s |
| Checker self-tests | `python3 -I tools/test_conventions.py` | `tools/conventions.py` itself | 6–19 s |
| Core tests | `tools/ci/core-tests.sh` | Test files in `Assets/Tests/EditMode` with no Unity reference: game rules, saves, economy. No Unity needed | 24–47 s with compile |
| One test group | `tools/test.sh <filter>` | Unity EditMode tests whose full name matches, e.g. `tools/test.sh SaveStoreTests` | Not measured; needs a Unity start |
| Fast verify | `tools/verify.sh --fast` | Rules, content seed, compile of every assembly, EditMode tests except `[Category("Slow")]`, player save untouched | Not measured |
| Full verify | `tools/verify.sh` | Fast verify plus preview shots and the Slow tests; `--build` adds a Mac build that is opened | Test step alone took 31, 39 and 76 min in three recorded runs (24–26 Nov) |

The three Unity rows were not run for this table: they are cited from the scripts and from `TestResults.xml` files of other checkouts.

| Change | Run |
|---|---|
| Docs, `production/`, `design/` only | Rules |
| `tools/conventions.py` or its baseline | Rules and checker self-tests |
| `tools/unity-gate.sh` or a tool that starts Unity | Rules and `python3 -I tools/test_unity_gate.py` (24 s, no Unity, outside the sandbox) |
| `Assets/VuonTra/Core` or a pure test | Rules and Core tests |
| `Game`, `Editor`, a screen test, assets | Rules, Core tests, `tools/test.sh <filter>` for the area, then fast verify before merge |
| A round of merges on `main`, or a release | Full verify |

CI (`.github/workflows/checks.yml`) runs only the first three rows. Nothing in CI compiles `Game` or `Editor`.

Traps:
- `tools/ci/core-tests.sh` fails inside the Claude Code sandbox with `SocketException (13): Permission denied`, because the .NET test host binds a local socket. Run it outside the sandbox.
- Every checkout shares one Mac and one Unity queue, `tools/unity-gate.sh` (`docs/save-va-test.md`). `tools/verify.sh`, `tools/test.sh` and `tools/build.sh` queue through it themselves; never start the Unity binary any other way. `tools/unity-gate.sh status` shows who holds Unity, who waits and for how long. The gate and its status need to run outside the Claude Code sandbox. Only session B runs Unity; other sessions ask B (`docs/studio/HUONG-DAN-STUDIO.md` section 5).
- Batch mode cannot open a project the Unity Editor holds, and Unity builds nothing from a checkout under `.claude/worktrees`.

Who does what, which file is the source of truth, and which skill each role uses: `docs/studio/HUONG-DAN-STUDIO.md` (Vietnamese). Folder map and first-clone setup: `README.md`.

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
| `unity-mcp-orchestrator` | Driving the Unity Editor over MCP (`CoplayDev/unity-mcp`): read Hierarchy, inspect components, wire `[SerializeField]`, run PlayMode tests, capture GameView. The package is not in `Packages/manifest.json`, so the skill has no tools to call until it is installed |
| `create-game-assets` | Lock an art-direction brief (palette hex, line weight, light direction) before generating art; asset QA and preview sheets |
| `shader-programming` | URP 2D shader effects: watercolor paper grain, wind sway, rim light |
| `img2game2d` | Post-processing art: background removal, defringing, layer split on one canvas size, atlas packing |
| `game-ui-ux` | 9-slice frames, warm palette, World Space vs Screen Space, `RenderTexture` for characters inside UI cards |
| `unity-2d-native-rigging` | Character animation with `SpriteLibrary`/`SpriteResolver`/`SortingGroup`/`Animator`, never transform code |
| `game-feel-juice` | DOTween punch/bounce, random-pitch SFX, Shuriken particles for every interaction |
| `visual-critic-gate` | Mandatory QA sheet or GameView capture graded by a vision model; only `APPROVED` closes a visual task |
| `cozy-watercolor-art` | House art style for this game |
