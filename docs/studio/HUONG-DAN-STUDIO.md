# Hướng dẫn dùng Claude Code Game Studios cho Vườn Trà Thỏ Trăng

*Viết 26/11/2026 khi đưa game vào quy trình studio (nhánh `studio-adopt`). Áp dụng cho mọi session từ khi nhánh này được gộp.*

Studio (Donchitos/Claude-Code-Game-Studios 1.3.0) đã cài ở `.claude/skills`, `.claude/agents`, `.claude/scripts`, `.claude/docs`; helper đọc cấu hình ở `.claude/hooks/yaml-helper.sh`. **Không** cài `CLAUDE.md`, `settings.json`, hook hay rules của studio: dự án giữ luật riêng (mục 5).

## 1. Một nguồn cho mỗi sự thật

| Câu hỏi | Nguồn duy nhất |
|---|---|
| Việc gì đang mở, ai làm, PO duyệt chưa | **GitHub issue** của `KaneNguyen03/moon-rabbit-tea-garden` (nhãn giữ nguyên) |
| Một việc phải đạt gì (acceptance criteria), theo GDD/ADR nào | **story file** `production/epics/<epic>/story-<số issue>-<slug>.md` |
| Hệ thống chạy theo luật gì | `design/gdd/*.md` (dẫn chiếu `docs/` thay vì chép) |
| Quyết định kỹ thuật | `docs/architecture/adr-*.md` |
| Hình ảnh | `design/art/art-bible.md` → `docs/art-bible.md` |
| Lộ trình tiền | `docs/phat-hanh/LO-TRINH-KIEM-TIEN.md` |
| Nhật ký bàn giao cho PO | `docs/bao-cao-po.md` (giữ như cũ) |
| Cấu hình studio | `project.yaml` (rigor `standard`, automation `guided`, engine Unity 6000.6.10f1) |

Bản đồ đầy đủ: [design/gdd/systems-index.md](../../design/gdd/systems-index.md). Kế hoạch di trú còn mở: [adoption-plan-2026-11-26.md](adoption-plan-2026-11-26.md).

## 2. Mỗi session làm gì

Gọi skill bằng `/tên-skill` (hoặc Skill tool). Mọi skill đọc `project.yaml`; ở `automation: guided` skill chỉ hỏi ở quyết định lớn và trước khi tạo file mới.

### B — code (giữ checkout main, gộp nhánh, chạy Unity)

| Khi | Skill | Ra gì |
|---|---|---|
| Issue vào sprint mà chưa có story file | `/create-stories <epic>` | `story-<issue>-<slug>.md`, AC lấy từ GDD + thân issue |
| Trước khi code | `/story-readiness <story>` | READY / BLOCKED |
| Làm | `/dev-story <story>` | code + test theo ADR; Status → In Progress |
| Xong | `/story-done <story>` | kiểm từng AC; Status → Complete; B gắn `chờ-PO-duyệt` và comment issue |
| Trước khi gộp | `/code-review <đường dẫn>` | nhận xét kiến trúc; luật máy vẫn là `tools/conventions.py` |
| Đổi kỹ thuật lớn | `/architecture-decision` | ADR mới, `Status: Proposed` tới khi chủ dự án duyệt |
| Đổi một con số trong GDD | `/propagate-design-change` | danh sách ADR/story bị ảnh hưởng |

Kiểm trước commit vẫn là `tools/verify.sh --fast` (Unity) hoặc ít nhất `python3 -I tools/conventions.py && tools/ci/core-tests.sh` (không Unity).

### Design — UX/UI, mỹ thuật, hoạt ảnh (worktree riêng)

| Khi | Skill | Ra gì |
|---|---|---|
| Sửa hướng hình | `/art-bible` | `design/art/art-bible.md` |
| Cần hình mới | `/asset-spec <hệ thống>` | `design/assets/…`; vẽ theo `cozy-watercolor-art`, không SpriteCook |
| Màn hình, HUD, luồng | `/ux-design <màn>` | `design/ux/<màn>.md`, `design/ux/hud.md`, `design/ux/interaction-patterns.md` |
| Duyệt UX | `/ux-review <file>` | APPROVED / NEEDS REVISION |
| Một màn từ đầu tới cuối | `/team-ui <màn>` | điều phối ux → hình → B code → review |
| Đóng việc hình | `visual-critic-gate` (skill dự án) | chỉ APPROVED mới xong (CLAUDE.md luật 5) |

### QA (QA A–D, D đóng issue)

