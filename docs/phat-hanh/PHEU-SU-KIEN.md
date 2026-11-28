# Bảng phễu sự kiện

Mỗi sự kiện trong `Core/Analytics.cs` (`AnalyticsEvents.All`) và câu hỏi nó trả lời. Mọi bản build hiện nay vẫn không gửi gì: adapter Firebase chỉ bật với `VUONTRA_FIREBASE` trên mobile và `GameRunner.AnalyticsInPlayerBuilds` đang tắt (#100). Mỗi sự kiện ghi một lần cho một hành động, không ghi theo khung.

Khi cờ bán hàng tắt (`Features.Monetization`, mặc định, #144), các sự kiện `golden_packs_opened`, `purchase_success`, `purchase_failed`, `rewarded_ad`, `offer_shown`, `offer_bought`, `offer_expired`, `monthpass_bought`, `pass_bought`, `pass_day_claimed` không được ghi (`AnalyticsEvents.Monetization`). `monthpass_tier` vẫn ghi vì hàng miễn phí vẫn chạy.

## Vòng 1 (#100 và các issue bán hàng)

| Sự kiện | Tham số | Câu hỏi |
|---|---|---|
| `game_open` | `new_garden` | Bao nhiêu người mở game, bao nhiêu là vườn mới? |
| `play_session_start` / `play_session_end` | `moon`, `level` / `seconds` | Mỗi phiên dài bao lâu, người chơi ở trăng và cấp nào? |
| `tutorial_step` / `tutorial_complete` / `tutorial_skip` | `step`, `step_name` | Người mới rơi ở bước hướng dẫn nào? |
| `garden_level_up` | `level`, `moon` | Cấp vườn lên nhanh cỡ nào? |
| `station_unlocked` | `station`, `station_index`, `moon` | Trạm nào mở, ở trăng nào? |
| `segment_opened` | `segments`, `moon` | Người chơi mở thêm đất tới đâu? |
| `rabbit_called` | `via`, `tier`, `kind` | Thỏ được gọi bằng gì, ra bậc nào? |
| `new_moon` | `moon` | Bao nhiêu người qua trăng mới? |
| `golden_packs_opened` | `from` | Bao nhiêu người mở thẻ gói cà rốt vàng? |
| `purchase_success` / `purchase_failed` | `product_id`, `price`, `late` / `reason` | Gói nào bán, mua hỏng vì sao? |
| `rewarded_ad` | `placement`, `game`, `result` | Video thưởng ở chỗ nào được xem hết? |
| `newcomer_task` / `newcomer_day` | `day`, `slot`, `task`, `fallback` | Tuần đầu: người mới làm tới ngày nào? |
| `offer_shown` / `offer_bought` / `offer_expired` | `product_id`, `level` | Ưu đãi hiện bao nhiêu lần thì có người mua? |
| `review_asked` / `review_liked` / `review_feedback` | `moment`, `asks`, `sent`, `chars` | Hỏi đánh giá lúc nào thì được thích? |
| `monthpass_tier` / `monthpass_bought` | `tier`, `premium`, `month` | Vé tháng: người chơi lên tới nấc nào, có mua bản trả phí không? |
| `welcome_back` | `days_away` | Người quay lại đã đi vắng bao lâu? |
| `overnight_story` | `kind` (`leaves`, `clover`, `stranger`, `bond`, `doze`), `claimed` (1 = chạm nhận, 0 = bị chuyện mới đẩy đi khi chưa nhận) | Chuyện "Đêm qua…" (#174) nào được kể, người chơi có chạm nhận không? |
| `trunk_open` / `trunk_buy` | `window`, `items` / `item`, `price`, `sale` | Rương xe đẩy: bao nhiêu người mở, món nào bán? |
| `pass_bought` / `pass_day_claimed` | `product_id`, `price`, `late` / `day`, `days` | Vé trăng: bao nhiêu người mua, nhận quà ngày đủ không? |

## Vòng 2 (#117)

| Sự kiện | Tham số | Ghi khi | Câu hỏi |
|---|---|---|---|
| `intro_shown` | `id` | Thẻ giới thiệu tính năng (#104) hiện lên | Tính năng nào được giới thiệu, bao nhiêu người thấy thẻ? |
| `intro_try` | `id`, `from` (`card` = bấm "Thử ngay", `dot` = chạm chấm vàng) | Người chơi bấm "Thử ngay" hoặc chạm chấm vàng | Thẻ nào khiến người chơi thử ngay? Chấm vàng có kéo người quay lại không? |
| `intro_later` | `id`, `from` (`card` = bấm "Để sau", `covered` = thẻ bị màn khác che nên tự cất) | Thẻ được cất để sau | Thẻ nào bị gạt đi nhiều? Bao nhiêu thẻ bị che mất? |
| `feature_first_use` | `feature` (`fishing`, `pounding`, `minigame`, `layout`, `photo`, và từ #122: `recipe_book`, `tank`, `trunk`, `fair`, `month_pass`, `week_chest`), `game` (`leaves`/`fireflies`, chỉ với `minigame`) | Lần đầu xong một mẻ câu, một ván giã mochi, một ván trò nhỏ, lần đầu xếp lại vườn, tấm ảnh đầu tiên; từ #122: lần đầu mở sổ công thức, thả cá vào bể, mở rương xe đẩy, mở thẻ hội, nhận nấc vé tháng, mở rương tuần (chỉ khi thẻ của nó đã tới lượt) | Bao nhiêu người thật sự dùng từng tính năng? Ghép với `intro_shown`/`intro_try` thành phễu thấy → thử → dùng. |
| `tank_release` | `kind` (số loài cá 0–5), `count` (số cá loài đó trong bể sau khi thả) | Mỗi con cá thả vào bể (#114) | Người chơi có thả cá không, loài nào? |
| `tank_milestone` | `step` (mốc 1–4: 1, 5, 20, 50 con), `kind` | Một loài vừa chạm mốc | Người chơi đi tới mốc nào của bể? |
| `tank_full` | `count` (tổng cá trong bể) | Lần đầu đủ 6 loài | Bao nhiêu người đủ bộ 6 loài, sau bao nhiêu cá? |
| `recipe_leaf` | `count`, `from` (`order`, `fish`, `away`, `overnight`) | Nhận lá công thức (#110) từ đơn, cho thỏ ăn cá, gom lúc vắng, hoặc từ chuyện "Đêm qua…" (#174) | Lá đến từ đâu, có đủ để nâng món không? |
| `dish_unlocked` | `station`, `dish` (món 2 hoặc 3), `moon` | Một trạm vừa mở món mới | Người chơi mở tới món thứ mấy, ở trăng nào? |
| `dish_level` | `station`, `dish` (đếm từ 1), `level` (cấp mới) | Nâng một món trong sổ công thức | Sổ công thức có được dùng không, món nào được nâng? |
| `fair_start` | `fair` | Người chơi vào game trong một lễ hội (#112) và nhập hội | Bao nhiêu người gặp từng hội? |
| `fair_coins` | `coins`, `from` (`order`, `photo_ask`, `pounding`, `leaves`, `fireflies`, `fish`) | Mỗi lần kiếm tiền hội | Tiền hội đến từ đâu, một hội kiếm được bao nhiêu? |
| `fair_trade` | `item` (`jars`, `clovers`, `golden`, `lantern`, `frame`, `rabbit`), `cost`, `fair` | Mỗi lần đổi quà ở quầy hội | Quầy nào được đổi, người chơi tiêu tiền hội vào đâu? |
| `fair_rabbit` | `fair`, `kind` | Đổi được thỏ hội | Bao nhiêu người đủ tiền đổi thỏ hội? |
| `daily_set_done` | `day` (ngày chơi thứ mấy, đếm từ 1) | Nhận nhiệm vụ ngày cuối cùng trong bộ (#103) | Bao nhiêu người làm hết nhiệm vụ ngày, vào ngày chơi thứ mấy? |
| `week_chest` | `chest` (1–3), `from` (`week` = mở trong tuần, `mail` = nhận rương bị lỡ qua thư) | Mở một rương tuần | Người chơi mở được mấy rương mỗi tuần? Bao nhiêu rương bị lỡ? |
| `achievement_tier` | `chain` (id chuỗi), `tier` (nấc vừa nhận, đếm từ 1) | Nhận một nấc thành tựu (#111) | Chuỗi nào được theo, người chơi lên tới nấc nào? |

### Phễu đọc thế nào

- **Thẻ giới thiệu:** `intro_shown` → `intro_try` (card) hoặc `intro_later` → `intro_try` (dot) → `feature_first_use`.
- **Bể cá:** `feature_first_use` (fishing) → `tank_release` → `tank_milestone` → `tank_full`.
- **Sổ công thức:** `recipe_leaf` → `dish_level`; `dish_unlocked` cho biết món mới mở theo cấp trạm.
- **Lễ hội:** `fair_start` → `fair_coins` → `fair_trade` → `fair_rabbit`.
- **Nhiệm vụ:** `daily_set_done` → `week_chest`; `achievement_tier` cho chuỗi thành tựu.

### Không ghi

- Lá công thức nhận dần khi thỏ ngồi nghỉ trong lúc đang chơi: lá đến theo thời gian chứ không theo hành động, nên chỉ ghi phần gom lúc vắng (`from = away`).
- Vườn cũ đã qua ngưỡng một tính năng trước bản này được coi là đã dùng (`Intros.Settle`), nên không có `feature_first_use` cho tính năng đó.
- Không có hộp đồng ý (#42), không dashboard, không chọn SDK (#100).
