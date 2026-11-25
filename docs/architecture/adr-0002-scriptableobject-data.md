# ADR-0002: Trạm, nâng cấp, nhiệm vụ, vật phẩm là asset ScriptableObject; Core chỉ thấy bản `*Def` thuần

> **Reverse-documented** from code and history at commit 9663644 on 2026-11-26; rationale not stated in docs is marked INTENT UNKNOWN.

## Status

Accepted

## Date

2026-11-21: `StationData` + `GameCatalog` (`1cd61c1` "playable garden with eight stations"), `UpgradeData` + `QuestData` (`8c716a0`). `ItemData` thêm 2026-11-22 (`ed6acdd`). Luật "Data-Driven Modularity" vào `CLAUDE.md` ngày 2026-11-24 (`d89852c`).

## Last Verified

2026-11-26

## Decision Makers

Chủ project và session lập kế hoạch mốc 0–1 (`docs/superpowers/plans/2026-11-21-moc-0-1-vong-choi-chinh.md`: "Content is data, so later milestones add stations, sprites and events as assets").

## Summary

Nội dung chơi cần chỉnh và thêm mà không sửa luật, trong khi Core không được biết Unity (ADR-0001). Trạm, nâng cấp, nhiệm vụ và vật phẩm là asset ScriptableObject dưới `Assets/VuonTra/Content/**`, gom trong `GameCatalog.asset`; mỗi lớp có `ToDef()` chép sang lớp thuần của Core (`StationDef`, `UpgradeDef`, `QuestDef`, `ItemDef`).

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.10f1 |
| **Domain** | Core |
| **Layer** | Foundation |
| **Knowledge Risk** | LOW (`ScriptableObject`, `CreateAssetMenu`, `AssetDatabase` ổn định nhiều đời) |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 |
| **Enables** | ADR-0005 (sprite trạm và thỏ giữ trong asset cho billboard) |
| **Blocks** | Epic thêm trạm, nhiệm vụ, vật phẩm |
| **Ordering Note** | Seeder Editor là nguồn sự thật của số liệu (xem Current State) |

## Context

### Problem Statement

Số liệu trạm, nhiệm vụ, vật phẩm và hình vẽ đi kèm cần một chỗ dữ liệu Unity tham chiếu được sprite, nhưng Core thuần C# không đọc được ScriptableObject.

### Current State

- `Assets/VuonTra/Game/Data/`: `StationData`, `UpgradeData`, `QuestData`, `ItemData`, `GameCatalog` (đều `ScriptableObject`, `[CreateAssetMenu]`).
- `Assets/VuonTra/Content/`: 16 trạm, 4 nâng cấp, 47 nhiệm vụ, 12 vật phẩm, cộng `GameCatalog.asset`.
- `GameCatalog.BuildDefs/BuildUpgrades/BuildQuests/BuildItems` gọi `ToDef()`; `GameRunner.Init` dùng chúng (ADR-0001). `StationData.ToDef()` đổi `StationData[] Boosts` thành `string[] BoostsIds`, bỏ sprite và màu.
- `GameCatalog` còn giữ toàn bộ hình chung (thỏ, bảng, nền, đồ trang trí, cá, cổng trăng…) cho View.
- **Số liệu thật nằm trong code Editor**: `Editor/ContentSeeder.cs` (mảng `Rows` của 16 trạm) và `Editor/MetaSeeder.cs` (nâng cấp, nhiệm vụ, vật phẩm) ghi đè mọi trường của asset mỗi lần `VuonTra/Seed Content` chạy, và `tools/verify.sh` chạy seed ở bước 1/4. Sửa tay một `.asset` sẽ mất ở lần verify sau.
- **Nhiều bảng nội dung là mảng tĩnh trong Core, không phải ScriptableObject**: `RabbitKinds.Table` (76 loại thỏ, từ `cc79c47` 22/11), `Offers.All`, `Storefront.Packs`, `Fairs`, `AlbumSets`, `Decor.Kinds`, `GiftCodes.Table`.
- Test lõi dựng `StationDef` bằng tay (`Assets/Tests/EditMode/Fixture.cs`), không cần asset.

### Constraints

- Core không tham chiếu Unity (ADR-0001), nên dữ liệu phải đi qua bản thuần.
- Sprite phải được Unity tham chiếu để vào build.

### Requirements

- Thêm trạm, nhiệm vụ, vật phẩm không sửa luật Core.
- Test luật chạy với dữ liệu giả không cần asset.

## Decision

Mỗi loại nội dung có hai lớp: ScriptableObject trong `VuonTra.Game` (số liệu + hình) và lớp `*Def` thuần trong Core (chỉ số liệu). `GameCatalog` là gốc duy nhất `GameRunner` nhận qua trường public `GameRunner.Catalog`.

### Architecture Diagram

