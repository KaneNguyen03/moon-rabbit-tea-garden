# Project Stage Analysis

**Date**: 2026-11-26
**Stage**: Production
**Stage Confidence**: PASS — `project.stage: Production` khớp heuristic (175 file C# trong `Assets/VuonTra`, phát triển đang diễn ra)

## Completeness Overview

- **Design**: 9 GDD hệ thống + game-concept + systems-index, tất cả `In Review` (viết ngược từ code 26/11); art bible studio 9 mục trên main (`design/art/art-bible.md`, AD sign-off CONCERNS). Chưa có UX spec, accessibility, entity registry.
- **Code**: 175 file C#, khoảng 45 800 dòng — Core 62 file (thuần C#, `noEngineReferences`), Game 96, Editor 17.
- **Architecture**: 5 ADR Accepted (Core thuần C#, ScriptableObject data, seam Storefront/Ads/Analytics, cờ Monetization, hướng 2.5D); thiếu `architecture.md`, `control-manifest.md`, `tr-registry.yaml`.
- **Production**: 1 epic (`vuon-2-5d`), 5 story Ready (#145–#149), 0 Complete; chưa có sprint plan, `sprint-status.yaml`, milestone file. Việc vẫn theo GitHub issue (ánh xạ một issue = một story).
- **Tests**: 112 file test EditMode; 963 test Core chạy không cần Unity, tất cả qua (26/11). Test cần Unity chưa chạy trong lần này.

## Stage Classification Rationale

Code đã đủ mọi hệ thống của vòng chơi; các artefact giai đoạn trước (concept, GDD, ADR) được bổ sung ngược hôm nay. Không có dấu hiệu Polish: cảnh vườn đang được làm lại sang 2.5D, chưa có smoke report hay playtest ([gate check](gate-checks/2026-11-26-production-to-polish.md): FAIL).

## Gaps Identified

1. Chưa có sprint plan — việc đang theo dõi bằng GitHub issue và nhãn; `/sprint-plan` lấy issue mở làm đầu vào.
2. 9 GDD còn câu `INTENT UNKNOWN` — cần chủ dự án trả lời trước `/design-review`.
3. Thiếu tài liệu kiến trúc tổng và control manifest — `/create-architecture`, `/architecture-review`, `/create-control-manifest`.
4. Chưa có UX spec cho màn vườn, HUD, Tiệm, Sổ thỏ, Nhiệm vụ — session design, `/ux-design`.
5. Engine reference 6000.6 chưa tra nguồn — `/setup-engine refresh` trước story-145.

## Recommended Next Steps

1. `/sprint-plan` — sprint 1 tuần đầu tiên: story-145 (B), mesh đảo và lối mòn (design), story-149 đo song song.
2. B: `/story-readiness` rồi `/dev-story production/epics/vuon-2-5d/story-145-dat-va-camera.md`.
3. Chủ dự án hoặc điều phối: trả lời câu hỏi mở trong GDD; `/design-review` từng GDD.

## Role-Specific Recommendations

- **B (code)**: `/setup-engine refresh`, story-145, `/code-review` trước gộp.
- **Design**: AD sign-off art bible sang APPROVED, `/ux-design` HUD và màn vườn 2.5D.
- **QA**: `/smoke-check`, `/qa-plan` sprint 1, `/playtest-report` sau story-147.
- **Điều phối**: `/sprint-plan`, `/sprint-status`, milestone M1/M2.

## Follow-Up Skills to Run

`/sprint-plan` · `/architecture-review` · `/create-control-manifest` · `/design-review` · `/ux-design` · `/smoke-check` · `/gate-check polish` (sau M1)

## Appendix: File Counts by Directory

| Thư mục | File |
|---|---|
| `Assets/VuonTra/Core` | 62 `.cs` |
| `Assets/VuonTra/Game` | 96 `.cs` |
| `Assets/VuonTra/Editor` | 17 `.cs` |
| `Assets/Tests/EditMode` | 112 `.cs` |
| `design/gdd` | 11 `.md` |
| `docs/architecture` | 5 ADR |
| `production/epics` | 1 epic, 5 story |
