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
| Area tests | `tools/test.sh --changed` | The Unity tests `tools/test-areas.tsv` maps to the files changed since `main` (or to paths you name), without the Slow ones. It prints the area and the Unity seconds | Recorded test time 0–106 s for a narrow area, 309 s for `text`, 960 s for `ui`, on top of the Unity start below |
| One test group | `tools/test.sh <filter>` | Unity EditMode tests whose full name matches the regular expression, e.g. `tools/test.sh SaveStoreTests`; `--fast` leaves the Slow ones out | Unity start plus the tests |
| One screen's picture | `tools/shot.sh <frame>` | One named preview frame as `Logs/shot/<frame>.png`, e.g. `tools/shot.sh 113-ruong-mo`; `--list` and `--groups` print the names without Unity. Its own test: `python3 -I tools/test_shot.py` (30 s, no Unity, outside the sandbox) | 113 s of Unity for `113-ruong-mo`, whose group starts alone; 521 s when the groups before it had to play first (one run each, 26 Nov) |
| Fast verify | `tools/verify.sh --fast` | Rules, content seed, compile of every assembly, EditMode tests except `[Category("Slow")]`, player save untouched | Not measured. Its tests took 496 s and 986 s in two recorded full runs, plus two Unity starts |
| Full verify | `tools/verify.sh` | Fast verify plus preview shots and the Slow tests; `--build` adds a Mac build that is opened | Test step alone took 31, 39, 71 and 76 min in four recorded runs (24–26 Nov). The 32 Slow tests are three quarters of it |

Of the four Unity rows only the area tests ran for this table: `tools/test.sh --changed` on the `content` area took 316 s of Unity in a fresh worktree (275 s first import, 21 s for its 47 tests), after two earlier waits of 45 minutes without a slot. Test times are sums of per-test durations in the `TestResults.xml` of two full runs (24 and 26 Nov; the second shared the Mac with other runs). A Unity start took 6–12 s in a checkout that has a `Library` folder and 144–275 s for the first import in a fresh worktree (logs of three fresh worktrees and four warm starts, 26 Nov). Waiting for a slot is extra: `tools/unity-gate.sh status`.

| Change | Run |
|---|---|
| Docs, `production/`, `design/` only | Rules |
| `tools/conventions.py` or its baseline | Rules and checker self-tests |
| `tools/unity-gate.sh` or a tool that starts Unity | Rules and `python3 -I tools/test_unity_gate.py` (24 s, no Unity, outside the sandbox) |
| `Assets/VuonTra/Core` or a pure test | Rules and Core tests |
| `tools/test.sh` or `tools/test-areas.tsv` | Rules and `python3 -I tools/test_test_areas.py` (1 s) |
| `Game`, `Editor`, a screen test, assets | Rules, Core tests, `tools/test.sh --changed`, then fast verify before merge |
| A round of merges on `main`, or a release | Full verify |

CI (`.github/workflows/checks.yml`) runs only the first three rows. Nothing in CI compiles `Game` or `Editor`.

Traps:
- `tools/ci/core-tests.sh` fails inside the Claude Code sandbox with `SocketException (13): Permission denied`, because the .NET test host binds a local socket. Run it outside the sandbox.
- Every checkout shares one Mac and one Unity queue, `tools/unity-gate.sh` (`docs/save-va-test.md`). `tools/verify.sh`, `tools/test.sh` and `tools/build.sh` queue through it themselves; never start the Unity binary any other way. `tools/unity-gate.sh status` shows who holds Unity, who waits and for how long. The gate and its status need to run outside the Claude Code sandbox. Any session may run Unity, only through the gate. The gate limits how many runs go at once and keeps a copy of the player's save; it does not undo a changed save, bound a run's length or clean the checkout. So never touch the player's save, give waits a limit (`UNITY_GATE_MAX_WAIT=1800`), and restore assets Unity rewrites that are not your change (`git status`, then `git checkout -- <file>`). Session B holds the main checkout and merges.
- Batch mode cannot open a project the Unity Editor holds, and Unity builds nothing from a checkout under `.claude/worktrees`.

Who does what, which file is the source of truth, and which skill each role uses: `docs/studio/HUONG-DAN-STUDIO.md` (Vietnamese). Folder map and first-clone setup: `README.md`.

