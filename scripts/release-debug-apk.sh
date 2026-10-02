#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS_DIR="$REPO_ROOT/artifacts"

mkdir -p "$ARTIFACTS_DIR"

VERSION="0.2.0"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
TARGET_APK="$ARTIFACTS_DIR/AndroidOptimizer-v${VERSION}-${TIMESTAMP}-Signed.apk"

echo "==> Building Android Optimizer (.NET 10 MAUI Android Release APK for ARM64)..."
cd "$REPO_ROOT"

# Clean stale apks to avoid picking up old artifacts
rm -f "$REPO_ROOT"/src/TvOptimizer.App/bin/Release/net10.0-android/*/*.apk 2>/dev/null || true
rm -f "$REPO_ROOT"/src/TvOptimizer.App/bin/Release/net10.0-android/*.apk 2>/dev/null || true

dotnet build -f net10.0-android -c Release -p:RuntimeIdentifier=android-arm64 -t:SignAndroidPackage src/TvOptimizer.App/TvOptimizer.App.csproj

EXPECTED_APK="$REPO_ROOT/src/TvOptimizer.App/bin/Release/net10.0-android/android-arm64/com.optimizer.android-Signed.apk"

if [[ -f "$EXPECTED_APK" ]]; then
    SOURCE_APK="$EXPECTED_APK"
else
    SOURCE_APK=$(find "$REPO_ROOT/src/TvOptimizer.App/bin" -name "*-Signed.apk" | head -n 1)
fi

if [[ -z "$SOURCE_APK" || ! -f "$SOURCE_APK" ]]; then
    echo "ERROR: Could not find generated signed APK in $REPO_ROOT/src/TvOptimizer.App/bin"
    exit 1
fi

cp "$SOURCE_APK" "$TARGET_APK"
cp "$TARGET_APK" "$ARTIFACTS_DIR/AndroidOptimizer-latest.apk"

echo "==> Artifact successfully created: $TARGET_APK"
echo "==> Latest alias: $ARTIFACTS_DIR/AndroidOptimizer-latest.apk"
echo "==> SHA256: $(sha256sum "$TARGET_APK" | cut -d" " -f1)"
echo "==> Size: $(du -h "$TARGET_APK" | cut -f1)"
