# Asset Specs — System: Ba trạm mẫu và thỏ billboard (2.5D)

> **Source**: issue #146; vòng làm việc ở [`docs/kiem-dinh-do-hoa.md`](../../../docs/kiem-dinh-do-hoa.md) §4a; [`docs/de-xuat-2-5d.md`](../../../docs/de-xuat-2-5d.md) §3–4
> **Art Bible**: design/art/art-bible.md
> **Generated**: 2026-11-26
> **Status**: 8 assets specced / 0 approved / 0 in production / 1 done

art-director và technical-artist không được gọi (chế độ `solo`): spec viết từ art bible trong session design; ràng buộc kỹ thuật chưa được technical-artist kiểm.

Bảng nhịp (thỏ làm gì, dụng cụ, món trung gian, hạt, món ra) của trà sữa, hái chè, pha trà **không chép lại ở đây**: xem `docs/kiem-dinh-do-hoa.md` §4a. Các spec dưới đây là hình cần có để chạy đúng bảng đó trong 2.5D.

Tỉ lệ theo chiều cao thỏ (thỏ = 1, art bible §5): trạm 0,8–1,3; quầy/tiệm 1,8–2,2; đèn cột 1,4; cối 0,7. Thỏ ~110 px cao ở zoom mặc định và phải là điểm sáng nhất.

Lời nhắc vẽ dùng Antigravity theo [`docs/anh/VE-BANG-ANTIGRAVITY.md`](../../../docs/anh/VE-BANG-ANTIGRAVITY.md): tấm nhiều hình trên nền phẳng `#2E5E49`, tham chiếu phong cách `docs/anh/po-do-vat/_tong-hop.png`, rồi cắt bằng `cat-luoi-cung-ti-le.py` (giữ tỉ lệ giữa các trạng thái).

## ASSET-001 — Thỏ Cream billboard (rig 2D)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (PSB rig) |
| Dimensions | theo rig PSB, ~110 px cao ở zoom mặc định |
| Format | PSB + SpriteLibrary + Animator |
| Naming | `Assets/VuonTra/Art/Rabbit25D/Cream/RabbitCream.prefab`, `Cream_*.anim` |

**Visual Description:** thỏ kem mũm mĩm, viền mực tím mảnh; góc trước và góc 3/4 (lật gương cho bên phải); pivot ở chân; bóng là lớp con `Shadow` riêng. Clip Idle và ba vòng làm việc bốn nhịp, 12 dụng cụ cầm tay đổi bằng SpriteResolver.

**Art Bible Anchors:**
- §3: khối tròn, tai dài là nét nhận biết.
- §5: góc trước và 3/4, pivot chân, chi tiết dưới 2 px bỏ.

**Generation Prompt:** không cần (đã xong).

**Status:** Done (main `47e6642`). Bốn giống còn lại (Brown Lop, Spotted, Starry, Thỏ Bông) cần vẽ góc trước trước khi dùng khuôn này.

## ASSET-002 — Quầy trà sữa trăng: trạng thái quầy

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (billboard trạm) |
| Dimensions | cạnh dài 512, chung một tỉ lệ cho mọi trạng thái; quầy 1,8–2,2 lần chiều cao thỏ |
| Format | PNG RGBA, cạnh bội số 4, nền trong suốt sạch |
| Naming | `prop_tra-sua.png` (idle); trạng thái `prop_tra-sua_b1.png` … `_b3.png` trong SpriteLibrary `Prop/idle, b1, b2, b3` |

**Visual Description:** quầy gỗ nhỏ mái vải sọc kem-hồng, trên quầy có hũ trân châu thuỷ tinh và chồng ly nhựa; giữa quầy có khay nhỏ (điểm `ProductSpawn`). Trạng thái: hũ trân châu đầy → vơi một nấc; chồng ly cao → thấp một ly. Không vẽ thỏ, không vẽ bóng, không vẽ giọt sữa (hạt là Shuriken).

**Art Bible Anchors:**
- §3: khối bo tròn, đế rộng; quầy là hình anh hùng của ô.
- §4: gỗ ấm, vải Paper/Blush; không màu nào sáng hơn thỏ.
- §8: pivot Bottom Center, cùng tỉ lệ giữa các trạng thái.

