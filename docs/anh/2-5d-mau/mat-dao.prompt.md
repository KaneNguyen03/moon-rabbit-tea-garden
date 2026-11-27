# mat-dao, vach-dao — mặt cỏ và vách đảo

| | |
|---|---|
| Tệp ra | `mat-dao.png` (ô cỏ lặp 1024×1024 cho 3,4 m), `vach-dao.png` (dải vách lặp ngang 2048×256) |
| Thay cho | `Art/Ground/bg_grass.png` (#129, chấm sáng dày nên nhiễu ở zoom mặc định, #168) và màu vách đặc hiện tại |
| Pivot | ô lặp, Wrap Repeat (riêng hai tệp này), Bilinear, không mipmap |
| Cắt | không cắt nền. Làm cho lặp không đường nối: `python3 -I cong-cu/lam-lap.py gốc.png mat-dao.png` (lệch nửa ô, hoà mối nối); vách: thêm tham số `ngang` |

## Prompt mặt đảo

```text
{STYLE}
Override the view: seen straight from above, a seamless tileable texture filling the whole square, no objects, no outline.
Content: image size 1024x1024. A painted watercolor lawn: base grass #3D8369 with very soft larger patches of #44896F and #367A62, sparse small brush-stroke grass blades in moss #6FBF8A, and at most 6-8 tiny scattered cream specks per tile. Low contrast, calm, no high-frequency noise, no flowers, no stones. Must tile seamlessly on all four edges.
```

## Prompt vách đảo

```text
{STYLE}
Override the view: straight side view of a cliff band, seamless horizontally, filling the whole image.
Content: image size 2048x256. The thick earth edge of a floating grass island: at the top a rounded grass lip #366F62 overhanging slightly with a dark gap #2A2449 under it, then layered earth strata from #60424A (upper) to #453448 (lower) with a few embedded rounded stones and small root tendrils, fading to the underside colour #212047 at the bottom edge. No brick red, no bright highlights. Must tile seamlessly left-right.
```

## Kiểm
- Đo cỏ trên màn trong ± 10 % độ sáng và độ bão hoà của `#3D8369` (#168).
- Ở zoom mặc định không đọc thành nhiễu chấm; không thấy mối nối ô.
