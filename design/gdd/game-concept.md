# Game Concept: Vườn Trà Thỏ Trăng (Moon Rabbit Tea Garden)

*Created: 2026-11-26*
*Status: Approved*

> **Reverse-documented from implementation** — viết bằng `/reverse-document` từ `Assets/VuonTra/` và tài liệu trong `docs/` ngày 2026-11-26, tại commit `9663644`.
> Tài liệu này ghi game **đang là gì**. Ý định nào không có chữ của chủ dự án trong `docs/` được ghi `INTENT UNKNOWN`. Khi tài liệu và code khác nhau, đừng mặc định tài liệu là yêu cầu.
> Đây là "brief" của game ở mức rigor `standard` (studio gọi là `game-concept.md`; `design/game-brief.md` chỉ dùng ở mức `minimal`).

---

## Elevator Pitch

> Một game idle thư giãn trên điện thoại (màn dọc), nơi bạn trông một tiệm trà nhỏ trong rừng đêm do bầy thỏ làm việc: chạm để pha trà, mở trạm, gọi thỏ qua cổng trăng và mở dần khu vườn, kể cả lúc bạn đi vắng.

Nguồn: văn bản cửa hàng đã soạn ở [docs/phat-hanh/mo-ta-cua-hang.md](../../docs/phat-hanh/mo-ta-cua-hang.md) mục 1.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Idle / cozy management, sưu tầm nhân vật |
| **Platform** | Mobile (Android trước, iOS sau), màn dọc, chạm |
| **Target Audience** | Người chơi thích game nhẹ nhàng, dễ thương, phiên ngắn; thị trường vi và en (doanh thu kỳ vọng chủ yếu từ en theo [LO-TRINH-KIEM-TIEN.md](../../docs/phat-hanh/LO-TRINH-KIEM-TIEN.md) mục 6) |
| **Player Count** | Một người |
| **Session Length** | Vài phút, nhiều lần mỗi ngày (thể loại idle; số đo thật chưa có) |
| **Monetization** | F2P: cà rốt vàng mua bằng tiền thật, vé tháng/Vé trăng, quảng cáo có thưởng. Toàn bộ nằm sau cờ `Features.Monetization`, **mặc định tắt** (KaneNguyen03/moon-rabbit-tea-garden#144). Xem [co-kiem-tien.md](co-kiem-tien.md) |
| **Estimated Scope** | Medium; khoảng 145 issue đã xong tới 26/11/2026 |
| **Comparable Titles** | Học cảm giác thể loại từ Cats & Soup (không mô phỏng tên, hình, giao diện): [nghien-cuu-cats-and-soup-vong-3.md](../../docs/nghien-cuu-cats-and-soup-vong-3.md). Số liệu bán hàng: not researched |

---

## Core Fantasy

Bạn là người giữ một khu vườn trà dưới trăng; bầy thỏ tự làm việc, tự đi dạo, tự trò chuyện, và khu vườn cứ thế lớn lên dù bạn chỉ ghé vài phút. Cảm giác chính là **được chăm sóc một thế giới nhỏ ấm áp mà không bị đòi hỏi**. `[inferred — confirm]`

## Unique Hook

Giống các game idle chăm mèo/nấu ăn, **và thêm** một thế giới văn hoá trà và trăng Việt (Trung thu, mochi giã, đèn lồng giấy, Tết) với thỏ sưu tầm được qua cổng trăng và ghép bậc. `[inferred — confirm]`

---

## Visual Identity Anchor

**Một câu** (nguyên văn [design/art/art-bible.md](../art/art-bible.md) mục 1, nguồn duy nhất): một trang sách tranh màu nước dưới trăng, đứng trên mặt đất có chiều sâu. Nền đêm sâu và yên, đèn lồng ấm là nơi mắt dừng, mọi thứ đáng chạm đều mũm mĩm, sắc nét và hơi nảy khi được chạm.

Art bible dạng studio (9 mục, session design): [design/art/art-bible.md](../art/art-bible.md). Nguyên tắc hỗ trợ (chi tiết và mã màu ở [docs/art-bible.md](../../docs/art-bible.md), hướng thẩm mỹ đầy đủ ở [docs/nghien-cuu-thiet-ke-do-hoa.md](../../docs/nghien-cuu-thiet-ke-do-hoa.md) mục 4):
1. Mọi màu lấy từ bảng màu trong `Palette.cs`; màu cà rốt `#FF9B4A` chỉ dành cho thứ bấm được và tiền tệ.
2. Ảnh trong, không đục: tối đa 2 lớp phủ toàn màn hình; vùng chưa mở là cỏ chìm trong bóng đêm, không sương.
3. Từ 25–26/11/2026 cảnh vườn chuyển sang 2.5D: đất 3D, camera nghiêng cố định, hình 2D hiện có đứng thẳng làm billboard ([docs/de-xuat-2-5d.md](../../docs/de-xuat-2-5d.md), ADR [adr-0005](../../docs/architecture/adr-0005-huong-2-5d.md)).

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Submission** (thư giãn) | 1 | Vòng idle, thu nhập khi đi vắng, không thua |
| **Sensation** | 2 | Hình màu nước, âm thanh theo trạm, hiệu ứng chạm (DOTween, Shuriken) |
| **Fantasy** | 3 | Tiệm trà của thỏ dưới trăng |
| **Discovery** | 4 | Hơn 70 loài thỏ, sổ sưu tầm, sự kiện mùa |
| **Expression** | 5 | Bố trí vườn, mũ, đặt tên thỏ |
| **Challenge** | 6 | Tối ưu nâng cấp, trò nhỏ |
| **Narrative** | N/A | Chỉ có lời thỏ trò chuyện |
| **Fellowship** | N/A | Chưa có tính năng xã hội |

Thứ tự trên là suy ra từ hệ thống đã có, `INTENT UNKNOWN`.

### Core Mechanics (Systems we build)

1. Trạm sản xuất và thu nhập, nâng cấp — [tram-va-thu-nhap.md](tram-va-thu-nhap.md)
2. Thỏ, gọi qua cổng trăng, ghép bậc, sổ sưu tầm — [tho-va-so-suu-tam.md](tho-va-so-suu-tam.md)
3. Mở rộng vườn và bản đồ — [mo-rong-vuon-va-ban-do.md](mo-rong-vuon-va-ban-do.md)
4. Nhiệm vụ ngày/tuần/tháng — [nhiem-vu-ngay-tuan-thang.md](nhiem-vu-ngay-tuan-thang.md)
5. Tiệm và cà rốt vàng — [tiem-va-ca-rot-vang.md](tiem-va-ca-rot-vang.md)
6. Trò nhỏ; mưa và lễ hội; đi vắng; cờ kiếm tiền — [tro-choi-nho.md](tro-choi-nho.md), [mua-va-le-hoi.md](mua-va-le-hoi.md), [di-vang.md](di-vang.md), [co-kiem-tien.md](co-kiem-tien.md)

Danh sách đầy đủ và trạng thái: [systems-index.md](systems-index.md).

---

## Core Loop

### Moment-to-Moment (30 seconds)
Chạm trạm để thu hàng, xem thỏ làm việc, chạm thỏ/đồ để có phản hồi.

### Short-Term (5-15 minutes)
Tiêu cà rốt nâng cấp trạm, gọi thỏ mới, làm nhiệm vụ ngày, chơi một trò nhỏ.

### Session-Level (30-120 minutes)
Không phải mục tiêu của thể loại; một ngày gồm nhiều phiên ngắn. `[inferred — confirm]`

### Long-Term Progression
Cấp vườn và các đợt mở đất, điền sổ thỏ, vé tháng, sự kiện mùa.

### Retention Hooks
Thu nhập khi đi vắng và màn chào lại, nhiệm vụ ngày/tuần/tháng, lịch quà, thông báo nhắc (KaneNguyen03/moon-rabbit-tea-garden#65), mùa lễ.

---

## Game Pillars

Bốn trụ lấy nguyên thứ tự từ skill `game-designer-thinking` (`.claude/skills/game-designer-thinking/SKILL.md` dòng 12) và đã dùng trong [design/art/art-bible.md](../art/art-bible.md) với nhãn P1–P4. GDD ghi `Implements Pillar` theo các nhãn này.

### Pillar 1 (P1): Yên — calm
Không gì làm chói mắt hay giục giã; không thua, không phạt khi đi vắng, không khẩn cấp giả. Màn hình yên cho tới khi người chơi chạm.

### Pillar 2 (P2): Của mình — ownership
Vườn và thỏ là của người chơi: bố trí, đặt tên, đội mũ; thỏ là điểm nhìn sáng nhất của cảnh (art bible, chuẩn 2.5D "ảnh xám thu 25 %").

### Pillar 3 (P3): Thấy tiến bộ — visible progress
Mỗi lần ghé thấy điều mới; nhịp không gãy ở ngày 2 và tuần 2–6 (KaneNguyen03/moon-rabbit-tea-garden#98, #99); số liệu nhịp được mô phỏng trong Core trước khi làm giao diện.

### Pillar 4 (P4): Sưu tầm nhẹ — gentle collection
Thỏ, món, đồ trang trí; sưu tầm không ép, không paywall trên sự yên.

### Anti-Pillars (What This Game Is NOT)
- Không mô phỏng ngoại hình, tên hay giao diện của Cats & Soup ([de-xuat-2-5d.md](../../docs/de-xuat-2-5d.md)).
- Không 3D toàn phần, không AI 3D trả phí ([ADR-0005](../../docs/architecture/adr-0005-huong-2-5d.md)).
- Không ép mua: kiếm tiền tắt mặc định, quyền lợi đã trả không bị thu hồi ([LO-TRINH-KIEM-TIEN.md](../../docs/phat-hanh/LO-TRINH-KIEM-TIEN.md) mục "Cờ bán hàng").

---

## Inspiration and References

- [docs/nghien-cuu-cats-and-soup-vong-3.md](../../docs/nghien-cuu-cats-and-soup-vong-3.md) — học cảm giác thể loại.
- [docs/nghien-cuu-thiet-ke-do-hoa.md](../../docs/nghien-cuu-thiet-ke-do-hoa.md) — hướng thẩm mỹ.
- [docs/kiem-dinh-do-hoa.md](../../docs/kiem-dinh-do-hoa.md) — kiểm định đồ hoạ.

## Technical Considerations

Unity 6000.6.10f1, URP 17.6, C#. Luật chơi trong assembly Core thuần C# ([ADR-0001](../../docs/architecture/adr-0001-core-thuan-csharp.md)), dữ liệu bằng ScriptableObject ([ADR-0002](../../docs/architecture/adr-0002-scriptableobject-data.md)), SDK cửa hàng/quảng cáo/analytics sau seam ([ADR-0003](../../docs/architecture/adr-0003-seam-storefront-ads-analytics.md)). Quy ước máy kiểm: `tools/conventions.py`; kiểm toàn bộ: `tools/verify.sh`; test Core không cần Unity: `tools/ci/core-tests.sh`.

## Risks and Open Questions

### Design Risks
- Nhịp tiến độ gãy ở ngày 2 và tuần 2–6 (#98, #99).

### Technical Risks
- Chuyển cảnh vườn sang 2.5D (renderer, camera, chạm) trên máy yếu: 60 fps chưa đo (#145–#149).
- GC khi vườn đầy (#97).

### Market Risks
- Chưa có số liệu giữ chân thật; ngưỡng tham khảo ngày 1 ≥ 35–40 %, ngày 7 ≥ 15 %, ngày 30 ≥ 5 % ([LO-TRINH-KIEM-TIEN.md](../../docs/phat-hanh/LO-TRINH-KIEM-TIEN.md) giai đoạn 4).

### Scope Risks
- Tính năng lớn (#91 bản đồ, #52/#53, #11, #10, #63, #39/#40) tạm gác tới khi có số liệu.

### Open Questions
- Thứ tự aesthetics và người chơi mục tiêu: INTENT UNKNOWN, cần chủ dự án xác nhận.

## MVP Definition

Bản phát hành mềm Android: vòng trạm, thỏ, mở vườn, nhiệm vụ, đi vắng, tiếng Anh, cảnh vườn 2.5D bước 1–2, cờ kiếm tiền tắt hoặc bật tuỳ chủ dự án. Danh sách phát hành: [docs/phat-hanh/CHECKLIST.md](../../docs/phat-hanh/CHECKLIST.md).

## Next Steps

Xem [docs/studio/HUONG-DAN-STUDIO.md](../../docs/studio/HUONG-DAN-STUDIO.md) và [docs/studio/adoption-plan-2026-11-26.md](../../docs/studio/adoption-plan-2026-11-26.md).
