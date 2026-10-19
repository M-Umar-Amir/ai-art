#!/usr/bin/env bash
# Build an installable Android APK on this PC — no EAS cloud build used.
#   bash scripts/build-apk-local.sh
# Toolchain lives on D: (JDK 17, Android SDK, Gradle cache) to spare the C: drive.
# Output: dist-apk/kfc-dallas-dash-<version>.apk
set -euo pipefail
cd "$(dirname "$0")/.."

TOOLS="${ANDROID_TOOLS:-D:/android-build}"
export JAVA_HOME="$TOOLS/jdk17"
export ANDROID_HOME="$TOOLS/sdk"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export ANDROID_USER_HOME="$TOOLS/home/.android"
export GRADLE_USER_HOME="$TOOLS/gradle-home"
export PATH="$JAVA_HOME/bin:$PATH"
export NODE_ENV=production

# 1. the game as one offline page, from the web build's source
node ../dallas-dash/tools/build-mobile.mjs

# 2. the native project, fresh from app.json (icons, name, splash)
CI=1 npx expo prebuild --platform android --clean --no-install
echo "sdk.dir=${ANDROID_HOME//\\//}" > android/local.properties
# the default 512 MB metaspace runs out compiling the Expo modules
sed -i 's|^org.gradle.jvmargs=.*|org.gradle.jvmargs=-Xmx4g -XX:MaxMetaspaceSize=1536m -Dfile.encoding=UTF-8|' android/gradle.properties
printf '\nkotlin.daemon.jvmargs=-Xmx2g -XX:MaxMetaspaceSize=1g\n' >> android/gradle.properties # the file has no trailing newline

# 3. a release APK (signed with the debug key: fine for testing, not for the Play Store)
(cd android && ./gradlew assembleRelease -PreactNativeArchitectures="${ARCHS:-arm64-v8a}" --no-daemon)

version=$(node -p "require('./app.json').expo.version")
mkdir -p dist-apk
out="dist-apk/kfc-dallas-dash-$version.apk"
cp android/app/build/outputs/apk/release/app-release.apk "$out"
echo "APK ready: $out"
