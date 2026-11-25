# Sprint 001 — 2026-11-26 to 2026-12-02

> Lập bởi `/sprint-plan new` trong vòng điều phối đêm 26/11 (chủ dự án giao điều phối tới 5h; PO management đang chờ một câu hỏi cho chủ dự án). PR-SPRINT skipped — Lean mode. Không có milestone file — lập từ backlog story; không có risk register — rủi ro đánh giá từ nội dung sprint.

> ⚠️ **No QA Plan**: Sprint này bắt đầu khi chưa có QA plan. Session QA chạy `/qa-plan sprint` trước khi story cuối được làm. Gate Production → Polish cần QA sign-off, và sign-off cần QA plan.

## Sprint Goal
Vườn mở ra là đảo cỏ 2.5D có mép đất, camera nghiêng, kéo, zoom, chạm và bản đồ chạy như bản 2D (story-145), với hình ba trạm mẫu sẵn sàng dựng đứng (story-146).

## Goal Prerequisites
| Needed to show the goal | Status |
|-------------------------|--------|
| Scene vườn trong build | exists — `Assets/VuonTra/Scenes/Garden.unity` (EditorBuildSettings) |
| `UniversalRendererData` Forward+ cho scene vườn | built by story-145 |
| Hình đất, lối mòn, ba trạm mẫu, đèn, trời | exists — trên main (ve-25d `bcf606f`, `3e15530`), 17/18 asset Done |
| ASSET-016 cây và bụi viền đảo | **missing** — Gemini hết hạn mức tới ~08:10 26/11; design vẽ sáng nay |
| Engine reference 6000.6 (Forward+, bóng sprite) | **missing** — `/setup-engine refresh` (kế hoạch di trú 2.3) |

## Capacity
- Total days: 5 (B code, design hình và UX, QA)
- Buffer (20%): 1 ngày
- Available: 4 ngày

Backlog Ready có 5 story, ít hơn khoảng 6–10 của `balanced`, nên lập cả năm; chỉ story-145 là Must Have.

## Tasks

### Must Have (Critical Path)
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| 145 | [Đất và camera](../epics/vuon-2-5d/story-145-dat-va-camera.md) (#145) | B; design mesh đảo và lối mòn | 4 | — | Story-145 AC 1–5; `visual-critic-gate` APPROVED; `verify.sh` xanh |

### Should Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| 149 | [Hiệu năng mobile](../epics/vuon-2-5d/story-149-hieu-nang-mobile.md) (#149) — chỉ đo nền sau story-145 | B | 0.5 | 145 | Số đo khung vườn đầy ghi vào `docs/bao-cao-po.md` |
| 146 | [Dựng đứng, bảng tỉ lệ, ba trạm mẫu](../epics/vuon-2-5d/story-146-dung-dung-billboard.md) (#146) | B; design bảng tỉ lệ | 3 | 145 | Story-146 AC 1–4 |

### Nice to Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| 147 | [Ánh sáng và bóng](../epics/vuon-2-5d/story-147-anh-sang-va-bong.md) (#147) | B; design chỉnh màu | 3 | 146 | Story-147 AC 1–4 |
| 148 | [Hậu kỳ và HUD](../epics/vuon-2-5d/story-148-hau-ky-va-hud.md) (#148) | B; design `design/ux/hud.md` | 2 | 147 | Story-148 AC 1–3 |

## Carryover from Previous Sprint
| Task | Reason | New Estimate |
|------|--------|-------------|
| — | Sprint đầu tiên | — |

## Risks
| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| ASSET-016 chưa vẽ (hạn mức Gemini) | High | Low | Viền đảo tạm bằng cây hiện có; design vẽ sau 08:10 |
| Forward+ và bóng sprite trên Unity 6000.6 chưa tra nguồn | Medium | Medium | `/setup-engine refresh` trước khi gộp story-145 |
| Máy chỉ chạy tối đa 3 Unity batch; chỉ B chạy Unity | Medium | Medium | Design và QA không mở Unity; B xếp lượt |
| Chưa có QA plan | High | Medium | QA chạy `/qa-plan sprint` sáng 26/11 |

## Dependencies on External Factors
- Chủ dự án: trả lời câu hỏi đang chờ ở PO management; chọn máy test đã chốt (iPhone 11, Snapdragon 665).
- Hạn mức Gemini cho ASSET-016.

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-001-[date].md`, from `/qa-plan sprint`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged
