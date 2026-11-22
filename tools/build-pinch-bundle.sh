#!/bin/bash
# Compiles the trackpad pinch plugin for the Mac player into Assets/Plugins/macOS/PinchZoom.bundle.
# The bundle is committed, so this only needs running after PinchZoom.m changes. Needs the Xcode
# command line tools (clang with the Cocoa framework).
set -eu
cd "$(dirname "$0")/.."
CLANG="${CLANG:-/Library/Developer/CommandLineTools/usr/bin/clang}"
SDK="${SDK:-$(xcrun --sdk macosx --show-sdk-path 2>/dev/null || echo /Library/Developer/CommandLineTools/SDKs/MacOSX.sdk)}"
SRC=Assets/Plugins/macOS/PinchZoom/PinchZoom.m
OUT=Assets/Plugins/macOS/PinchZoom.bundle
mkdir -p "$OUT/Contents/MacOS"
cat > "$OUT/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleDevelopmentRegion</key><string>en</string>
	<key>CFBundleExecutable</key><string>PinchZoom</string>
	<key>CFBundleIdentifier</key><string>com.vuontratho.pinchzoom</string>
	<key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
	<key>CFBundleName</key><string>PinchZoom</string>
	<key>CFBundlePackageType</key><string>BNDL</string>
	<key>CFBundleShortVersionString</key><string>1.0</string>
	<key>CFBundleVersion</key><string>1</string>
	<key>LSMinimumSystemVersion</key><string>12.0</string>
</dict>
</plist>
PLIST
# Linked and ad-hoc signed in a temp folder: files made inside Documents carry extended attributes that codesign refuses.
TMP=$(mktemp -d); trap 'rm -rf "$TMP"' EXIT
"$CLANG" -isysroot "$SDK" -fobjc-arc -Wall -Werror -O2 -mmacosx-version-min=12.0 -arch arm64 -arch x86_64 \
  -framework Cocoa -bundle -o "$TMP/PinchZoom" "$SRC"
codesign --force --sign - "$TMP/PinchZoom"
cp "$TMP/PinchZoom" "$OUT/Contents/MacOS/PinchZoom"
echo "built $OUT"
file "$OUT/Contents/MacOS/PinchZoom"
