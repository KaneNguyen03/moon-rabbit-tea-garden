#!/usr/bin/env bash
set -euo pipefail

PY="${PY:-python3}"
ROOT="$(git rev-parse --show-toplevel)"
OUT="$ROOT/docs/anh/2-5d-mau"
WORK="${WORK:-/tmp/vuontra-2-5d-mau}"
WIP=origin/backup/agent-146-wip
SUA=origin/backup/mac-2026-11-27/wt/MoonRabbitTeaGarden-sua146
OWNER="${OWNER_SHOT:-/mnt/d/Projects/owner-feedback/2026-11-28-do-hoa-xau.png}"

rm -rf "$WORK"
mkdir -p "$WORK/stand" "$WORK/rig" "$WORK/qa"
for f in $(git ls-tree --name-only "$WIP" Assets/VuonTra/Art/Garden25D/Stand/ | grep '\.png$'); do
  git show "$WIP:$f" > "$WORK/stand/$(basename "$f")"
done
for f in ArmL ArmR Body Ears Head; do
  git show "$SUA:docs/anh/do-hoa-tho/rig-cream-25d/truoc/$f.png" > "$WORK/rig/$f.png"
done
git show "$SUA:docs/anh/po-147/decor_lantern.png" > "$WORK/stand/decor_lantern.png"
git show "$WIP:docs/anh/po-146/146-sau.png" > "$WORK/qa/146-sau.png"
git show "$WIP:docs/anh/po-146/146-sau-gan.png" > "$WORK/qa/146-sau-gan.png"
cp "$OWNER" "$OUT/truoc/2d-chu-du-an-28-11.png"

A="$ROOT/Assets/VuonTra/Art"
cat > "$WORK/style.json" <<EOF
{"kind":"style-ref","rabbit_layers":"$WORK/rig","stall":"$WORK/stand/prop_tra-sua.png","lantern":"$WORK/stand/decor_lantern.png","out":"$OUT/_style-ref.png"}
EOF
cat > "$WORK/pool.json" <<EOF
{"kind":"light-pool","out":"$OUT/vung-sang-den.png","size":512}
EOF
cat > "$WORK/before.json" <<EOF
{"kind":"before","out":"$OUT/truoc/bang-truoc.png","title":"Trước: vườn 2D chủ dự án chê (28/11) và spike 2.5D #146 (27/11)","panels":[
 {"path":"$OUT/truoc/2d-chu-du-an-28-11.png","caption":"2D trên web, ảnh chủ dự án 28/11\ntối, đục, vignette 0,62, đế lệch nhau"},
 {"path":"$WORK/qa/146-sau.png","caption":"2.5D #146 (backup agent-146-wip)\ncây, bụi, nồi, nấm, đá là vector viền dày"},
 {"path":"$WORK/qa/146-sau-gan.png","caption":"2.5D #146, phóng gần\nnét trên màn: quầy AI 3,9 px, cây vector 6,5 px, thỏ 2,9 px"}]}
EOF
S="$WORK/stand"
cat > "$WORK/audit.json" <<EOF
{"kind":"audit","rabbit_layers":"$WORK/rig","out":"$OUT/truoc/hien-trang-ti-le.png","items":[
 {"name":"prop_tra-sua","path":"$S/prop_tra-sua.png","metres":2.0,"note":"AI 26/11, nét nâu"},
 {"name":"prop_tiem","path":"$S/prop_tiem.png","metres":2.0,"note":"AI, lưng xám sáng"},
 {"name":"prop_am-tra","path":"$S/prop_am-tra.png","metres":1.15,"note":"AI, bếp gần đen"},
 {"name":"prop_gio-la","path":"$S/prop_gio-la.png","metres":0.9,"note":"AI, nét xanh-nâu"},
 {"name":"prop_coi-gia-banh","path":"$S/prop_coi-gia-banh.png","metres":0.7,"note":"AI, cục trắng chói"},
 {"name":"prop_noi-mut","path":"$S/prop_noi-mut.png","metres":1.15,"note":"vector placeholder"},
 {"name":"decor_tree","path":"$S/decor_tree.png","metres":3.0,"note":"vector, phóng to mờ"},
 {"name":"decor_bush","path":"$S/decor_bush.png","metres":0.6,"note":"vector placeholder"},
 {"name":"decor_lantern","path":"$S/decor_lantern.png","metres":1.4,"note":"AI, nét chuẩn"},
 {"name":"decor_mushrooms","path":"$S/decor_mushrooms.png","metres":0.4,"note":"vector placeholder"},
 {"name":"decor_stones","path":"$S/decor_stones.png","metres":0.3,"note":"vector placeholder"},
 {"name":"decor_den-da","path":"$A/Final/decor_den-da.png","metres":0.9,"note":"AI, đèn xanh lạnh"},
 {"name":"pad_dat","path":"$A/Final/pad_dat.png","metres":0.9,"note":"đế 3/4, tối"},
 {"name":"pad_vong-da","path":"$A/Final/pad_vong-da.png","metres":0.9,"note":"đế 3/4, sáng chói"},
 {"name":"pad_san-go","path":"$A/Final/pad_san-go.png","metres":0.9,"note":"đế 3/4, gỗ bão hoà"},
 {"name":"decor_pond","path":"$A/Placeholder/decor_pond.png","metres":0.9,"note":"vector, nước xanh dương"},
 {"name":"decor_fence","path":"$A/Placeholder/decor_fence.png","metres":0.6,"note":"vector placeholder"}]}
EOF
for s in style pool before audit; do "$PY" -I "$ROOT/docs/anh/2-5d-mau/cong-cu/dung-bang.py" "$WORK/$s.json"; done
echo "$OUT"
