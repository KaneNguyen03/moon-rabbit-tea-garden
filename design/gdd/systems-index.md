# Systems Index: Vườn Trà Thỏ Trăng

> **Status**: Under Review
> **Created**: 2026-11-26
> **Last Updated**: 2026-11-26
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

Game idle/cozy màn dọc: vòng 30 giây là chạm trạm, thu cà rốt, nâng cấp; vòng mỗi lần ghé là quay lại, nhận phần đi vắng, làm nhiệm vụ, gọi thỏ; vòng dài là mở rộng vườn, điền sổ thỏ, mùa lễ và trăng mới. Mọi hệ thống dưới đây **đã chạy trong code** trước khi dùng studio; GDD của chúng được viết ngược từ code ngày 26/11/2026 (`/reverse-document`), nên code đã có nhưng Status tài liệu là `In Review` (GDD) hoặc `Not Started` (hệ thống chưa có GDD riêng) cho tới khi `/design-review` duyệt; nội dung còn câu `INTENT UNKNOWN` chờ chủ dự án. Bốn trụ P1–P4 ở [game-concept.md](game-concept.md).

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Trạm và thu nhập | Economy | MVP | In Review | design/gdd/tram-va-thu-nhap.md | Save, Thỏ |
| 2 | Thỏ và sổ sưu tầm | Progression | MVP | In Review | design/gdd/tho-va-so-suu-tam.md | Trạm và thu nhập, Mở rộng vườn |
| 3 | Mở rộng vườn và bản đồ | Progression | MVP | In Review | design/gdd/mo-rong-vuon-va-ban-do.md | Trạm và thu nhập, Thỏ |
| 4 | Đi vắng | Economy | MVP | In Review | design/gdd/di-vang.md | Trạm và thu nhập |
| 5 | Cờ kiếm tiền | Meta | MVP | In Review | design/gdd/co-kiem-tien.md | Storefront/Ads/Analytics seams (ADR-0003) |
| 6 | Nhiệm vụ ngày/tuần/tháng | Progression | Vertical Slice | In Review | design/gdd/nhiem-vu-ngay-tuan-thang.md | Trạm và thu nhập, Tiệm |
| 7 | Tiệm và cà rốt vàng | Economy | Vertical Slice | In Review | design/gdd/tiem-va-ca-rot-vang.md | Trạm và thu nhập, Thỏ, Cờ kiếm tiền |
| 8 | Trò nhỏ | Gameplay | Alpha | In Review | design/gdd/tro-choi-nho.md | Trạm và thu nhập, Mở rộng vườn |
| 9 | Mưa và lễ hội | Gameplay | Alpha | In Review | design/gdd/mua-va-le-hoi.md | Trạm và thu nhập, Thỏ, Tiệm, Nhiệm vụ |
| 10 | Save và mã hoá save (inferred) | Persistence | MVP | Not Started | — (code `Core/SaveCodec.cs`, `Game/SaveStore.cs`; [docs/save-va-test.md](../../docs/save-va-test.md)) | — |
| 11 | Hướng dẫn và người mới (inferred) | Meta | Vertical Slice | Not Started | — (một phần trong nhiem-vu-ngay-tuan-thang.md: `Newcomer`) | Nhiệm vụ |
| 12 | Cảnh vườn 2.5D | UI | Vertical Slice | In Progress | — (chuẩn ở [docs/de-xuat-2-5d.md](../../docs/de-xuat-2-5d.md), ADR-0005; epic `production/epics/vuon-2-5d`) | Mở rộng vườn và bản đồ |
| 13 | Âm thanh (inferred) | Audio | Alpha | Not Started | — (`Game/Audio`) | — |

---

## Dependency Map

### Foundation Layer (no dependencies)
1. Save và mã hoá save — mọi hệ thống ghi trạng thái qua đây.
2. Cờ kiếm tiền — quyết định có tạo Storefront/Ads hay không, trước mọi màn bán hàng.

### Core Layer (depends on foundation)
1. Trạm và thu nhập — depends on: Save
2. Thỏ và sổ sưu tầm — depends on: Trạm và thu nhập
3. Mở rộng vườn và bản đồ — depends on: Trạm và thu nhập, Thỏ

### Feature Layer (depends on core)
1. Đi vắng — depends on: Trạm và thu nhập
2. Nhiệm vụ ngày/tuần/tháng — depends on: Trạm và thu nhập
3. Tiệm và cà rốt vàng — depends on: Thỏ, Cờ kiếm tiền
4. Trò nhỏ — depends on: Trạm và thu nhập
5. Mưa và lễ hội — depends on: Thỏ, Tiệm, Nhiệm vụ

### Presentation Layer (depends on features)
1. Cảnh vườn 2.5D — depends on: Mở rộng vườn và bản đồ
2. Âm thanh

---

## Circular Dependencies

- Thỏ ↔ Mở rộng vườn: số thỏ và loài đã gặp là điều kiện mở đất, còn chỗ nghỉ và chỗ làm của thỏ đến từ đất đã mở. Đã giải trong code bằng cách Core đọc cả hai từ `GameState` mỗi lần tính; không cần đổi.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Cảnh vườn 2.5D | Technical | Forward+ và bóng sprite trên máy yếu (iPhone 11, Snapdragon 665) | story-149 đo từ story-145; `/setup-engine refresh` |
| Trạm và thu nhập | Design | Nhịp gãy ngày 2 và tuần 2–6 (#98, #99) | `/balance-check`, mô phỏng nhịp trong Core |
| Cờ kiếm tiền | Scope | Chưa có SDK quảng cáo thật; tài liệu nói AdMob, báo cáo #66 nói LevelPlay | chủ dự án chọn SDK (LO-TRINH-KIEM-TIEN "Chủ project cần quyết ngay") |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 13 |
| Design docs started | 9 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
