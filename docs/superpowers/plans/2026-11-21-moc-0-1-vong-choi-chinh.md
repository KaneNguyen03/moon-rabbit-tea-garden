# Mốc 0 + 1: Dựng nền và vòng chơi chính — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Unity project where pressing Play shows a garden of stations that earn "sương" on their own, can be upgraded and unlocked, pay out for time away, and keep their progress between runs.

**Architecture:** All game rules live in `VuonTra.Core`, a plain C# assembly with no `UnityEngine` reference, covered by EditMode tests. `VuonTra.Game` is the Unity layer: ScriptableObject data, a `GameRunner` MonoBehaviour that owns the state and ticks the core, and uGUI views that only read state and send commands. Content is data, so later milestones add stations, sprites and events as assets.

**Tech Stack:** Unity 6 (6000.6.10f1), 2D URP, C#, uGUI + TextMeshPro, Unity Test Framework (NUnit, EditMode), `JsonUtility`, Git.

**Spec:** `docs/superpowers/specs/2026-11-21-vuon-tra-nam-design.md`

## Global Constraints

- Portrait orientation, reference resolution 1080×1920.
- No ads, no real-money purchases, no online account, no cloud save, no iOS build.
- The game must progress without tapping; tapping is only a small speed-up.
- Stations, and later sprites, events and quests, are data assets. Adding one must not require a code change.
- Save file carries a version number; loading an older version must not lose progress.
- All player-facing text is Vietnamese. Currency is "cà rốt" (changed from "sương" at the user's request on 2026-11-21).
- Art is replaceable by swapping sprite files; no code refers to a specific image.
- Theme and art are original. Nothing from Cats & Soup (name, characters, art, layout) is copied.
- Unity binary: `/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity` (called `$UNITY` below).
- Test command (called `$TEST` below): `"$UNITY" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -logFile -` ; pass means exit code 0 and `TestResults.xml` has `result="Passed"` on the root `test-run`.

## Review Focus

1. **Device clock moved backwards** (player changes the date, or travels west): away time must be treated as 0, never negative, and never crash.
2. **Away for weeks:** payout is capped at the away limit (2 hours by default) and the number stays finite.
3. **Corrupt or half-written save file:** the game starts fresh and keeps the bad file as `save.bad.json` instead of crashing or overwriting it silently.
4. **Save from a newer or unknown version:** not loaded, not overwritten; start fresh in memory and keep the file.
5. **Very large numbers** (past 1e308): costs, income and the on-screen format stay correct; nothing shows `Infinity` or `NaN`.

Each of these has a test in the task that owns the code (Tasks 2, 4, 5).

## File Structure

```
Assets/
  VuonTra/
    Core/                      VuonTra.Core.asmdef (noEngineReferences: true)
      BigNum.cs                number type + Vietnamese short format
      StationDef.cs            immutable station definition, StationKind
      GameState.cs             plain serializable state
      Economy.cs               cost, milestone, income, boost formulas
      Simulation.cs            tick, tap, buy, offline payout
      SaveCodec.cs             state <-> JSON string, versions, validation
    Game/                      VuonTra.Game.asmdef (references Core, TMP, URP)
      Data/StationData.cs      ScriptableObject -> StationDef
      Data/GameCatalog.cs      ScriptableObject: ordered list of StationData
      GameRunner.cs            owns state, ticks, autosaves, raises events
      SaveStore.cs             file IO around SaveCodec
      UI/HudView.cs            sương total and per-second
      UI/StationRowView.cs     one upgrade row
      UI/UpgradePanelView.cs   list of rows, buy mode x1/x10/Tối đa
      UI/GardenView.cs         places StationActorView per station
      UI/StationActorView.cs   sprite, progress bar, tap, payout popup
      UI/AwayPopupView.cs      "Mừng bạn quay lại"
    Content/Stations/*.asset   8 StationData + GameCatalog.asset
    Art/Placeholder/           CC0 sprites
    Scenes/Garden.unity
  Tests/EditMode/              VuonTra.Core.Tests.asmdef
    BigNumTests.cs EconomyTests.cs SimulationTests.cs SaveCodecTests.cs
```

---

### Task 1: Project scaffold

**Files:**
- Create: Unity project at repo root (2D URP template), `.gitignore` (Unity standard), the three `.asmdef` files, `Assets/Tests/EditMode/SmokeTests.cs`, `Assets/VuonTra/Scenes/Garden.unity`

**Interfaces:**
- Produces: assemblies `VuonTra.Core`, `VuonTra.Game`, `VuonTra.Core.Tests`; `$TEST` runs green.

- [ ] **Step 1:** Create the project in place with `"$UNITY" -batchmode -createProject . -quit -logFile -`, then add packages `com.unity.render-pipelines.universal`, `com.unity.ugui`, `com.unity.test-framework` to `Packages/manifest.json` if the template lacks them. Keep `docs/` and `tham-khao/` untouched.
- [ ] **Step 2:** `git init`; add the standard Unity `.gitignore` (`Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build*/`, `TestResults.xml`, `*.csproj`, `*.sln`).
- [ ] **Step 3:** Create the three asmdefs. `VuonTra.Core`: `"noEngineReferences": true`. `VuonTra.Core.Tests`: Editor-only, references `VuonTra.Core`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `"overrideReferences": true` with `nunit.framework.dll`, define constraint `UNITY_INCLUDE_TESTS`.
- [ ] **Step 4:** Write `SmokeTests.Core_Assembly_Loads` asserting `typeof(VuonTra.Core.BigNum) != null` — expect compile FAIL until Task 2; for now assert `1 + 1 == 2`.
- [ ] **Step 5:** Run `$TEST`. Expected: exit 0, 1 test passed.
- [ ] **Step 6:** Player settings: default orientation Portrait, company `VuonTraNam`, product `Vườn Trà Thỏ Trăng`. Create empty `Garden.unity`, add to build settings.
- [ ] **Step 7:** Commit `chore: scaffold Unity project`.

### Task 2: BigNum

**Files:**
- Create: `Assets/VuonTra/Core/BigNum.cs`
- Test: `Assets/Tests/EditMode/BigNumTests.cs`

**Interfaces:**
- Produces: `readonly struct BigNum : IComparable<BigNum>, IEquatable<BigNum>` with `double Mantissa` (0 or in [1,10)), `long Exponent` (base 10); `static BigNum Zero, One`; `BigNum(double value)`; `static BigNum FromParts(double mantissa, long exponent)` (normalizes); operators `+ - * /` between BigNum, `*` and `/` with `double`, comparisons; `BigNum Pow(double exponent)` for non-negative base; `double ToDouble()` (clamped to `double.MaxValue`); `string ToShortString()`; `double Log10()`.
- Negative results of `-` clamp to `Zero` (the game never holds debt).

- [ ] **Step 1: Write failing tests**

```csharp
[Test] public void Normalizes() { var n = BigNum.FromParts(123, 0); Assert.AreEqual(1.23, n.Mantissa, 1e-9); Assert.AreEqual(2, n.Exponent); }
[Test] public void Adds_Across_Magnitudes() { Assert.AreEqual(new BigNum(1001).ToDouble(), (new BigNum(1000) + new BigNum(1)).ToDouble(), 1e-6); }
[Test] public void Tiny_Addend_Is_Dropped() { var big = BigNum.FromParts(1, 400); Assert.IsTrue((big + BigNum.One).Equals(big)); }
[Test] public void Subtract_Clamps_To_Zero() { Assert.IsTrue((new BigNum(5) - new BigNum(9)).Equals(BigNum.Zero)); }
[Test] public void Multiplies_Past_Double_Range() { var n = BigNum.FromParts(5, 300) * BigNum.FromParts(4, 300); Assert.AreEqual(2, n.Mantissa, 1e-9); Assert.AreEqual(601, n.Exponent); }
[Test] public void Pow_Matches_Double() { Assert.AreEqual(Math.Pow(1.09, 50), new BigNum(1.09).Pow(50).ToDouble(), 1e-6); }
[Test] public void Compare_Orders() { Assert.IsTrue(BigNum.FromParts(9, 5) < BigNum.FromParts(1, 6)); }
[TestCase(0, "0")] [TestCase(999.9, "999")] [TestCase(1000, "1,00K")] [TestCase(12345, "12,3K")] [TestCase(123456, "123K")] [TestCase(2.5e6, "2,50M")] [TestCase(1e9, "1,00B")] [TestCase(1e12, "1,00T")] [TestCase(1e15, "1,00aa")] [TestCase(1e18, "1,00ab")]
public void ShortString(double v, string s) { Assert.AreEqual(s, new BigNum(v).ToShortString()); }
[Test] public void ShortString_Huge_Has_No_Infinity() { var s = BigNum.FromParts(3, 400).ToShortString(); StringAssert.DoesNotContain("Infinity", s); StringAssert.DoesNotContain("NaN", s); }
[Test] public void NaN_And_Infinity_Become_Zero() { Assert.IsTrue(new BigNum(double.NaN).Equals(BigNum.Zero)); Assert.IsTrue(new BigNum(double.PositiveInfinity).Equals(BigNum.Zero)); }
```

- [ ] **Step 2:** Run `$TEST`, expect FAIL (BigNum missing).
- [ ] **Step 3:** Implement. Format rule: below 1000 → floored integer; otherwise 3 significant digits, comma as decimal separator, suffixes `K M B T` then two letters `aa, ab … az, ba …` continuing without limit. Addition drops the smaller operand when exponents differ by more than 17.
- [ ] **Step 4:** Run `$TEST`, expect PASS.
- [ ] **Step 5:** Commit `feat(core): BigNum`.

### Task 3: Station definitions and economy formulas

**Files:**
- Create: `Assets/VuonTra/Core/StationDef.cs`, `GameState.cs`, `Economy.cs`
- Test: `Assets/Tests/EditMode/EconomyTests.cs`

**Interfaces:**
- Consumes: `BigNum`.
- Produces:
  - `enum StationKind { Brew, Ingredient }`
  - `sealed class StationDef { string Id; string Name; StationKind Kind; double BaseCost; double CostGrowth; double BaseIncome; double CycleSeconds; string[] BoostsIds; }` — `BaseIncome`/`CycleSeconds` used by Brew; `BoostsIds` (ids of Brew stations) used by Ingredient.
  - `sealed class GameState { BigNum Coins; int[] Levels; double[] Progress; int BuyMode; long LastSeenUnixSeconds; bool HintSeen; }` with `static GameState NewGame(IReadOnlyList<StationDef> defs)` → first station level 1, others 0, coins 0, `BuyMode` 1.
  - `static class Economy`:
    - `static readonly int[] Milestones = {10, 25, 50, 100, 150, 200, 300, 400, 500}`
    - `double MilestoneMultiplier(int level)` → 2^(milestones reached)
    - `int NextMilestone(int level)` → 0 when none left
    - `BigNum CostOfNext(StationDef d, int level, int count)` → `BaseCost · g^level · (g^count − 1)/(g − 1)`; unlocking (level 0, count 1) costs exactly `BaseCost`
    - `int MaxAffordable(StationDef d, int level, BigNum coins)`
    - `double IngredientBoost(int level)` → `1 + 0.10 · level · MilestoneMultiplier(level)`; level 0 → 1
    - `BigNum IncomePerCycle(IReadOnlyList<StationDef> defs, GameState s, int index)` → for Brew: `BaseIncome · level · MilestoneMultiplier(level) · product of IngredientBoost of every Ingredient station whose BoostsIds contains this id`; Ingredient or level 0 → Zero
    - `BigNum IncomePerSecond(IReadOnlyList<StationDef> defs, GameState s)`
    - `bool CanUnlock(GameState s, int index)` → index 0, or previous station level > 0

- [ ] **Step 1: Write failing tests** (fixture: `tea` Brew cost 10, growth 1.09, income 1, cycle 1.5; `leaf` Ingredient cost 120, growth 1.11, boosts `tea`; `jam` Brew cost 1400, growth 1.12, income 80, cycle 5)

```csharp
[TestCase(1, 1)] [TestCase(9, 1)] [TestCase(10, 2)] [TestCase(25, 4)] [TestCase(500, 512)] [TestCase(9999, 512)]
public void Milestone(int lv, double m) => Assert.AreEqual(m, Economy.MilestoneMultiplier(lv));
[Test] public void Unlock_Costs_BaseCost() => Assert.AreEqual(120, Economy.CostOfNext(leaf, 0, 1).ToDouble(), 1e-9);
[Test] public void Cost_Of_Ten_Is_Geometric_Sum() { double e = 0; for (int k = 1; k <= 10; k++) e += 10 * Math.Pow(1.09, k); Assert.AreEqual(e, Economy.CostOfNext(tea, 1, 10).ToDouble(), 1e-6); }
[Test] public void MaxAffordable_Is_Exact() { int n = Economy.MaxAffordable(tea, 1, new BigNum(1000)); Assert.IsTrue(Economy.CostOfNext(tea, 1, n) <= new BigNum(1000)); Assert.IsTrue(Economy.CostOfNext(tea, 1, n + 1) > new BigNum(1000)); }
[Test] public void MaxAffordable_Zero_When_Broke() => Assert.AreEqual(0, Economy.MaxAffordable(tea, 1, BigNum.Zero));
[Test] public void Ingredient_Boosts_Only_Its_Targets() { s.Levels = new[] {4, 5, 2}; Assert.AreEqual(1 * 4 * 1.5, Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9); Assert.AreEqual(80 * 2, Economy.IncomePerCycle(defs, s, 2).ToDouble(), 1e-9); Assert.IsTrue(Economy.IncomePerCycle(defs, s, 1).Equals(BigNum.Zero)); }
[Test] public void IncomePerSecond_Sums_Brew_Stations() { s.Levels = new[] {3, 0, 1}; Assert.AreEqual(3 / 1.5 + 80 / 5.0, Economy.IncomePerSecond(defs, s).ToDouble(), 1e-9); }
[Test] public void CanUnlock_Requires_Previous() { s.Levels = new[] {1, 0, 0}; Assert.IsTrue(Economy.CanUnlock(s, 1)); Assert.IsFalse(Economy.CanUnlock(s, 2)); }
[Test] public void Cost_At_High_Level_Is_Finite() { var c = Economy.CostOfNext(tea, 20000, 1); Assert.IsTrue(c > BigNum.FromParts(1, 308)); StringAssert.DoesNotContain("Infinity", c.ToShortString()); }
```

- [ ] **Step 2:** Run `$TEST`, expect FAIL.
- [ ] **Step 3:** Implement the three files. `CostOfNext` computes with `BigNum.Pow`. `MaxAffordable` solves the closed form with `Log10`, then corrects by ±1 against `CostOfNext` so the test's exactness holds.
- [ ] **Step 4:** Run `$TEST`, expect PASS.
- [ ] **Step 5:** Commit `feat(core): station economy`.

### Task 4: Simulation (tick, tap, buy, away payout)

**Files:**
- Create: `Assets/VuonTra/Core/Simulation.cs`
- Test: `Assets/Tests/EditMode/SimulationTests.cs`

**Interfaces:**
- Consumes: Task 3 types.
- Produces: `static class Simulation`:
  - `const double TapSeconds = 0.4`, `const double AwayCapSeconds = 7200`, `const double AwayMinSeconds = 30`
  - `void Tick(IReadOnlyList<StationDef> defs, GameState s, double dt, Action<int, BigNum> onPayout = null)` — advances every Brew station with level > 0 by `dt / CycleSeconds`; each whole cycle pays `IncomePerCycle` (computed before the tick mutates levels); callback gets station index and total paid this tick. Ingredient stations do not cycle. `dt <= 0` does nothing.
  - `bool Tap(defs, s, int index)` — Brew, level > 0: progress += `TapSeconds / CycleSeconds`, returns true; otherwise false. Payout happens on the next `Tick`.
  - `int BuyCount(defs, s, int index)` — level 0 → 1; `BuyMode` 1 or 10 → that; `BuyMode` 0 ("Tối đa") → `max(1, MaxAffordable)`.
  - `bool TryBuy(defs, s, int index)` — fails without changing state when locked-and-not-unlockable or coins insufficient.
  - `BigNum ClaimAway(defs, s, long nowUnixSeconds, out double countedSeconds)` — `elapsed = now − LastSeenUnixSeconds`; `elapsed < AwayMinSeconds` (including negative) → Zero, `countedSeconds` 0; otherwise pays `IncomePerSecond · min(elapsed, AwayCapSeconds)`. Always sets `LastSeenUnixSeconds = now`.

- [ ] **Step 1: Write failing tests** (same fixture as Task 3)

```csharp
[Test] public void Tick_Pays_Whole_Cycles_And_Keeps_Remainder() { Simulation.Tick(defs, s, 4.0); Assert.AreEqual(2, s.Coins.ToDouble(), 1e-9); Assert.AreEqual(4.0 / 1.5 - 2, s.Progress[0], 1e-9); }
[Test] public void Tick_Ignores_Locked_And_Ingredient() { s.Levels = new[] {1, 3, 0}; Simulation.Tick(defs, s, 100); Assert.AreEqual(0, s.Progress[1]); Assert.AreEqual(0, s.Progress[2]); }
[Test] public void Tick_Reports_Payout_Per_Station() { int idx = -1; BigNum paid = BigNum.Zero; Simulation.Tick(defs, s, 1.5, (i, p) => { idx = i; paid = p; }); Assert.AreEqual(0, idx); Assert.AreEqual(1, paid.ToDouble(), 1e-9); }
[Test] public void Tick_Negative_Dt_Does_Nothing() { Simulation.Tick(defs, s, -5); Assert.AreEqual(0, s.Progress[0]); }
[Test] public void Tap_Advances_By_TapSeconds() { Assert.IsTrue(Simulation.Tap(defs, s, 0)); Assert.AreEqual(0.4 / 1.5, s.Progress[0], 1e-9); Assert.IsFalse(Simulation.Tap(defs, s, 2)); }
[Test] public void Buy_Deducts_And_Levels() { s.Coins = new BigNum(100); Assert.IsTrue(Simulation.TryBuy(defs, s, 0)); Assert.AreEqual(2, s.Levels[0]); Assert.AreEqual(100 - 10 * 1.09, s.Coins.ToDouble(), 1e-9); }
[Test] public void Buy_Fails_Cleanly_When_Broke() { Assert.IsFalse(Simulation.TryBuy(defs, s, 0)); Assert.AreEqual(1, s.Levels[0]); }
[Test] public void Cannot_Skip_Unlock_Order() { s.Coins = new BigNum(1e9); Assert.IsFalse(Simulation.TryBuy(defs, s, 2)); }
[Test] public void BuyMode_Max_Buys_All_Affordable() { s.Coins = new BigNum(1000); s.BuyMode = 0; int n = Simulation.BuyCount(defs, s, 0); Simulation.TryBuy(defs, s, 0); Assert.AreEqual(1 + n, s.Levels[0]); Assert.IsTrue(n > 1); }
[Test] public void Away_Pays_Elapsed() { s.LastSeenUnixSeconds = 1000; var p = Simulation.ClaimAway(defs, s, 1600, out var c); Assert.AreEqual(600, c); Assert.AreEqual(600 / 1.5, p.ToDouble(), 1e-6); Assert.AreEqual(1600, s.LastSeenUnixSeconds); }
[Test] public void Away_Is_Capped_At_Two_Hours() { s.LastSeenUnixSeconds = 0; Simulation.ClaimAway(defs, s, 30L * 86400, out var c); Assert.AreEqual(7200, c); }
[Test] public void Away_Clock_Moved_Back_Pays_Nothing() { s.LastSeenUnixSeconds = 5000; var p = Simulation.ClaimAway(defs, s, 1000, out var c); Assert.IsTrue(p.Equals(BigNum.Zero)); Assert.AreEqual(0, c); Assert.AreEqual(1000, s.LastSeenUnixSeconds); }
[Test] public void Away_Under_Thirty_Seconds_Pays_Nothing() { s.LastSeenUnixSeconds = 1000; Assert.IsTrue(Simulation.ClaimAway(defs, s, 1020, out _).Equals(BigNum.Zero)); }
```

- [ ] **Step 2:** Run `$TEST`, expect FAIL.
- [ ] **Step 3:** Implement `Simulation.cs`.
- [ ] **Step 4:** Run `$TEST`, expect PASS.
- [ ] **Step 5:** Commit `feat(core): simulation and away payout`.

### Task 5: Save and load

**Files:**
- Create: `Assets/VuonTra/Core/SaveCodec.cs`, `Assets/VuonTra/Game/SaveStore.cs`
- Test: `Assets/Tests/EditMode/SaveCodecTests.cs`

**Interfaces:**
- Consumes: `GameState`, `StationDef`.
- Produces:
  - `static class SaveCodec { const int CurrentVersion = 1; string Encode(IReadOnlyList<StationDef> defs, GameState s); LoadResult Decode(IReadOnlyList<StationDef> defs, string json); }`
  - `enum LoadStatus { Ok, Empty, Corrupt, NewerVersion }`; `struct LoadResult { LoadStatus Status; GameState State; }` — `State` is a fresh `NewGame` for every status except `Ok`.
  - Format: JSON object `{ "version": 1, "coinsMantissa": double, "coinsExponent": long, "stations": [ { "id": string, "level": int, "progress": double } ], "buyMode": int, "lastSeen": long, "hintSeen": bool }`. Stations are matched by `id`, not position, so reordering or adding stations later keeps progress; unknown ids are ignored; missing ids start at level 0 (first station floor 1).
  - Written by hand (small writer + tolerant reader) because Core cannot reference `JsonUtility`.
  - `SaveStore` (Game assembly): `LoadResult Load(defs)`, `void Save(defs, state)`. Path `Application.persistentDataPath/save.json`; writes to `save.tmp` then replaces. On `Corrupt`, copies the file to `save.bad.json` before anything can overwrite it. On `NewerVersion`, sets a flag that disables `Save` for the session.

- [ ] **Step 1: Write failing tests**

```csharp
[Test] public void RoundTrip() { s.Coins = BigNum.FromParts(4.2, 320); s.Levels = new[] {12, 3, 0}; s.Progress[0] = 0.25; s.BuyMode = 10; s.LastSeenUnixSeconds = 99; s.HintSeen = true; var r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)); Assert.AreEqual(LoadStatus.Ok, r.Status); Assert.IsTrue(r.State.Coins.Equals(s.Coins)); CollectionAssert.AreEqual(s.Levels, r.State.Levels); Assert.AreEqual(0.25, r.State.Progress[0], 1e-12); Assert.AreEqual(10, r.State.BuyMode); Assert.AreEqual(99, r.State.LastSeenUnixSeconds); Assert.IsTrue(r.State.HintSeen); }
[TestCase(null)] [TestCase("")] [TestCase("   ")] public void Empty_Starts_Fresh(string j) { var r = SaveCodec.Decode(defs, j); Assert.AreEqual(LoadStatus.Empty, r.Status); Assert.AreEqual(1, r.State.Levels[0]); }
[TestCase("{")] [TestCase("not json")] [TestCase("{\"version\":1,\"stations\":[{\"id\":\"tea\",\"level\":")] [TestCase("[1,2,3]")] public void Corrupt_Starts_Fresh(string j) => Assert.AreEqual(LoadStatus.Corrupt, SaveCodec.Decode(defs, j).Status);
[Test] public void Newer_Version_Is_Refused() => Assert.AreEqual(LoadStatus.NewerVersion, SaveCodec.Decode(defs, "{\"version\":99}").Status);
[Test] public void Matches_Stations_By_Id() { var json = SaveCodec.Encode(defs, s2 /* levels 5,4,3 */); var reordered = new[] { defs[2], defs[0], defs[1] }; CollectionAssert.AreEqual(new[] {3, 5, 4}, SaveCodec.Decode(reordered, json).State.Levels); }
[Test] public void Unknown_And_Missing_Ids_Are_Tolerated() { var r = SaveCodec.Decode(defs, "{\"version\":1,\"coinsMantissa\":5,\"coinsExponent\":0,\"stations\":[{\"id\":\"ghost\",\"level\":9,\"progress\":0}],\"buyMode\":1,\"lastSeen\":0,\"hintSeen\":false}"); Assert.AreEqual(LoadStatus.Ok, r.Status); CollectionAssert.AreEqual(new[] {1, 0, 0}, r.State.Levels); }
[Test] public void Bad_Values_Are_Sanitized() { var r = SaveCodec.Decode(defs, "{\"version\":1,\"coinsMantissa\":-3,\"coinsExponent\":0,\"stations\":[{\"id\":\"tea\",\"level\":-7,\"progress\":55}],\"buyMode\":7,\"lastSeen\":0,\"hintSeen\":false}"); Assert.IsTrue(r.State.Coins.Equals(BigNum.Zero)); Assert.AreEqual(1, r.State.Levels[0]); Assert.AreEqual(0, r.State.Progress[0]); Assert.AreEqual(1, r.State.BuyMode); }
```

- [ ] **Step 2:** Run `$TEST`, expect FAIL.
- [ ] **Step 3:** Implement `SaveCodec`, then `SaveStore`.
- [ ] **Step 4:** Run `$TEST`, expect PASS.
- [ ] **Step 5:** Commit `feat: save and load with versioning`.

### Task 6: Station content as data

**Files:**
- Create: `Assets/VuonTra/Game/Data/StationData.cs`, `GameCatalog.cs`, `Assets/VuonTra/Content/Stations/*.asset`, `Assets/VuonTra/Content/GameCatalog.asset`, `Assets/VuonTra/Game/Editor/ContentSeeder.cs`
- Test: `Assets/Tests/EditMode/CatalogTests.cs` (add `VuonTra.Game` + `UnityEngine`/`UnityEditor` references to the test asmdef)

**Interfaces:**
- Produces: `StationData : ScriptableObject` (fields mirror `StationDef` plus `Sprite Icon`, `Sprite Prop`, `Color Accent`, `StationData[] Boosts`) with `StationDef ToDef()`; `GameCatalog : ScriptableObject { StationData[] Stations; IReadOnlyList<StationDef> BuildDefs(); }`.
- `ContentSeeder` menu item `VuonTra/Seed Content` creates the assets below so they are reproducible; also callable as `-executeMethod VuonTra.Game.EditorTools.ContentSeeder.Seed`.

Content, in unlock order:

| id | Tên | Kind | BaseCost | Growth | BaseIncome | Cycle (s) | Boosts |
|---|---|---|---|---|---|---|---|
| `am-tra` | Ấm trà sương | Brew | 10 | 1.09 | 1 | 1.5 | |
| `gio-la` | Giỏ lá trà | Ingredient | 120 | 1.11 | | | `am-tra`, `tra-sua` |
| `noi-mut` | Nồi mứt dâu | Brew | 1 400 | 1.12 | 80 | 5 | |
| `coi-hoa` | Cối giã hoa | Ingredient | 16 000 | 1.13 | | | `noi-mut`, `binh-sao` |
| `lo-banh` | Lò bánh hạt dẻ | Brew | 190 000 | 1.13 | 6 500 | 12 | |
| `to-mat` | Tổ mật ong | Ingredient | 2 400 000 | 1.14 | | | `lo-banh`, `noi-mut` |
| `tra-sua` | Quầy trà sữa trăng | Brew | 32 000 000 | 1.14 | 600 000 | 24 | |
| `binh-sao` | Bình ủ sao | Brew | 450 000 000 | 1.15 | 6 000 000 | 32 | |

- [ ] **Step 1: Write failing tests:** catalog asset loads via `AssetDatabase`; has 8 stations in the order above; ids unique; first is Brew; every `Boosts` target is a Brew station in the catalog; every Brew has `BaseIncome > 0` and `CycleSeconds > 0`; every `CostGrowth > 1`.
- [ ] **Step 2:** Run `$TEST`, expect FAIL.
- [ ] **Step 3:** Implement the two ScriptableObjects and the seeder; run the seeder.
- [ ] **Step 4:** Run `$TEST`, expect PASS.
- [ ] **Step 5:** Commit `feat: station catalog as data`.

### Task 7: Playable garden scene

**Files:**
- Create: `GameRunner.cs`, all `UI/*.cs`, prefabs `Assets/VuonTra/Prefabs/StationActor.prefab`, `StationRow.prefab`; CC0 placeholder sprites under `Art/Placeholder/` with a `CREDITS.md` naming pack, author and licence
- Modify: `Assets/VuonTra/Scenes/Garden.unity`

**Interfaces:**
- Consumes: everything above.
- Produces: `GameRunner : MonoBehaviour { GameCatalog Catalog; GameState State { get; } IReadOnlyList<StationDef> Defs { get; } event Action StateChanged; event Action<int, BigNum> Payout; event Action<BigNum, double> AwayClaimed; bool Buy(int index); bool Tap(int index); void SetBuyMode(int mode); }`
  - `Awake`: build defs, `SaveStore.Load`, `ClaimAway` with `DateTimeOffset.UtcNow`; raise `AwayClaimed` when the payout is at least 1.
  - `Update`: `Tick(Time.unscaledDeltaTime)` clamped to 1 s per frame; frames longer than 2 s go through `ClaimAway` instead.
  - Autosave every 5 s, on `OnApplicationPause(true)` and `OnApplicationQuit`.

Screen layout (portrait 1080×1920, Canvas Scaler "Scale With Screen Size", match 0.5):

```
┌──────────────────────────┐
│ Vườn Trà Thỏ Trăng    💧 12,3K │  HUD: total + "+3,2/giây"
├──────────────────────────┤
│                          │
│   garden: 8 station      │  top ~55%: backdrop + StationActor per station,
│   actors on a 2-wide     │  2 columns × 4 rows, locked ones shown asleep
│   staggered grid         │
│                          │
├──────────────────────────┤
│ [×1] [×10] [Tối đa]      │
│ ▢ Ấm trà sương  Cấp 12   │  bottom ~40%: scrolling upgrade rows
│   9 sương / 3,0 giây  [Nâng +1  120] │
│ ▢ Giỏ lá trà   Chưa mở   │
└──────────────────────────┘
```

Row text: Brew `"{income} sương / {cycle} giây"`, Ingredient `"Tăng giá ×{boost} cho {tên các trạm}"`, plus `" · ×2 ở cấp {next}"` when a milestone remains. Locked row button: `"Mở trạm"` + cost; disabled with `"Mở trạm phía trên trước"` when the previous station is locked. Away popup: title `"Mừng bạn quay lại"`, amount, `"trong {n} phút"`, button `"Nhận sương"`.

- [ ] **Step 1:** Implement `GameRunner`. Add an EditMode test that constructs it on a temporary GameObject with the real catalog and asserts `State.Levels[0] == 1` and `Defs.Count == 8`.
- [ ] **Step 2:** Import a CC0 2D pack for placeholders (Kenney, kenney.nl, CC0); record it in `CREDITS.md`. Assign `Icon`/`Prop` on each StationData.
- [ ] **Step 3:** Build `HudView`, `UpgradePanelView` + `StationRowView` (rows refresh on `StateChanged`, throttled to 6 times a second).
- [ ] **Step 4:** Build `GardenView` + `StationActorView`: progress bar from `State.Progress`, tap → `GameRunner.Tap`, floating `+N` on `Payout`, asleep look when level 0.
- [ ] **Step 5:** Build `AwayPopupView`.
- [ ] **Step 6:** Run `$TEST`, expect PASS.
- [ ] **Step 7:** Verify in the Editor in Play mode at 1080×1920: coins rise without input; tapping a station speeds it; buying raises the level and lowers coins; unlocking station 2 raises station 1's income; stop and start Play → levels kept; set `lastSeen` in the save 10 minutes back → away popup shows about 10 minutes of income. Capture a screenshot.
- [ ] **Step 8:** Commit `feat: playable garden scene`.

### Task 8: Build and hand over

- [ ] **Step 1:** Make a macOS standalone build to `Build/Mac/` via `-batchmode -buildTarget StandaloneOSX` and an Editor build script `Assets/VuonTra/Game/Editor/BuildScript.cs` (`BuildMac`).
- [ ] **Step 2:** Launch the build once; confirm it starts in a portrait-shaped window and saves between runs.
- [ ] **Step 3:** Commit `chore: build script`. Report to the user what is playable, what was verified, and what milestone 2 adds.