**Generation Prompt:** "Use the image-generator subagent to create ONE image … Style reference: /tmp/agy-art/style-ref.png (soft storybook watercolor and gouache, chubby rounded shapes, thin dark purple ink outlines #3A2A4D, no text). Content: a sheet of exactly 4 versions of the SAME small wooden milk-tea stall with a cream and pink striped cloth awning, seen from a gentle front three-quarter view, arranged 2 columns x 2 rows with wide gaps, on a flat uniform dark green background (#2E5E49). Identical stall, size and angle in all four; only these differ: (1) full glass jar of tapioca pearls, tall stack of 5 cups; (2) same as 1; (3) pearl jar one step lower, stack of 4 cups; (4) same as 3. A small empty tray in the middle of the counter. No rabbit, no people, no shadow on the ground, no splashes, no text." Negative: realistic, photo, black outlines, ground shadow, steam, text, logo.

**Status:** Needed

## ASSET-003 — Món trà sữa trên khay

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (món) |
| Dimensions | cạnh dài 512 nguồn, hiển thị ~0,35 chiều cao thỏ |
| Format | PNG RGBA |
| Naming | `product_tra-sua.png` (kiểm bản hiện có trong `Art/Final` trước khi vẽ lại) |

**Visual Description:** ly trà sữa caramel có trân châu ở đáy, nắp vòm và ống hút hồng `#F6A5B8`; đứng thẳng, đọc được ở 40 px.

**Art Bible Anchors:** §3 khối tròn; §4 Paper/Blush, viền Ink; §8 PNG sạch viền.

**Generation Prompt:** dùng lại tấm `po-37` nếu `product_tra-sua.png` đạt; nếu vẽ lại: "one cute chubby milk tea cup with dark pearls at the bottom, dome lid and a pink (#F6A5B8) straw, storybook watercolor, thin purple ink outline #3A2A4D, front view, on flat #2E5E49, no shadow, no text."

**Status:** Needed (kiểm trước)

## ASSET-004 — Luống chè và giỏ: trạng thái trạm hái chè

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (billboard trạm) |
| Dimensions | cạnh dài 512, chung một tỉ lệ; luống 0,8–1,0 chiều cao thỏ, giỏ 0,4 |
| Format | PNG RGBA |
| Naming | `prop_gio-la.png` (luống búp non, idle), `prop_gio-la_hai.png` (đã hái); giỏ `item_gio-la_voi.png`, `_nua.png`, `_day.png` |

**Visual Description:** luống chè thấp tròn trên đất nâu, búp non xanh sáng ở ngọn (trạng thái búp non) và ngọn đã hái còn lá sẫm (trạng thái đã hái). Giỏ tre đan tròn có quai, ba mức: vơi, nửa, đầy có lá ló miệng. Thay `Art/Placeholder/prop_gio-la.png`.

**Art Bible Anchors:** §3 khối tròn thấp, lùi sau thỏ; §4 xanh lá đậm hơn Grass một bậc để tách khỏi nền cỏ; §6 dụng cụ đúng nghề.

**Generation Prompt:** "a sheet of exactly 5 items, 3 columns x 2 rows (last cell empty), storybook watercolor, thin purple ink outlines #3A2A4D, flat #2E5E49 background, same scale for all: (1) a low round tea bush on a small mound of brown soil with bright young leaf buds on top; (2) the same bush after picking, darker leaves, no buds; (3) a round woven bamboo basket with a handle, nearly empty; (4) same basket half full of tea leaves; (5) same basket full with leaves peeking out. No shadows, no rabbit, no text."

**Status:** Needed

## ASSET-005 — Bó lá chè buộc lạt (món hái chè)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (món) |
| Dimensions | cạnh dài 512 nguồn, hiển thị ~0,35 chiều cao thỏ |
| Format | PNG RGBA |
| Naming | `product_gio-la.png` (kiểm bản hiện có) |

**Visual Description:** bó lá chè non xanh tươi buộc bằng lạt tre vàng nhạt, nằm nghiêng; đọc được ở 40 px.

**Art Bible Anchors:** §4 xanh lá, lạt Lantern nhạt; §8 PNG sạch viền.

