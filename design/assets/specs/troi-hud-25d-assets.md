# Asset Specs — System: Trời, viền xa và HUD trên cảnh 2.5D

> **Source**: issue #148; [`docs/de-xuat-2-5d.md`](../../../docs/de-xuat-2-5d.md) §3, §5; dẫn chiếu #137 (viền rừng), #142 (phông), #143 (khung thẻ)
> **Art Bible**: design/art/art-bible.md
> **Generated**: 2026-11-26
> **Status**: 5 assets specced / 0 approved / 0 in production / 0 done

art-director và technical-artist không được gọi (chế độ `solo`): spec viết từ art bible; ràng buộc kỹ thuật chưa được technical-artist kiểm. Bố cục HUD (vị trí, kích thước, luồng) để `/ux-design` của #148, không nằm trong spec hình này.

## ASSET-014 — Bầu trời đêm (ba lớp)

| Field | Value |
|-------|-------|
| Category | Environment (nền) |
| Dimensions | lớp trời 1080×1920 (gradient, có thể tạo bằng script); lớp trăng 512×512; lớp sao 1024×1024 lặp liền |
| Format | PNG RGBA |
| Naming | `bg_troi.png`, `bg_trang.png`, `bg_sao.png` |

**Visual Description:** trời Night đậm dần lên trên, gần chân trời pha nhẹ tím; trăng tròn kem ngà mềm có quầng rất nhẹ ở góc trên phải (không cạnh tranh với đèn lồng); sao nhỏ thưa, vài sao 4 cánh. Lấp một phần ba trên khung mà spike để trống.

**Art Bible Anchors:** §2 vườn đêm yên; §4 Night, trăng Paper nhạt; §1 tối và yên là nền.

**Generation Prompt:** trời bằng script (gradient Night). Trăng và sao: "a sheet of 2 items on flat #2E5E49, storybook watercolor: (1) a soft round full moon in pale cream with gentle watercolor texture, no face, no halo beyond its edge; (2) a seamless tileable square of sparse tiny stars, a few four-point stars, on transparent-looking flat dark navy (#1B1E45). No text."

**Status:** Needed

## ASSET-015 — Dải rừng và đồi xa phía trên đảo

| Field | Value |
|-------|-------|
| Category | Environment (lớp nền xa) |
| Dimensions | 2048×512, lặp ngang liền mép |
| Format | PNG RGBA |
| Naming | `bg_rung-xa.png` |

**Visual Description:** hàng cây tròn và đồi thấp xa, chỉ hai bậc giá trị (Night pha Grass 30 % và 45 %), mép trên mềm, không chi tiết lá; đứng sau đảo để mép đảo không lơ lửng giữa trời.

**Art Bible Anchors:** §3 đất và rừng tròn mềm; §4 lạnh và tối hơn đảo; §6 viền rừng (#137).

**Generation Prompt:** "a wide seamless horizontally tileable strip of distant round treetops and low rolling hills, storybook watercolor, only two flat dark blue-green values, very soft top edge, no details, no light spots, on flat #2E5E49 above and below, 2048x512, no text."

**Status:** Needed

## ASSET-016 — Cây cụm viền đảo (billboard)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (billboard đồ) |
| Dimensions | cạnh dài 512, cao 2,5–3,5 lần thỏ |
| Format | PNG RGBA |
| Naming | `decor_tree.png`, `decor_bush.png` trong `Art/Final` (thay bản `Art/Placeholder`), thêm `_b` cho biến thể |

**Visual Description:** cây tán tròn ba cụm, thân nâu ngắn; bụi tròn thấp có vài hoa hồng; màu tán đậm hơn Grass một bậc để tách khỏi cỏ. Dùng chung với #137 và #146.

**Art Bible Anchors:** §3 khối tròn; §4 xanh đậm, hoa Blush; §8 pivot chân, không vẽ bóng.

**Generation Prompt:** "a sheet of 4 items, 2x2, storybook watercolor, thin purple ink outlines #3A2A4D, flat #2E5E49 background, same scale: (1) a round three-cluster tree with a short brown trunk; (2) a variant of the same tree; (3) a low round bush with a few small pink flowers; (4) a variant bush without flowers. No ground shadow, no text."

**Status:** Needed

## ASSET-017 — Vân giấy (pass Multiply)

| Field | Value |
|-------|-------|
| Category | VFX (texture toàn màn hình) |
| Dimensions | 512×512 lặp liền |
| Format | PNG xám |
| Naming | `fx_van-giay.png` |

**Visual Description:** vân giấy màu nước mịn, chỉ làm tối (Multiply), độ đậm 6–8 %; thay `PaperGrain` uGUI.

**Art Bible Anchors:** art bible gốc mục 6 (vân giấy chỉ làm tối); §8 tối đa 2 lớp phủ.

**Generation Prompt:** tạo bằng script từ nhiễu (như `PaperGrain.Make`), hoặc chụp vân giấy thật; không cần AI.

**Status:** Needed

## ASSET-018 — Dải tối sau HUD

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | 1080×260 (gradient dọc) |
| Format | PNG RGBA hoặc tạo bằng code |
| Naming | `ui_dai-hud.png` |

**Visual Description:** gradient Night từ alpha 0,35 ở mép trên xuống 0 ở đáy, để thẻ HUD kem đứng rõ trên trời và cây xa mà không phải tối cả màn. Chỉ bật khi camera thấy trời phía sau HUD.

**Art Bible Anchors:** §7 thông tin quan trọng nổi nhất; §8 tính là lớp phủ cục bộ, không phải toàn màn.

**Generation Prompt:** tạo bằng code (`UiKit` gradient).

**Status:** Needed
