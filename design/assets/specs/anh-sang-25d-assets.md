# Asset Specs — System: Ánh sáng và bóng (2.5D)

> **Source**: issue #147; [`docs/de-xuat-2-5d.md`](../../../docs/de-xuat-2-5d.md) §3, §5
> **Art Bible**: design/art/art-bible.md
> **Generated**: 2026-11-26
> **Status**: 5 assets specced / 0 approved / 0 in production / 0 done

art-director và technical-artist không được gọi (chế độ `solo`): spec viết từ art bible; ràng buộc kỹ thuật chưa được technical-artist kiểm. Đèn thật (directional trăng, point light đèn lồng) là phần code của #147, không phải hình; các spec dưới đây là hình đi kèm.

## ASSET-009 — Đèn lồng cột (billboard)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (billboard đồ) |
| Dimensions | cạnh dài 512, cao 1,4 lần thỏ |
| Format | PNG RGBA |
| Naming | `decor_lantern.png` trong `Art/Final` (thay bản `Art/Placeholder`), lớp giấy sáng tách riêng `decor_lantern_giay.png` |

**Visual Description:** cột gỗ cong nhẹ có cần treo, một đèn lồng giấy tròn màu kem có nan ngang; phần giấy là một lớp riêng để tô sáng mà không làm sáng cột. Giấy không được trắng xoá (spike: giấy cháy trắng): tô Lantern nhạt với nan Ink.

**Art Bible Anchors:** §2 đèn lồng là yếu tố mang không khí; §4 Lantern là điểm ấm duy nhất cạnh thỏ; §8 pivot chân, bóng không vẽ vào.

**Generation Prompt:** "a sheet of 2 items on flat #2E5E49, storybook watercolor, thin purple ink outlines #3A2A4D: (1) a slender gently curved wooden lantern post with a hook arm and a round cream paper lantern with horizontal ribs hanging from it; (2) the same paper lantern alone, same size, glowing soft warm amber (#FFC866) from inside, ribs still visible, not white. No ground shadow, no glow halo outside the lantern, no text."

**Status:** Needed

## ASSET-010 — Quầng sáng đèn (cộng màu)

| Field | Value |
|-------|-------|
| Category | VFX (sprite cộng màu, camera-facing) |
| Dimensions | 256×256 |
| Format | PNG RGBA, alpha mềm hướng tâm |
| Naming | `vfx_quang-den.png` |

**Visual Description:** vòng sáng mềm hướng tâm, tâm trắng ngà nhạt sang trong suốt ở mép, không vân; tint `Lantern` trong shader cộng màu. Kích thước hiển thị ~1,2 lần đèn lồng.

**Art Bible Anchors:** §2, §4 điểm ấm; §8 tối đa 2 lớp phủ toàn màn hình (quầng là cục bộ, không tính).

**Generation Prompt:** tạo bằng script (gradient hướng tâm), không cần vẽ.

**Status:** Needed

## ASSET-011 — Vũng sáng đèn trên cỏ

| Field | Value |
|-------|-------|
| Category | VFX (quad nằm phẳng trên đất) |
| Dimensions | 512×256 (elip) |
| Format | PNG RGBA, alpha mềm |
| Naming | `vfx_vung-sang-den.png` |

**Visual Description:** elip ánh sáng hổ phách nằm phẳng dưới chân đèn lồng, mép rất mềm, có vài vệt cỏ sáng hơn để không phẳng như vết sơn. Bù cho việc point light không đủ hắt sáng xuống cỏ (spike: đèn không hắt vũng trên đất).

**Art Bible Anchors:** §2 "vũng sáng đèn lồng trên cỏ"; §4 Lantern, cộng màu nhẹ (alpha ≤ 0,45).

**Generation Prompt:** tạo bằng script (elip gradient + nhiễu nhẹ theo ô cỏ #129); vẽ lại chỉ khi bản script trông phẳng.

**Status:** Needed

## ASSET-012 — Bóng elip dưới chân

| Field | Value |
|-------|-------|
| Category | Sprite (quad bóng nằm phẳng) |
| Dimensions | 256×96 |
| Format | PNG RGBA, alpha mềm |
| Naming | `shadow_elip.png` (thay `Art/Placeholder/shadow.png` mép cứng) |

**Visual Description:** elip mềm, màu Ink, alpha tâm 0,3, mép tan dần; dùng cho thỏ (lớp `Shadow` của rig) và đồ nhỏ khi bóng sprite theo trăng bị răng cưa. Chọn giữa bóng sprite và quad này bằng tấm so sánh của #147.

**Art Bible Anchors:** §4 bóng nhuộm về Night/Ink, không đen; §8 bóng là lớp riêng.

**Generation Prompt:** tạo bằng script.

**Status:** Needed

## ASSET-013 — Đèn đá (billboard)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (billboard đồ) |
| Dimensions | cạnh dài 512, cao 1,0–1,2 lần thỏ |
| Format | PNG RGBA |
| Naming | kiểm bản đèn đá hiện có ở `Art/Final` (spike 2.5D đã dùng); lớp cửa sổ sáng tách riêng nếu cần |

**Visual Description:** đèn đá rêu phong mũm mĩm có ô cửa sáng; ô cửa là lớp riêng tô Lantern như ASSET-009.

**Art Bible Anchors:** §2, §4 điểm ấm phụ; §6 kiến trúc vườn.

**Generation Prompt:** dùng bản có sẵn; nếu cần tách lớp sáng: "the same stone garden lantern, a second version with only the window glowing warm amber (#FFC866), everything else identical, flat #2E5E49, no halo, no shadow, no text."

**Status:** Needed (kiểm trước)
