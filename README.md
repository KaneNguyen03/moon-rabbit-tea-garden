# Vườn Trà Thỏ Trăng (Moon Rabbit Tea Garden)

A cozy idle game for phones, played in portrait: moon rabbits run a tea and mochi garden, stations earn while the player is away, and the garden grows. Languages are Vietnamese and English. A Mac build is used for play tests.

## Stack

Unity 6000.6.10f1, URP 17.6, C#, uGUI built in code, Unity Localization, Unity IAP, Google Play Games, mobile notifications. Tests are NUnit EditMode. The scripts in `tools/` expect Unity at `/Applications/Unity/Hub/Editor/6000.6.10f1` (set `UNITY` to override).

On Windows, run the scripts from WSL with `UNITY` set to the Windows editor, e.g. `UNITY=/mnt/d/Unity/Editors/6000.6.10f1/Editor/Unity.exe tools/test.sh SaveStoreTests`. The checkout must be on a Windows drive (`/mnt/d/...`), checked out with LF line endings (WSL git does this); `tools/unity-env.sh` turns the paths it passes to `Unity.exe` into Windows paths, adds the `VUONTRA_*` variables Unity reads (shots, Android signing, monetization) to `WSLENV` so they reach it, and the player save is then in `%USERPROFILE%\AppData\LocalLow`. The Editor needs the iOS Build Support module, because `BuildScript.cs` uses `UnityEditor.iOS.Xcode`. Mac builds (`tools/build.sh mac`, `tools/verify.sh --build`) need a Mac.

## Folder map

| Path | What is there |
|---|---|
| `Assets/VuonTra/Core` | Game rules in pure C#, no Unity reference: economy, saves, quests. Compiles and tests without Unity |
| `Assets/VuonTra/Game` | The Unity side: runner, UI, platform seams, the 2.5D garden |
| `Assets/VuonTra/Editor` | Content seeders, preview shots, build script |
| `Assets/VuonTra/Content`, `Art`, `Scenes` | Seeded data assets, art, and the one scene, `Garden.unity` |
| `Assets/Tests/EditMode` | All tests. Files with no Unity reference also run without Unity |
| `tools` | Checks and builds: `verify.sh`, `test.sh`, `build.sh`, `conventions.py`, `ci/core-tests.sh` |
| `docs` | Project documents, mostly Vietnamese: ADRs in `docs/architecture`, release plan in `docs/phat-hanh`, studio guide in `docs/studio` |
| `design` | Game design documents (`design/gdd`), UX specs, art bible |
| `production` | Epics and story files (one per GitHub issue), sprint status, QA plans |
| `.claude` | Agent skills, agents and the edit hook |

## Setup after a fresh clone

```sh
git config core.hooksPath .githooks
```

This turns on the pre-commit rule check. Gift codes need `Assets/Resources/keys.json`, which is never committed; `tools/make-code.sh` explains how to make one.

## Web version

The game also plays in a browser: https://kanenguyen03.github.io/moon-rabbit-tea-garden/ (WebGL, served from the `gh-pages` branch). After each merge to `main`, update it with two commands, then wait a minute or two:

```sh
tools/build-webgl.sh
tools/publish-pages.sh
```

`tools/build-webgl.sh` builds into `Build/WebGL` through the shared Unity queue; `tools/publish-pages.sh` adds that build as one new commit on `gh-pages`, without force-pushing, and pushes nothing if the build has not changed. Details, the first-time Pages setup and what is still untested are in [docs/phat-hanh/BAN-WEB.md](docs/phat-hanh/BAN-WEB.md).

## Working here

Rules and the commands to check a change, with their times, are in [CLAUDE.md](CLAUDE.md). Who does what and which file is the source of truth for what is in [docs/studio/HUONG-DAN-STUDIO.md](docs/studio/HUONG-DAN-STUDIO.md). Work is tracked as GitHub issues.