```
Editor/ContentSeeder.Rows, MetaSeeder rows  --Seed-->  Content/**/*.asset (StationData, QuestData...)
                                                          |
                                              GameCatalog.asset (+ shared sprites)
                                                          | Build*() / ToDef()
                                              List<StationDef/UpgradeDef/QuestDef/ItemDef>  --> Core
```

### Key Interfaces

- `StationData.ToDef() -> StationDef`, tương tự cho `UpgradeData`, `QuestData`, `ItemData`.
- `GameCatalog.BuildDefs()`, `BuildUpgrades()`, `BuildQuests()`, `BuildItems()`.

### Implementation Guidelines

- Trường số liệu mới must có ở cả ScriptableObject và `*Def`, và `ToDef()` must chép nó.
- Core must never nhận `ScriptableObject`, `Sprite` hay `Color`.
- Đổi số liệu trạm, nâng cấp, nhiệm vụ, vật phẩm must sửa ở `ContentSeeder`/`MetaSeeder`, không sửa tay `.asset`.
- Hình mới cho trạm hoặc thỏ must gắn qua asset hoặc `GameCatalog`, không nạp bằng đường dẫn trong View.

## Alternatives Considered

Không có phương án thay thế nào được ghi lại. Việc dữ liệu loại thỏ, gói, lễ hội nằm trong mảng tĩnh Core thay vì ScriptableObject không có lý do trong docs: INTENT UNKNOWN.

## Consequences

### Positive

- Luật Core test được với `Fixture` nhỏ; asset không cần cho 963 test lõi.
- Một `GameCatalog` cho View mọi hình chung; đổi hình là đổi tệp.

### Negative

- Hai nguồn sự thật cho số liệu (seeder và asset), seeder thắng; người sửa asset trong Inspector bị ghi đè.
- Mỗi trường mới phải chép ở ba chỗ (seeder, ScriptableObject, `*Def`).
- Luật `CLAUDE.md` "Use ScriptableObject for all items, characters, and station stats" chưa đúng với nhân vật: 76 loại thỏ là bảng tĩnh `RabbitKinds.Table`.

### Neutral

- `GameCatalog.cs` còn 22 comment trong `tools/conventions.baseline`; ratchet chỉ cho giảm.

## Risks

- Sửa tay asset mất khi verify. Giảm thiểu: ghi luật seeder trong control manifest.
- Bảng tĩnh Core lớn dần, xa luật dữ liệu của `CLAUDE.md`. Giảm thiểu: quyết riêng (ADR mới) có chuyển `RabbitKinds` sang ScriptableObject + bản thuần hay sửa luật.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| [tram-va-thu-nhap.md](../../design/gdd/tram-va-thu-nhap.md) | Số liệu 16 trạm, nâng cấp toàn vườn | `StationData`, `UpgradeData` → `StationDef`, `UpgradeDef` |
| [nhiem-vu-ngay-tuan-thang.md](../../design/gdd/nhiem-vu-ngay-tuan-thang.md) | 47 nhiệm vụ | `QuestData` → `QuestDef` |
| [tho-va-so-suu-tam.md](../../design/gdd/tho-va-so-suu-tam.md) | Vật phẩm (mũ, bùa) | `ItemData` → `ItemDef`; loại thỏ dùng `RabbitKinds.Table` (ngoại lệ) |

GDD đang viết song song; đối chiếu lại khi xong.

## Performance Implications

- **CPU**: `ToDef()` chạy một lần ở `Init`.
- **Memory**: `GameCatalog` giữ tham chiếu mọi sprite chung, tất cả nạp khi scene mở.
- **Load Time**: tỉ lệ với số sprite trong `GameCatalog` (chưa đo).
- **Network**: không có.

## Migration Plan

Không cần cho phần đã có. Nếu chuyển `RabbitKinds`, `Offers`… sang ScriptableObject thì làm theo cùng mẫu `*Data.ToDef()` và giữ test Core bằng `Fixture`.

**Rollback plan**: không áp dụng.

## Validation Criteria

- `CatalogTests` (Unity EditMode) xanh: 16 trạm theo thứ tự mở, id duy nhất, có hình, liên kết boost đi qua `Defs`.
- `grep -rn "ScriptableObject" Assets/VuonTra/Core` không có kết quả.
- `tools/verify.sh` bước seed chạy xong và `git status` không đổi `Assets/VuonTra/Content/**` (seeder và asset khớp).

## Related

- ADR-0001 (Core thuần), ADR-0005 (hình 2D giữ làm billboard).
- `CLAUDE.md` "Unity Anti-Slop Rules" mục 2.
- `Assets/VuonTra/Game/Data/GameCatalog.cs`, `Assets/VuonTra/Editor/ContentSeeder.cs`, `Assets/VuonTra/Editor/MetaSeeder.cs`, `Assets/VuonTra/Core/RabbitKinds.cs`.
