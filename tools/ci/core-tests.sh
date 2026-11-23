#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
DOTNET="${DOTNET:-$(command -v dotnet || echo /Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet)}"
mkdir -p obj
{
  echo '<Project><ItemGroup>'
  for f in ../../Assets/Tests/EditMode/*.cs; do
    grep -qE 'UnityEngine|UnityEditor|VuonTra\.Game|VuonTra\.EditorTools' "$f" || echo "    <Compile Include=\"$f\" />"
  done
  echo '</ItemGroup></Project>'
} > obj/pure-tests.props
echo "core-tests: $(grep -c Compile obj/pure-tests.props) test files without Unity references"
"$DOTNET" test CoreTests.csproj --nologo -v q "$@"
