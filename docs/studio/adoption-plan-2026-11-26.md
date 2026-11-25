# Adoption Plan

> **Generated**: 2026-11-26 (`/adopt full`, chạy tay theo `.claude/skills/adopt/` vì `.claude/hooks/yaml-helper.sh` không được cài)
> **Project phase**: Production (175 file code trong `Assets/VuonTra`; `project.stage: Production` đặt 26/11 — xem [gate-checks](../../production/gate-checks/2026-11-26-production-to-polish.md))
> **Engine**: Unity 6000.6.10f1 (URP 17.6, C#)
> **Template version**: Claude Code Game Studios 1.3.0, rigor `standard`, automation `guided`

Làm theo thứ tự. Đánh dấu từng mục khi xong. Chạy lại `/adopt` sau Bước 3.

Tài liệu này nằm ở `docs/studio/` (không phải `docs/adoption-plan-*.md` như skill mặc định) để mọi tài liệu chuyển đổi ở một chỗ; không skill nào đọc đường dẫn này ngoài lần chạy `/adopt` sau, và nó chỉ dùng để ghi "lần trước".

---

## Bước 0: Kết quả kiểm (trước nhánh `studio-adopt`)

| Loại | Trước | Sau nhánh này |
|---|---|---|
| Engine trong `project.yaml` | không có | Unity 6000.6.10f1, specialists, naming, commands |
| `modes.rigor` / `modes.automation` | không có (mặc định `minimal`/`collaborative`) | `standard` / `guided` |
| `design/gdd/game-concept.md` | không có | có (kèm pillars và Visual Identity Anchor) |
| `design/gdd/systems-index.md` | không có | có |
| GDD hệ thống | 0 | 9, đủ 5 mục bắt buộc của `standard` + Formulas |
| ADR `docs/architecture/adr-*.md` | 0 | 5, `Status: Accepted` |
| `docs/engine-reference/unity/VERSION.md` | không có | có, risk MEDIUM, phần breaking changes chưa tra nguồn |
| Story trong `production/epics/` | 0 (việc nằm trên GitHub issue) | 1 epic `vuon-2-5d`, 5 story Ready (#145–#149) |
| `production/sprint-status.yaml`, sprint plan | không có | không có |
| `docs/architecture/tr-registry.yaml`, `control-manifest.md`, `architecture.md` | không có | không có |
| Cấu hình v1.0 (`production/stage.txt`, `technical-preferences.md` có giá trị) | không — không cần chạy `migrate-v1-config.sh` | — |

## Bảng tương đương: artefact studio ↔ tài liệu đã có

Nguyên tắc: **một nguồn cho mỗi sự thật**. Tài liệu studio dẫn chiếu `docs/`, không chép lại.

| Artefact studio (đường dẫn skill đọc) | Tương đương đã có | Tình trạng | Cách xử lý |
|---|---|---|---|
| `design/gdd/game-concept.md` | rải rác ở `docs/phat-hanh/mo-ta-cua-hang.md`, `docs/nghien-cuu-cats-and-soup-vong-3.md` | đã viết | giữ; sửa khi chủ dự án xác nhận các chỗ `INTENT UNKNOWN` |
| `design/art/art-bible.md` (9 mục) | `docs/art-bible.md` (7 mục) + `docs/nghien-cuu-thiet-ke-do-hoa.md` §4 + chuẩn 2.5D ở `docs/de-xuat-2-5d.md` §5 | có trên main (`dd50a04`), dẫn chiếu `docs/` | Bước 2.1 |
| `docs/architecture/adr-*.md` | quyết định nằm trong `CLAUDE.md`, `docs/de-xuat-2-5d.md`, `LO-TRINH-KIEM-TIEN.md` "Cờ bán hàng" | đã viết 5 ADR | giữ; ADR mới qua `/architecture-decision` |
| `docs/architecture/architecture.md` | không có | **thiếu** | Bước 2.2 |
| `production/milestones/*.md` | `docs/de-xuat-2-5d.md` §6 (5 bước 2.5D), `docs/phat-hanh/LO-TRINH-KIEM-TIEN.md` (giai đoạn 0–4) | **thiếu** dạng studio | Bước 3.4 |
| `production/epics/*/story-*.md` | GitHub issue (#16 là hàng đợi MVP) | dùng issue làm story | ánh xạ ở [HUONG-DAN-STUDIO.md](HUONG-DAN-STUDIO.md) §3; không tạo file story song song |
| `production/session-state/active.md`, báo cáo story-done | `docs/bao-cao-po.md` (nhật ký bàn giao cho PO) | khác mục đích | giữ `bao-cao-po.md` là nhật ký PO; story-done ghi vào comment issue |
| `production/qa/` (qa-plan, smoke, playtests) | `docs/danh-sach-duyet.md`, ghi chú nghiệm thu QA trong thư mục ghi chú chung, `docs/anh/qa-*` | **thiếu** dạng studio | Bước 3.5 |
| `design/ux/*.md`, `design/ux/hud.md`, `design/ux/interaction-patterns.md` | không có; giao diện nằm trong code `Assets/VuonTra/Game/UI` và ảnh `docs/anh/` | **thiếu** | Bước 4.2 |
| `design/accessibility-requirements.md` | test `AccessibilityTests*.cs`, tương phản AA ở #121, #142 | **thiếu** văn bản | Bước 4.3 |
| Release checklist | `docs/phat-hanh/CHECKLIST.md`, `build-android.md`, `build-ios.md` | đủ nội dung | `/release-checklist` đọc và dẫn tới các file này |
| Asset audit | `docs/kiem-dinh-do-hoa.md` | đủ cho đợt 25/11 | lần sau dùng `/asset-audit` |
| Analytics / phễu | `docs/phat-hanh/PHEU-SU-KIEN.md` | đủ | giữ |
| Quyền riêng tư / khai báo dữ liệu | `docs/chinh-sach-quyen-rieng-tu.md`, `docs/khai-bao-du-lieu-cua-hang.md` | đủ | `/security-audit` đọc trước khi Polish |
| CI test | `.github/workflows/checks.yml`, `tools/ci/core-tests.sh` (963 test, 1 s, không cần Unity) | có, khác tên file | Bước 2.4 |
| Coding standards / forbidden patterns | `CLAUDE.md`, `tools/conventions.py` + baseline | mạnh hơn mẫu studio | giữ; studio không ghi đè (xem HUONG-DAN-STUDIO §5) |

---

## Step 1: Fix Blocking Gaps

Không còn khoảng trống BLOCKING sau nhánh này và hai commit mới trên main. Các mục BLOCKING đã xử lý:

- [x] Engine chưa cấu hình trong khi code đã có — `project.yaml` có `engine.*` (5 phút).
- [x] Không có `modes.rigor` nên mọi skill chạy ở `minimal` và bỏ qua GDD/ADR — đặt `standard` (5 phút).

Một vấn đề BLOCKING về **hạ tầng studio**, không về tài liệu:

- [x] **1.1 Skill studio gọi `.claude/hooks/yaml-helper.sh` không tồn tại.** Khi chuyển đổi này bắt đầu, mọi skill studio lỗi ngay ở dòng `!bash …/hooks/yaml-helper.sh resolve_config` ("No such file or directory"), nên các bước dưới được làm bằng cách đọc SKILL.md bằng tay. **Đã sửa trên main** ở `be7bb76`: helper được chép riêng, không đăng ký hook nào. Từ giờ gọi skill qua Skill tool bình thường.

---

## Step 2: Fix High-Priority Gaps

- [x] **2.1 Art bible ở đường dẫn studio.** Session design đã viết `design/art/art-bible.md` đủ 9 mục trên main (`dd50a04`), dẫn chiếu `docs/art-bible.md` và `docs/de-xuat-2-5d.md`; ký duyệt AD-ART-BIBLE đang là CONCERNS (xem cuối file đó). Còn lại: đưa trạng thái về APPROVED trước gate Pre-Production → Production.
- [ ] **2.2 Không có tài liệu kiến trúc tổng.** Chạy `/create-architecture` (đọc 5 ADR + `Assets/VuonTra/{Core,Game}`) → `docs/architecture/architecture.md`. **Thời gian:** 1 phiên.
- [ ] **2.3 Engine reference chưa tra nguồn** cho 6000.6 (breaking changes, Forward+ trên mobile). Chạy `/setup-engine refresh` trước khi B làm #145. **Thời gian:** 30 phút.
- [ ] **2.4 Studio tìm CI ở `.github/workflows/tests.yml`.** Dự án có `checks.yml`. Không đổi tên CI; ghi `testing.framework: nunit` (đã làm) và coi `/test-setup` là xong — ghi chú trong gate report. Nếu muốn artifact-check báo PRESENT, thêm alias trong `workflow-catalog.yaml` (việc của B, không cần gấp). **Thời gian:** 5 phút.

---

## Step 3: Bootstrap Infrastructure

### 3a. Register existing requirements (creates tr-registry.yaml)
Chạy `/architecture-review` — dựng TR registry từ 9 GDD và 5 ADR hiện có.
**Time**: 1 session
- [ ] `docs/architecture/tr-registry.yaml` created

### 3b. Create control manifest
Chạy `/create-control-manifest` từ 5 ADR Accepted. Manifest phải lặp lại luật của `CLAUDE.md` (không comment, không toán hoạt ảnh, Update rỗng), không thêm luật trái với `tools/conventions.py`.
**Time**: 30 min
- [ ] `docs/architecture/control-manifest.md` created

### 3c. Create sprint tracking file
Chạy `/sprint-plan new` với sprint 1 tuần (`cadence.sprint_length: 1w`) lấy issue mở trên GitHub, rồi `/sprint-plan update`.
**Time**: 30 min
- [ ] `production/sprints/sprint-01.md` và `production/sprint-status.yaml` created

### 3d. Set authoritative project stage
Điều phối yêu cầu đặt thẳng `Production` (khớp heuristic) thay vì chờ gate vào Production, gate này hiện FAIL vì thiếu epics và sprint plan. Đã ghi bằng `bash .claude/scripts/set-stage.sh Production` (commit `ffb1ffa`). Gate vào Production vẫn nên chạy lại sau 3c và 3.3 để ghi bằng chứng.
**Time**: 5 min
- [x] `project.stage` in `project.yaml` written

### 3.3 Epics từ issue đang mở
Chạy `/create-epics` theo module kiến trúc, mỗi epic dẫn tới issue GitHub thay vì sinh story file mới (ánh xạ một issue = một story, xem HUONG-DAN-STUDIO §3). Epic đầu tiên: **2.5D vertical slice** (#145, #146, #147; #148, #149 sau).
- [x] Epic `vuon-2-5d` và 5 story (#145–#149) đã tạo trong nhánh này (`production/epics/`)
- [ ] Epic cho issue mở khác khi chúng vào sprint

### 3.4 Milestone
Viết `production/milestones/m1-vertical-slice-2-5d.md` (bước 1–3 của `docs/de-xuat-2-5d.md` §6) và `m2-phat-hanh-mem-android.md` (giai đoạn 0–4 của `LO-TRINH-KIEM-TIEN.md`) bằng mẫu `.claude/docs/templates/milestone-definition.md`, dẫn chiếu thay vì chép.
- [ ] hai file milestone

### 3.5 QA dạng studio
Session QA chạy `/smoke-check` (lệnh `commands.smoke` = conventions + Core tests, không cần Unity) và `/qa-plan` cho sprint 1; báo cáo vào `production/qa/`.
- [ ] smoke report, qa plan

---

## Step 4: Medium-Priority Gaps

- [ ] **4.1 Chưa có `/design-review` cho 9 GDD.** GDD là reverse-documented; mỗi file có mục `INTENT UNKNOWN`. Chủ dự án hoặc session điều phối trả lời câu hỏi mở rồi chạy `/design-review design/gdd/<file>.md`, đổi Status trong `systems-index.md` thành `Approved`. Sau đó `/review-all-gdds`.
- [ ] **4.2 UX spec.** Session design chạy `/ux-design` cho HUD (`design/ux/hud.md`), màn vườn, Tiệm, Sổ thỏ, Nhiệm vụ, và `design/ux/interaction-patterns.md`; `/ux-review` từng file.
- [ ] **4.3 Accessibility.** `/ux-design accessibility` → `design/accessibility-requirements.md`, dẫn tới `AccessibilityTests*.cs` và tiêu chí #121/#142.
- [ ] **4.4 Balance.** `/balance-check` trên công thức trong `tram-va-thu-nhap.md` và `mo-rong-vuon-va-ban-do.md`, so với mô phỏng nhịp trong Core.
- [ ] **4.5 Playtest.** Gate Production → Polish cần ít nhất 1 playtest ghi lại; `/playtest-report` từ ghi chú QA hiện có.

## Step 5: Optional Improvements

- [ ] **5.1** `CLAUDE.md` ghi "701 tests"; Core tests nay là 963 (chạy 26/11). B sửa con số khi chạm `CLAUDE.md`.
- [ ] **5.2** Entity registry `design/registry/entities.yaml` còn trống: `/consistency-check` sau khi có 9 GDD.
- [ ] **5.4** `project.local.yaml` (cấu hình riêng từng người của studio) chưa có trong `.gitignore`: B thêm `/project.local.yaml`.
- [ ] **5.3** `.claude/docs/technical-preferences.md` (bản phụ của `project.yaml`) chưa điền; studio nói `project.yaml` thắng khi khác nhau, nên không bắt buộc.

---

## What to Expect from Existing Stories

Dự án không có story file; việc nằm trong GitHub issue. Skill studio đọc story ở `production/epics/**/story-*.md` sẽ thấy rỗng cho tới khi Bước 3.3 xong. Không sinh story cho issue đã đóng.

---

## Re-run

Chạy lại `/adopt` sau Bước 3 (khi helper ở 1.1 đã có thì gọi được qua Skill tool).