## Working On The PC (Windows 11 + WSL2)
All game work now runs on the owner's PC: Orca and Claude Code in WSL2 (Ubuntu), Unity 6000.6.10f1 for Windows, one RTX 4070. The tables above were measured on the Mac; the same scripts run here from WSL (PR #165). On the PC:

- **Checkouts.** The main checkout is `/mnt/d/Projects/moon-rabbit-tea-garden` (`D:\Projects\moon-rabbit-tea-garden`). Make every worktree that will run Unity with WSL git under `/mnt/d/Projects/mrtg-wt/<name>`: `git -C /mnt/d/Projects/moon-rabbit-tea-garden worktree add --detach /mnt/d/Projects/mrtg-wt/<name> origin/<branch>`. Unity.exe runs projects on a Windows drive, not on the WSL disk (`/home/...`, where Orca workspaces live): edit in an Orca workspace if you like, but run Unity on a `D:` worktree. Never use Windows git on these checkouts: it checks files out with CRLF, which breaks every bash tool, and Windows `git worktree prune` deletes the WSL worktrees' records because it cannot resolve their `/mnt/d` paths.
- **Unity.** Set `export UNITY=/mnt/d/Unity/Editors/6000.6.10f1/Editor/Unity.exe` before any Unity tool; without it the tools look for the Mac editor. Everything still goes through `tools/unity-gate.sh`, run outside the Claude Code sandbox.
- **Light mode.** When the owner is playing on the PC, do not start Unity, ComfyUI, builds, browser renders or other GPU or heavy CPU jobs, even through the gate. The owner marks this with `D:\Projects\.pc-busy` (double-click `tools/windows/pc-busy.cmd`, then `pc-free.cmd`; or `tools/unity-gate.sh busy <reason>` and `free` from WSL). While the file exists the gate starts no new run, `tools/unity-gate.sh status` prints `HOLD:`, and runs already holding a slot go on. Check `status` before any GPU or heavy job, not only Unity. Reading, editing, git, `gh`, the Python tool tests and pushing for CI are fine. Write the Unity checks you skipped into the PR as exact commands for later.
- **Files Unity rewrites.** A batch run on Windows rewrites files that are not your change: `ProjectSettings/ProjectSettings.asset`, `Assets/Localization/LocalizationSettings.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`, `Assets/VuonTra/Scenes/Garden.unity` and `Assets/VuonTra/Art/Placeholder/bg_*.png.meta`. After each run, `git status`, then `git checkout -- <file>` for each of these your branch did not mean to change. Never commit them by accident.
- **Core tests.** WSL has no .NET SDK. Use the one inside Unity: `DOTNET=/mnt/d/Unity/Editors/6000.6.10f1/Editor/Data/DotNetSdk/dotnet.exe tools/ci/core-tests.sh` (outside the sandbox), or let CI run them on push.
- **PlayerPrefs.** The Editor's PlayerPrefs live in the Windows registry, shared by every checkout and every parallel run. A test that reads a pref another run writes flakes (#188, #191). Tests that depend on the real clock (`DateTime.Now`) flake by the hour (#166, #179). Name these when a run fails, and do not count them against a change that does not touch them.
- **Push.** Use `gh` as the credential helper: `git -c credential.helper='!gh auth git-credential' push -u origin <branch>`.
- **Times.** A fresh worktree imports for 144–275 s and grows a Library of about 3.7 GB. QA runs held the gate 18–30 minutes (#185 to #197); give waits a limit with `UNITY_GATE_MAX_WAIT`.

## Unity Anti-Slop Rules
1. **No Procedural Animation Math:** Never animate character limbs, props, or poses via `Mathf.Sin`/`Lerp` in C#. Use Unity `Animator`, `.anim` clips, or `2D Bone Rigging`.
2. **Data-Driven Modularity:** Use `ScriptableObject` for all items, characters, and station stats. For 2D character customization/skins, use `SpriteLibrary` + `SpriteResolver` + `SortingGroup`, never duplicate prefabs per skin.
3. **Native VFX Only:** Never bake sparkles, dust, steam, or falling leaves into character/prop PNGs. Use Unity `ParticleSystem` (Shuriken).
4. **Clean Lifecycle:** Keep `Update()` empty unless handling per-frame input/physics. Use C# events and `DOTween` for UI transitions and tactile "juice" feedback.
5. **Visual Proof Required:** Before marking any visual/UI/animation task complete, generate a composite preview image or GameView screenshot (`tools/shot.sh <frame>` renders one screen) and verify alignment, palette consistency, and clean alpha edges.

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
