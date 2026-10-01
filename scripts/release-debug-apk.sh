#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS_DIR="$REPO_ROOT/artifacts"

mkdir -p "$ARTIFACTS_DIR"

VERSION="0.2.0"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
TARGET_APK="$ARTIFACTS_DIR/AndroidOptimizer-v${VERSION}-${TIMESTAMP}-Signed.apk"

echo "==> Building Android Optimizer (.NET 10 MAUI Android Release APK)..."
cd "$REPO_ROOT"
dotnet build -f net10.0-android -c Release src/TvOptimizer.App/TvOptimizer.App.csproj

SOURCE_APK="$REPO_ROOT/src/TvOptimizer.App/bin/Release/net10.0-android/com.optimizer.android-Signed.apk"

if [[ ! -f "$SOURCE_APK" ]]; then
    # Fallback if Release signed is at Debug or direct output
    SOURCE_APK="$REPO_ROOT/src/TvOptimizer.App/bin/Debug/net10.0-android/com.optimizer.android-Signed.apk"
fi

if [[ ! -f "$SOURCE_APK" ]]; then
    echo "ERROR: Could not find generated signed APK at $SOURCE_APK"
    exit 1
fi

cp "$SOURCE_APK" "$TARGET_APK"
echo "==> Artifact successfully created: $TARGET_APK"
echo "==> SHA256: $(sha256sum "$TARGET_APK" | cut -d" " -f1)"
echo "==> Size: $(du -h "$TARGET_APK" | cut -f1)"