**Generation Prompt:** "one small bundle of fresh green tea leaves tied with a pale bamboo strip, lying slightly tilted, storybook watercolor, thin purple ink outline #3A2A4D, flat #2E5E49 background, no shadow, no text."

**Status:** Needed (kiểm trước)

## ASSET-006 — Ấm trà sương: trạng thái bếp và ấm

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (billboard trạm) |
| Dimensions | cạnh dài 512, chung một tỉ lệ; trạm 1,0–1,3 chiều cao thỏ |
| Format | PNG RGBA |
| Naming | `prop_am-tra.png` (idle, đã có ở `Art/Final`); trạng thái `prop_am-tra_nap-mo.png`, `_nap-dong.png`, `_ngam.png` |

**Visual Description:** ấm men xanh ngọc có hoạ tiết trăng trên bếp than nhỏ đỏ ấm. Ba trạng thái: nắp mở; nắp đóng; ngấm (thân ấm sẫm hơn một bậc). Giữ đúng hình `prop_am-tra.png` hiện có, chỉ đổi nắp và độ sẫm. Không vẽ hơi nước.

**Art Bible Anchors:** §3 ấm là hình anh hùng của trạm; §4 xanh ngọc, than Carrot; §8 cùng tỉ lệ với bản idle.

**Generation Prompt:** sửa trên bản có sẵn (Antigravity, tham chiếu là chính `prop_am-tra.png`): "three versions of THIS exact teapot on its little brazier, same size and angle: (1) lid lifted and resting beside the opening; (2) lid closed; (3) lid closed and the teapot body one shade darker as if steeping. Keep everything else identical. Flat #2E5E49 background, no steam, no shadow, no text."

**Status:** Needed

## ASSET-007 — Chén trà trên đĩa (món pha trà)

| Field | Value |
|-------|-------|
| Category | Sprite / 2D Art (món) |
| Dimensions | cạnh dài 512 nguồn, hiển thị ~0,3 chiều cao thỏ |
| Format | PNG RGBA |
| Naming | `product_am-tra.png` (kiểm bản hiện có) |

**Visual Description:** chén men xanh ngọc nhỏ đựng trà hổ phách trên đĩa tròn, cùng men với ấm.

**Art Bible Anchors:** §4 xanh ngọc, trà Lantern; §8 PNG sạch viền.

**Generation Prompt:** "one small teal porcelain tea cup with amber tea on a round saucer matching a teal moon teapot, storybook watercolor, thin purple ink outline #3A2A4D, flat #2E5E49, no steam, no shadow, no text."

**Status:** Needed (kiểm trước)

## ASSET-008 — Hạt cho ba trạm (Shuriken)

| Field | Value |
|-------|-------|
| Category | VFX / Particles |
| Dimensions | texture hạt 64×64 hoặc 128×128, mềm, nền trong suốt |
| Format | PNG RGBA (texture), `ParticleSystem` prefab |
| Naming | `vfx_giot-sua.png`, `vfx_giot-tra.png`, `vfx_la-che.png`, `vfx_hoi-nuoc.png`, `vfx_lap-lanh.png` |

**Visual Description:** giọt sữa trắng kem và giọt trà hổ phách tròn nhỏ; lá chè non xoay nhẹ; hơi nước là đám mờ trắng ngà tan dần (không trắng tinh, alpha ≤ 0,5); lấp lánh 4 cánh màu Lantern cho nhịp 4. Số hạt mỗi nhịp theo §4a (3–5 giọt, 1–3 lá, hơi 0,8–1,2 s).

**Art Bible Anchors:** §1 ít mà rõ; quy tắc nhà số 3 (hiệu ứng là Shuriken, không vẽ vào hình); §4 không trắng tinh.

**Generation Prompt:** "a sheet of 5 tiny soft particle sprites on flat #2E5E49, storybook watercolor, no outlines except the leaf: (1) a round cream milk droplet; (2) a round amber tea droplet; (3) a small young tea leaf with a thin purple outline; (4) a soft off-white steam puff with feathered edges; (5) a small four-point golden sparkle (#FFC866). No text."

**Status:** Needed
