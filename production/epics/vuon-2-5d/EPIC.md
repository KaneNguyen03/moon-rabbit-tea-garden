# Epic: Vườn 2.5D

> **Layer**: Presentation
> **GDD**: design/gdd/mo-rong-vuon-va-ban-do.md
> **Architecture Module**: Game (thế giới vườn, camera, billboard, ánh sáng); Core giữ nguyên
> **Status**: Ready
> **Stories**: 5 — xem bảng Stories

## Overview

Chuyển cảnh vườn từ uGUI `RectTransform` sang scene 2.5D: đảo cỏ 3D có mép đất, camera phối cảnh nghiêng cố định, mọi hình 2D màu nước hiện có đứng thẳng làm billboard, ánh trăng và đèn lồng thật, hậu kỳ vân giấy, rồi khoá hiệu năng mobile. Luật vườn (ô đất, mở đất, đặt trạm) vẫn ở Core và không đổi. Hướng do chủ dự án duyệt 26/11/2026; chuẩn và lộ trình năm bước ở [docs/de-xuat-2-5d.md](../../../docs/de-xuat-2-5d.md) §3–§6, chuẩn hình ở [design/art/art-bible.md](../../../design/art/art-bible.md).

Đây là mốc **M1 — Vertical slice 2.5D** (story 145–147) và phần mở đầu của M2 (148–149). Một story = một issue GitHub (xem [docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3).

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|------------------|-------------|
| ADR-0005: Hướng 2.5D | Đất 3D + camera nghiêng cố định + hình 2D làm billboard; không 3D toàn phần, không AI 3D trả phí | MEDIUM (Forward+ và bóng sprite trên Unity 6000.6 mobile chưa tra nguồn) |
| ADR-0001: Core thuần C# | Luật vườn ở Core, cảnh 2.5D chỉ đọc Core | LOW |
| ADR-0002: ScriptableObject data | Prefab billboard lấy sprite từ dữ liệu trạm/đồ đã có | LOW |

## GDD Requirements

`docs/architecture/tr-registry.yaml` chưa có (kế hoạch di trú Bước 3a), nên chưa có TR-ID. Yêu cầu tạm lấy từ GDD và issue:

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| (chưa cấp) | Kéo, zoom, chạm trạm, mở bản đồ hoạt động như bản 2D trên cảnh 2.5D | ADR-0005 ✅ |
| (chưa cấp) | Trạm, đồ, thỏ đặt theo toạ độ đất từ `GardenGrid`, không theo màn hình | ADR-0001 ✅, ADR-0005 ✅ |
| (chưa cấp) | Vùng chưa mở không có sương (art bible) | ADR-0005 ✅ |
| (chưa cấp) | 60 fps (sàn 45) trên iPhone 11 và Android Snapdragon 665 | ADR-0005 ✅ |

## Stories

| Story | Issue | Title | Type | Status |
|-------|-------|-------|------|--------|
| [story-145](story-145-dat-va-camera.md) | #145 | Đất và camera | Integration | Ready |
| [story-146](story-146-dung-dung-billboard.md) | #146 | Trạm, đồ, thỏ dựng đứng; bảng tỉ lệ; ba trạm mẫu | Visual/Feel | Ready |
| [story-147](story-147-anh-sang-va-bong.md) | #147 | Ánh sáng và bóng | Visual/Feel | Ready |
| [story-148](story-148-hau-ky-va-hud.md) | #148 | Hậu kỳ, trời phía trên đảo, HUD trên cảnh | UI | Ready |
| [story-149](story-149-hieu-nang-mobile.md) | #149 | Hiệu năng mobile 60 fps | Integration | Ready |

## Definition of Done

- Mọi story xong qua `/story-done`, issue gắn `chờ-PO-duyệt`, QA D đóng sau khi PO duyệt
- Kéo, zoom, chạm, bản đồ trên cảnh 2.5D có test EditMode xanh; `tools/verify.sh` xanh; `python3 -I tools/conventions.py` không tăng baseline
- Visual/Feel và UI: ảnh trước/sau ở `docs/anh/po-<số issue>/` (thư mục bằng chứng của dự án, thay cho `production/qa/evidence/`) và `visual-critic-gate` ra `APPROVED`
- Ít nhất một playtest ghi ở `production/qa/playtests/` sau story-147 (điều kiện M1)
