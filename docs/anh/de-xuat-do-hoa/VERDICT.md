# Tấm mẫu định hướng đồ hoạ — kết quả `visual-critic-gate` (25/11/2026)

Vẽ bằng Antigravity (`agy`, subagent `image-generator`) theo `docs/anh/VE-BANG-ANTIGRAVITY.md`, style ref `docs/anh/po-do-vat/_tong-hop.png`, thỏ mẫu `Art/Final/rabbit.png`. Nguồn: AI. Đây là hướng đi để chủ project và session thiết kế nhìn, không phải hình đưa vào game. Mỗi tấm do một subagent chấm độc lập theo bảy mục của `visual-critic-gate`.

| Tấm | Vòng chấm | Kết quả cuối |
|---|---|---|
| `o-dat-day.png` — một ô đất "đầy": sàn gỗ, bếp và ấm trà, luống chè, giỏ lá, hàng rào tre, lối mòn, đèn lồng, chậu, ghế; `o-dat-day-truoc-sau.png` đặt cạnh khung vườn hiện tại | 1 | **APPROVED** |
| `tra-sua-4-nhip.png` / `.gif`, khung trong `tra-sua-khung/` — quầy trà sữa 4 nhịp: rót sữa, lắc bình, rót ra ly có trân châu, ly lên khay | 5 | **APPROVED** |
| `hud-the.png`, mảnh trong `hud-manh/` — bộ thẻ giấy, ruy băng, nút cà rốt và rêu, ô chiếu tre, đèn lồng tab tắt/sáng, thanh tre | 5 | **APPROVED** |

Bằng chứng: `qa/preview_qa_sheet_tra-sua.png`, `qa/preview_qa_sheet_hud.png` (mỗi hình trên ca rô, cỏ `#3D8369`, tối `#2B2420`, ô phóng 400 %, ảnh chồng các khung, thử kéo giãn 9-slice), `qa/o-dat-day-crop200.png`.

## Tấm trà sữa: lỗi đã sửa qua các vòng

1. Bản vẽ đầu: đồ trên quầy đổi chỗ giữa các khung, đầu thỏ lệch vài px, không có bóng tiếp đất → vẽ lại với ràng buộc "quầy và đồ trên quầy giống hệt, chỉ tay, mặt và đồ cầm thay đổi", có bóng.
2. Bản hai vẫn vẽ lại quầy mỗi khung (rung 1–2 px), nét xám nâu, viền xanh quanh hình cắt → ghép một lớp quầy tĩnh duy nhất từ khung 1, chỉ thay vùng thỏ, căn đầu thỏ theo khung 1, đổi nét tối về mực `#3A2A4D`, gọt mép và tô mực; mái sọc xanh xám đổi sang hồng `#F6A5B8` pha kem.
3. Còn một mảnh nét lạc 4 px, rồi vệt bóng ở mép chồng ly → xoá mảnh lạc, chép dải chồng ly từ khung 1.
4. Ghi chú còn lại (không chặn): thỏ chỉ lệch ≤ 2 px giữa khung vì là bốn tranh vẽ riêng; trong game, rig PSB của pipeline thỏ 2D sẽ thay thân thỏ.

## Tấm HUD: lỗi đã sửa qua các vòng

1. Bản đầu: nút bóng kiểu nhựa, mép giấy xé không đều nên không cắt 9 phần được, đồ trang trí dính vào khung → vẽ lại matte, mép đều, mảnh rời.
2. Viền tím nhạt ngoài nét, ruy băng cong và ô đan không giãn được → cắt từng mảnh ra nền trong, gọt 2 px mép và tô mực; ghi rõ kiểu Image: thẻ và thanh HUD 9-slice, nút 9-slice b80 chỉ giãn ngang, thanh tre 3-slice ngang b60, ruy băng / trang trí góc / ô chiếu / đèn lồng là Simple cỡ cố định.
3. Thanh tre bị cắt rời 5 mảnh, trang trí góc mép răng cưa, hai đèn lệch cỡ → cắt lại liền khối, mép mềm, hai đèn chung khung 282×306.
4. Nét thanh tre xám trung tính, đèn tắt nét mảnh hơn đèn sáng → đổi nét về mực, đèn tắt dùng vòng nét của đèn sáng.
5. Ghi chú còn lại (không chặn): thẻ và nút dùng nét 1 px, đồ vật kiểu đạo cụ (đèn, ô chiếu) nét 3–5 px; giữ đúng hai họ này cho mảnh UI sau.

Script xử lý nằm ngoài repo (`/tmp/fix-dh/`), không đưa vào dự án.
