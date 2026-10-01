#!/bin/bash
export ANDROID_HOME=/opt/android-sdk
export ANDROID_AVD_HOME=/opt/data/.android/avd
export ANDROID_SDK_ROOT=/opt/android-sdk
emulator_pid=0
cleanup() {
  if [ $emulator_pid -ne 0 ]; then
    kill $emulator_pid
  fi
}
trap cleanup EXIT

"$ANDROID_HOME/emulator/emulator" -avd test_tv -no-window -no-audio &
emulator_pid=$!

# Wait for device
timeout=60
while [ $timeout -gt 0 ]; do
  if "$ANDROID_HOME/platform-tools/adb" devices | grep -q 'test_tv.*device'; then
    echo "Device is online"
    break
  fi
  sleep 1
  timeout=$((timeout-1))
done

if [ $timeout -eq 0 ]; then
  echo "Timeout waiting for device"
  exit 1
fi

echo "Installing APK..."
"$ANDROID_HOME/platform-tools/adb" install -r src/TvOptimizer.App/bin/Debug/net10.0-android/com.optimizer.tv.apk
install_result=$?

exit $install_result