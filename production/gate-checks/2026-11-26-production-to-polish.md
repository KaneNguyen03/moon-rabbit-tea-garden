## Gate Check: Production → Polish

**Date**: 2026-11-26
**Checked by**: gate-check skill (session studio-adopt), `workflow: standard`, `qa.level: standard`, `review_mode: lean`, `project.stage: Production`
**Director Panel**: không chạy trong lần này. Lý do: 9 GDD và 5 ADR vừa được viết ngược từ code hôm nay và chưa qua `/design-review`; panel CD/TD/PR/AD sẽ chấm tài liệu chưa ai duyệt. Chạy lại `/gate-check polish` (có panel) sau khi M1 xong.

### Required Artifacts (mức `standard`): 1/4 present, 1 not assessed

- [x] **Core mechanics implemented** — 9 hệ thống trong [systems-index](../../design/gdd/systems-index.md) đều có code trong `Assets/VuonTra/Core` (62 file) và `Game` (96 file); mỗi GDD đối chiếu code và test theo tên.
- [ ] **Smoke check report** trong `production/qa/` — MISSING. Có bằng chứng thô: `tools/ci/core-tests.sh` 963/963 qua và `python3 -I tools/conventions.py` "ok (1517 known violations left)" chạy 26/11 trong worktree này, nhưng chưa có báo cáo `/smoke-check` và chưa ai mở bản build.
- [?] **Main path playable end-to-end** — NOT ASSESSED trong lần này: bản Mac `Build/Mac/VuonTraTho.app` có theo `docs/bao-cao-po.md`, nhưng session này không được chạy Unity hay build. Hơn nữa cảnh vườn đang chuyển sang 2.5D (#145–#149), nên đường chơi chính sẽ đổi.
- [ ] **≥ 1 playtest ghi lại** trong `production/qa/playtests/` — MISSING. Ghi chú nghiệm thu QA có trong thư mục ghi chú chung của các session (ngoài repo), không phải báo cáo playtest.
- [x] **Không có lỗi S1 Critical đang mở** — lỗi mở duy nhất có nhãn `bug` là KaneNguyen03/moon-rabbit-tea-garden#5 (chụm hai ngón trên trackpad Mac), không chặn đường chơi trên mobile.

Recommended ở mức `standard` (thiếu thì là CONCERNS):
- [ ] QA sign-off (`/team-qa`) — chưa có.
- [ ] Test theo story — chưa có story nào hoàn tất theo studio; 5 story mới ở `production/epics/vuon-2-5d/`.
- [ ] QA plan (`/qa-plan`) — chưa có.

### Quality Checks

- [x] Core tests: 963 qua, 0 lỗi (dotnet, không Unity). Test EditMode cần Unity (GardenScreenTests, MapViewScreenTests…) — NOT ASSESSED lần này; B chạy `tools/verify.sh`.
- [?] Performance within budget — NOT ASSESSED: `performance.target_framerate: 60` đã đặt nhưng chưa có số đo; story-149 đo trên iPhone 11 và Snapdragon 665.
- [ ] Mọi màn đã làm có UX spec — chưa có `design/ux/` (kế hoạch di trú 4.2).
- [ ] Accessibility theo `design/accessibility-requirements.md` — file chưa có (kế hoạch 4.3).
- [?] Core loop chạy như GDD — GDD viết ngược từ code nên khớp theo định nghĩa; câu hỏi thật là ý định (`INTENT UNKNOWN`), chờ `/design-review`.

### Blockers

1. **Không có báo cáo smoke check** — session QA chạy `/smoke-check` (lệnh `commands.smoke` không cần Unity) và B mở bản build một lần.
2. **Không có playtest ghi lại** — `/playtest-report` sau story-147 (vườn 2.5D chơi được).
3. **Đường chơi chính đang được làm lại** — cảnh vườn 2.5D (epic `vuon-2-5d`) phải xong M1 trước khi Polish có nghĩa.

### Thiếu gì để qua mốc kế tiếp

**M1 — Vertical slice 2.5D** (story-145, 146, 147; `docs/de-xuat-2-5d.md` §6 bước 1–3):
- [ ] `/setup-engine refresh` cho 6000.6 (Forward+, bóng sprite) — trước story-145
- [ ] `/sprint-plan` sprint 1 tuần đầu tiên từ story-145 (và story-149 đo song song)
- [ ] story-145 → 146 → 147 qua `/dev-story`, `/story-done`, ảnh `docs/anh/po-<issue>/` + `visual-critic-gate` APPROVED
- [ ] `/vertical-slice` báo cáo + 1 playtest (`/playtest-report`)
- [ ] gate hồi tố Pre-Production → Production còn thiếu: `docs/architecture/architecture.md` (`/create-architecture`), `control-manifest.md`, UX spec màn vườn và HUD, sprint plan

**M2 — Phát hành mềm Android rồi Release** (`docs/phat-hanh/LO-TRINH-KIEM-TIEN.md`, `docs/phat-hanh/CHECKLIST.md`):
- [ ] Gate này (Production → Polish) PASS: smoke report, playtest, story-148, story-149 đạt 60 fps
- [ ] Gate Polish → Release: `/balance-check` (nhịp #98, #99), `/security-audit` (save, gift code, IAP), `/localize` vi/en, không còn lỗi S1–S3, `/release-checklist` dẫn tới CHECKLIST.md
- [ ] Việc của chủ dự án trong LO-TRINH giai đoạn 0: tài khoản Google Play, chính sách quyền riêng tư công khai, chọn SDK quảng cáo/analytics; quyết bật cờ kiếm tiền (#144)

### Recommendations

- Chủ dự án trả lời các câu `INTENT UNKNOWN` trong 9 GDD, rồi `/design-review` từng file.
- Giữ `project.stage: Production` tới khi M1 xong; chạy lại gate này lúc đó, có Director Panel.

### Verdict: FAIL

Hai artefact bắt buộc ở mức `standard` không có (smoke report, playtest) và đường chơi chính đang làm lại. Stage giữ `Production`.
