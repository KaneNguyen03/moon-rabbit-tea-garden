# Technical Preferences

<!-- project.yaml at the repo root is the machine-readable source of truth for
     engine, specialists, naming, performance, platform, and testing.framework.
     This file is the human-readable LEGACY FALLBACK: agents and skills resolve
     each key from project.yaml first and fall back here only when the
     project.yaml key is absent. /setup-engine dual-writes both.
     Forbidden patterns and allowed libraries are NOT migrated — they live only
     in this file. Populated by /setup-engine; updated as decisions are made. -->

## Engine & Language

- **Engine**: Unity 6000.6.10f1
- **Language**: C#
- **Rendering**: URP 17.6, Renderer2D today; a UniversalRendererData (Forward+) is planned for the 2.5D garden scene (docs/de-xuat-2-5d.md §3, issue #145)
- **Physics**: PhysX (3D taps through PhysicsRaycaster in the 2.5D garden); no gameplay physics

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: Mobile (Android first, then iOS); Mac build for internal play tests
- **Input Methods**: Touch
- **Primary Input**: Touch
- **Gamepad Support**: None
- **Touch Support**: Full
- **Platform Notes**: Portrait only; safe areas; text in vi and en through Unity Localization; tap targets and contrast per AccessibilityTests

## Naming Conventions

- **Classes**: PascalCase
- **Variables**: camelCase (private fields camelCase, as in Assets/VuonTra)
- **Signals/Events**: C# events named for what happened (PascalCase)
- **Files**: PascalCase matching the class
- **Scenes/Prefabs**: PascalCase
- **Constants**: PascalCase

## Performance Budgets

- **Target Framerate**: 60
- **Frame Budget**: 16.67 ms
- **Draw Calls**: not set; measured on device in issue #149
- **Memory Ceiling**: not set; zero garbage per frame in a full garden (#123)

## Testing

- **Framework**: NUnit (Unity Test Framework EditMode in Assets/Tests/EditMode; the pure Core tests also run without Unity through tools/ci/core-tests.sh)
- **Minimum Coverage**: none set; every Core rule change ships with a test
- **Required Tests**: Balance formulas, gameplay systems, networking (if applicable)

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- Code comments of any kind (CLAUDE.md, tools/conventions.py)
- Procedural animation math in C# (Mathf.Sin/Lerp on limbs or props); use Animator and .anim clips
- GameObject.Find / FindObjectOfType and other scene searches in Game
- New per-frame Update() outside input or physics
- Workarounds: deferred frames, Invoke by name, SendMessage, empty catch, disabled warnings
- Sparkles, dust, steam or leaves baked into PNGs; use Shuriken
- A duplicated prefab per skin; use SpriteLibrary and SpriteResolver

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- DOTween (UI transitions and juice)
- Unity IAP 4.15 (behind Features.Monetization, off by default)
- Unity Mobile Notifications 2.5
- Unity Localization 1.5
- Unity 2D Animation and PSD Importer (rabbit rig)
- Google Play Games plugin 2.3 (cloud save seam)

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- docs/architecture/adr-0001 to adr-0005 (pure C# Core, ScriptableObject data, Storefront/Ads/Analytics seams, Monetization flag, 2.5D direction)

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (Shader Graph, HLSL, URP materials)
- **UI Specialist**: unity-ui-specialist (UGUI Canvas, World Space labels)
- **Additional Specialists**: unity-addressables-specialist (Addressables groups and loading)
- **Routing Notes**: Primary for architecture and C# review; shader specialist for the 2.5D lighting and paper-grain pass; UI specialist for every screen. Only the session that owns main (B) runs the Unity Editor.

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (Canvas prefabs) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Native extension / plugin files | unity-specialist |
| General architecture review | Primary |
