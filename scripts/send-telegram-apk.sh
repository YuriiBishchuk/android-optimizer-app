#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS_DIR="$REPO_ROOT/artifacts"

mkdir -p "$ARTIFACTS_DIR"

# 1. Locate secrets
SECRETS_FILE="${HOME}/.config/ai-secrets/telegram.env"
if [[ -f "$SECRETS_FILE" ]]; then
    # Load secrets without overriding existing exported vars
    while IFS='=' read -r key val || [[ -n "$key" ]]; do
        [[ -z "$key" || "$key" =~ ^# ]] && continue
        key="$(echo "$key" | xargs)"
        val="$(echo "$val" | xargs)"
        if [[ -z "${!key:-}" ]]; then
            export "$key"="$val"
        fi
    done < "$SECRETS_FILE"
fi

# 2. Determine bot token
TOKEN="${TELEGRAM_BOT_TOKEN:-}"
if [[ -z "$TOKEN" ]]; then
    echo "ERROR: TELEGRAM_BOT_TOKEN is not set and could not be loaded from $SECRETS_FILE" >&2
    exit 1
fi

# 3. Determine chat ID
CHAT_ID="${TELEGRAM_CHAT_ID:-${TELEGRAM_ALLOWED_USERS:-292510743}}"
# Handle comma-separated list of allowed users (take first)
CHAT_ID="$(echo "$CHAT_ID" | cut -d',' -f1 | xargs)"

if [[ -z "$CHAT_ID" ]]; then
    echo "ERROR: Could not determine Telegram CHAT_ID" >&2
    exit 1
fi

# 4. Find or build latest APK
APK_PATH="${1:-}"
if [[ -z "$APK_PATH" ]]; then
    # Look for most recent APK in artifacts
    APK_PATH="$(ls -t "$ARTIFACTS_DIR"/*.apk 2>/dev/null | head -n 1 || true)"
fi

if [[ -z "$APK_PATH" || ! -f "$APK_PATH" ]]; then
    echo "==> No pre-existing APK found, triggering release build..."
    bash "$REPO_ROOT/scripts/release-debug-apk.sh"
    APK_PATH="$(ls -t "$ARTIFACTS_DIR"/*.apk 2>/dev/null | head -n 1)"
fi

if [[ ! -f "$APK_PATH" ]]; then
    echo "ERROR: Could not locate or build APK at $APK_PATH" >&2
    exit 1
fi

FILE_NAME="$(basename "$APK_PATH")"
FILE_SIZE="$(du -h "$APK_PATH" | cut -f1)"
SHA256="$(sha256sum "$APK_PATH" | cut -d' ' -f1)"

echo "==> Sending APK to Telegram..."
echo "    File: $FILE_NAME ($FILE_SIZE)"
echo "    Chat: $CHAT_ID"
echo "    SHA256: $SHA256"

CAPTION="🚀 <b>Android Optimizer v0.2.0</b> release APK ready!
📱 <i>Universal No-Root Debloater & Tweaks for TV, Mobile & Tablet</i>
📦 <b>File:</b> <code>${FILE_NAME}</code> (${FILE_SIZE})
🛡️ <b>SHA256:</b> <code>${SHA256}</code>
✅ All Kanban tasks completed and verified."

RESP=$(curl -s -w "\n%{http_code}" \
    -F "chat_id=${CHAT_ID}" \
    -F "document=@${APK_PATH}" \
    -F "caption=${CAPTION}" \
    -F "parse_mode=HTML" \
    "https://api.telegram.org/bot${TOKEN}/sendDocument")

HTTP_CODE=$(echo "$RESP" | tail -n 1)
BODY=$(echo "$RESP" | sed '$d')

if [[ "$HTTP_CODE" == "200" ]] && echo "$BODY" | grep -q '"ok":true'; then
    echo "==> SUCCESS: APK sent to Telegram chat $CHAT_ID!"
else
    echo "ERROR: Telegram sendDocument failed (HTTP $HTTP_CODE): $BODY" >&2
    exit 1
fi
