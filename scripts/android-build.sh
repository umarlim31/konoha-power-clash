#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the licensed Unity 6000.0.60f1 executable on the remote runner}"
if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo 'Unity executable is missing or not executable.' >&2
  exit 2
fi
if [[ -n "$(git status --porcelain --untracked-files=normal)" ]]; then
  echo 'Refusing an unstamped/dirty source build. Commit source changes first.' >&2
  exit 2
fi
export KONOHA_COMMIT
KONOHA_COMMIT="$(git rev-parse HEAD)"
mkdir -p TestResults Builds/Android
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" -buildTarget Android \
  -executeMethod Konoha.Editor.SpikeProject.Prepare -logFile TestResults/prepare.log
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -buildTarget Android \
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile TestResults/editmode.log
python3 scripts/verify-test-results.py TestResults/editmode.xml
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" -buildTarget Android \
  -executeMethod Konoha.Editor.AndroidBuild.Build -logFile TestResults/android-build.log
test -s Builds/Android/KONOHA_0.0.1.apk
python3 scripts/verify-apk.py Builds/Android/KONOHA_0.0.1.apk
sha256sum Builds/Android/KONOHA_0.0.1.apk > Builds/Android/SHA256SUMS
tar -czf Builds/Android/resolved-project-settings.tar.gz ProjectSettings Packages Assets/Konoha/Generated
echo 'APK artifact created. Real Android install/touch/camera gate remains PENDING.'
