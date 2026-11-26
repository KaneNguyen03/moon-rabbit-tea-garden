# Sprint 001 — 2026-11-26 đến 2026-12-03

> Gate PR-SPRINT: bỏ qua (review mode `lean`). QA plan: `/qa-plan sprint` chạy trước khi bắt đầu story 146 (QA D được giao ngay khi plan này được ghi).

## Sprint Goal
Vertical slice 2.5D chơi được trên Mac: đảo cỏ 3D với camera nghiêng cố định, trạm, đồ và thỏ dựng đứng có bóng thật, ánh trăng và đèn lồng; kéo, zoom, chạm trạm và bản đồ hoạt động như bản 2D (mốc M1 của epic Vườn 2.5D).

## Goal Prerequisites
| Needed to show the goal | Status |
|-------------------------|--------|
| Scene vườn (`Assets/VuonTra/Scenes/Garden.unity`) | exists |
| Cảnh 2.5D: đảo cỏ mesh, camera nghiêng, renderer URP 3D | built by story-145 |
| Bản đồ #91 qua RenderTexture trên cảnh 2.5D | built by story-145 |
| 18 asset vẽ theo `design/assets/asset-manifest.md` (#146–#148) | **missing** trong story — session thiết kế đang vẽ ngoài sprint (nhánh ve-25d); nếu trễ, story-146 dùng hình 2D hiện có làm billboard tạm |
| Mốc hiệu năng iPhone 11 / Snapdragon 665 (#149) | chủ project chốt 26/11, dùng trong story-149 |

## Capacity
- Total days: 7 (một agent cảnh của B tại một thời điểm, vì story 145–148 cùng sửa `GardenScreen` và scene; session thiết kế vẽ song song; QA D nghiệm thu từng story)
- Buffer (20%): 1,5 ngày cho lỗi đường dẫn, pin, hàng đợi Unity
- Available: 5,5 ngày

## Tasks

### Must Have (Critical Path)
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| 2.5d-145 | Đất và camera (#145) | B / agent-145 + design | 2 | ADR-0005 | story-145: 5 tiêu chí; spike Forward+ và bóng sprite trước |
| 2.5d-146 | Trạm, đồ, thỏ dựng đứng; bảng tỉ lệ; vòng làm việc ba trạm mẫu (#146) | B / agent-146 + design (asset) | 2 | 2.5d-145, asset #146 | story-146: 5 tiêu chí; APPROVED visual-critic-gate |
| 2.5d-147 | Ánh sáng và bóng (#147) | B / agent-147 + design (asset) | 1 | 2.5d-146, asset #147 | story-147: 5 tiêu chí |

### Should Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| 2.5d-148 | Hậu kỳ, trời phía trên đảo, HUD trên cảnh 2.5D (#148) | B / agent-148 + design (ux-design HUD) | 1,5 | 2.5d-147, asset #148 | story-148: 4 tiêu chí |

### Nice to Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| 2.5d-149 | Hiệu năng mobile: draw call, bóng, cấp chất lượng, 60 fps (#149) | B / agent-149 | 1,5 | 2.5d-148 | story-149: 4 tiêu chí; đo theo iPhone 11 / SD665 hoặc profile quy đổi trong Editor |

## Carryover from Previous Sprint
| Task | Reason | New Estimate |
|------|--------|-------------|
| (không có — sprint đầu tiên của studio) | | |

## Risks
| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Asset #146–#148 vẽ trễ (nằm ngoài story) | Cao | Trung bình | 145 không cần asset nên làm trước; 146 dùng hình 2D hiện có làm billboard tạm, thay asset khi có |
| Forward+ và bóng sprite trên Unity 6000.6 mobile chưa tra nguồn (ADR-0005) | Trung bình | Cao | Spike nhỏ ngay trong story-145 trước khi dựng cảnh; nếu không được, dùng Forward thường + bóng blob |
| Máy chỉ có một đường verify, hay hết pin, worktree bị dọn nhầm | Cao | Trung bình | Cổng chung của B (tối đa 3 batch, 1 khi pin < 50 %), luật "chỉ xoá worktree của mình", cắm sạc |
| Story 145–148 cùng sửa `GardenScreen`/scene | Cao | Trung bình | Làm nối tiếp, mỗi story gộp xong mới bắt đầu story sau |

## Dependencies on External Factors
- Sạc 68 W cắm liên tục (Unity bị chặn khi pin < 30 %).
- Session thiết kế (ve-25d) giao asset theo `design/assets/*-25d-assets.md`.
- Chủ project duyệt #91 (bản đồ 2D) và các mục cần-chủ-project nằm ngoài sprint.

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

> **Scope check:** sprint chỉ gồm năm story của epic Vườn 2.5D; chạy `/scope-check vuon-2-5d` nếu có story thêm ngoài epic.