| Khi | Skill | Ra gì |
|---|---|---|
| Đầu sprint | `/qa-plan` | `production/qa/qa-plan-sprint-NN.md` |
| Trước khi giao QA | `/smoke-check` | chạy `commands.smoke` (conventions + Core tests, không Unity); báo cáo vào `production/qa/` |
| Nghiệm thu story | `/test-evidence-review <story>` | ADEQUATE / INCOMPLETE / MISSING; D đóng issue khi ADEQUATE và PO đã duyệt |
| Cuối sprint | `/regression-suite` | lỗi đã sửa mà thiếu test hồi quy |
| Chơi thử | `/playtest-report` | `production/qa/playtests/*.md` (gate Production → Polish cần ít nhất 1) |
| Lỗi mới | `/bug-report` | thân issue nhãn `bug` + `QA`; không tạo file lỗi riêng |

### Điều phối

| Khi | Skill | Ra gì |
|---|---|---|
| Đầu tuần (sprint 1 tuần) | `/sprint-plan` | `production/sprints/sprint-NN.md`, `production/sprint-status.yaml` từ issue mở |
| Giữa tuần | `/sprint-status` | burndown, rủi ro |
| Cuối mốc | `/milestone-review`, `/gate-check <giai đoạn>` | `production/gate-checks/*.md`; chỉ gate-check ghi `project.stage` |
| Không biết làm gì tiếp | `/next-step` | một bước kế tiếp |
| Scope phình | `/scope-check`, `/cut` | đề xuất cắt |

## 3. Ánh xạ GitHub issue ↔ story

**Một issue = một story.** Không có story nào không có issue, và không mở issue mới chỉ để có story.

- **Tên file:** `production/epics/<epic-slug>/story-<số issue>-<slug>.md` (ví dụ `production/epics/vuon-2-5d/story-145-dat-va-camera.md`). Số story của studio là số issue, nên `NNN` không cần đánh lại.
- **Dòng đầu phần header:** `> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#145`. Thân issue thêm một dòng `Spec: production/epics/…/story-145-….md` khi story file được tạo.
- **Acceptance criteria:** sau khi story file có, AC sửa trong story file; issue chỉ giữ thảo luận, ảnh và nghiệm thu.
- **Chỉ tạo story file khi issue vào sprint.** Không sinh lại story cho ~145 issue đã đóng.
- **Epic = nhóm issue theo module kiến trúc** (`/create-epics`). Epic đầu tiên: `vuon-2-5d` (#145–#149). Có thể gắn GitHub milestone cùng tên với `production/milestones/*.md`; không thêm nhãn mới.
- **Nhãn giữ nguyên**, trạng thái đối chiếu như sau:

| GitHub | Story `Status` | Ai đổi |
|---|---|---|
| mở, chưa vào sprint | (chưa có file) | — |
| mở, trong sprint | `Ready` | `/create-stories` |
| `cần-chủ-project` hoặc `question` | `Blocked — BLOCKED: cần chủ project` | người gắn nhãn |
| mở, đang làm | `In Progress` | `/dev-story` |
| `chờ-PO-duyệt` | `Complete` | `/story-done` + B gắn nhãn |
| `PO-trả-lại` | `In Progress` | B, khi nhận lại |
| đóng (QA D) | `Complete` (giữ) | D đóng issue, không sửa file |
| `bug`, `QA` | story `Type: Logic` hoặc `Visual/Feel` như việc thường | — |

- **Bằng chứng hình:** ảnh trước/sau giữ ở `docs/anh/po-<số issue>/` như trước (thay cho `production/qa/evidence/` của studio); story ghi đường dẫn đó ở mục Test Evidence.

`production/sprint-status.yaml` do `/sprint-plan` và `/story-done` ghi; không sửa tay. Epic đầu tiên đã có: [production/epics/vuon-2-5d/EPIC.md](../../production/epics/vuon-2-5d/EPIC.md).

## 4. Giai đoạn và mốc

`project.stage: Production` (đặt 26/11 theo yêu cầu điều phối, khớp heuristic: 175 file code). Mốc kế tiếp:

1. **M1 — Vertical slice 2.5D:** bước 1–3 của `docs/de-xuat-2-5d.md` §6 (#145, #146, #147) chơi được trên máy thật, có một playtest ghi lại.
2. **M2 — Phát hành mềm Android:** giai đoạn 0–4 của `LO-TRINH-KIEM-TIEN.md`, `docs/phat-hanh/CHECKLIST.md`.

Chi tiết thiếu gì để qua mốc: [production/gate-checks/2026-11-26-production-to-polish.md](../../production/gate-checks/2026-11-26-production-to-polish.md).

## 5. Chỗ studio khác luật dự án — luật dự án thắng

| Studio | Dự án | Quy ước từ giờ |
|---|---|---|
| Skill viết code (`/dev-story`, `/test-helpers`, `/test-setup`) và mẫu C# của studio có `///` XML doc và comment | `CLAUDE.md`: **không comment** trong file nào; `tools/conventions.py` chặn | Sau mỗi `/dev-story` chạy `python3 -I tools/conventions.py`; control manifest (`/create-control-manifest`) phải ghi "Forbidden: comment" như luật đầu. Skill studio sinh comment thì xoá, không hạ baseline bằng tay |
| Mẫu YAML/Markdown studio có `#` comment (`sprint-status.yaml`, `project.yaml`) | Luật không comment nói "mọi file bạn viết" | Comment có sẵn trong mẫu studio để nguyên (CLAUDE.md: "leave existing comments"); khi tự viết khối mới thì không thêm comment. `conventions.py` chỉ quét `.cs` |
| `commands.test`/`smoke` mặc định gọi Unity batch | Chỉ B chạy Unity, tối đa 3 batch cùng lúc; `verify.sh` đòi đóng Editor | `commands.test` và `commands.smoke` dùng Core tests (dotnet, không Unity). `commands.build`/`run` gọi `tools/verify.sh` và chỉ B chạy |
| Agent `unity-specialist` có thể đề nghị mở Editor, chạy PlayMode | Như trên; dự án trong `.claude/worktrees` thì Unity không build được | Session khác B không chạy Unity; cần chụp GameView thì nhờ B |
| Studio tìm CI ở `.github/workflows/tests.yml` | CI là `checks.yml` (conventions + Core tests) | Giữ `checks.yml`; gate ghi "test framework: có, khác tên file" |
| Studio hooks (`validate-commit.sh`, session-start, statusline) | Hook Claude + `.githooks/pre-commit` của dự án | Không cài hook studio; chỉ `yaml-helper.sh` |
| Commit message studio tự do | Một câu tiếng Anh nói điều thay đổi + dòng Co-Authored-By; commit theo đường dẫn | Giữ quy ước dự án |
| Asset skill gợi ý công cụ AI tạo hình | Không SpriteCook; hình theo `cozy-watercolor-art`, cắt bằng `img2game2d`, đóng bằng `visual-critic-gate` | Skill dự án quyết; `/asset-spec` chỉ viết spec |
| Studio có agent chuyên Godot và Unreal (5 `godot-*`, 4 `ue-*`, `unreal-specialist`) | Game là Unity | Đã xoá 10 agent đó khỏi `.claude/agents` (còn 39); khi nâng cấp studio không chép lại. Skill studio vẫn nhắc tên chúng ở nhánh "nếu engine là Godot/Unreal"; `project.yaml` ghi Unity nên nhánh đó không chạy |
| `/prototype` dựng game trình duyệt | Game là Unity, đã có | Không dùng `/prototype`; spike thì làm nhánh Unity như `spike-3d` |
| Tài liệu studio tiếng Anh | Tài liệu dự án tiếng Việt | Viết tiếng Việt; giữ nguyên tiêu đề mục tiếng Anh của mẫu (`## Overview`, `## Status`…) vì script studio tìm theo tên đó |
| `review_mode: lean` (từ rigor `standard`) gọi agent giám đốc ở gate | Chi phí token | Ai thấy tốn có thể đặt `--local modes.review_mode=solo` trong `project.local.yaml`; file này là cấu hình riêng từng máy và `.gitignore` đã bỏ qua nó (`git check-ignore -v project.local.yaml`) |
| `CLAUDE.md` ghi "701 tests" | Core tests nay 963 (26/11) | B sửa con số khi chạm `CLAUDE.md` |

## 6. Mặc định đã chọn khi không hỏi được ai

- Rigor `standard`, automation `guided`, sprint 1 tuần, mốc 4 tuần, mục tiêu `mobile`, ngôn ngữ `vi`, `en`, màn dọc.
- 9 GDD và 5 ADR là **reverse-documented**: ghi điều code đang làm. Câu `INTENT UNKNOWN` chờ chủ dự án; sau khi trả lời mới `/design-review` và đổi sang `Approved`.
- ADR để ở `docs/architecture/` (nơi studio đọc), các tài liệu chuyển đổi khác ở `docs/studio/`.
- `project.stage: Production` đặt bằng `.claude/scripts/set-stage.sh` theo yêu cầu điều phối (sửa auto-detect, không qua gate); từ giờ chỉ `/gate-check` đổi nó.
