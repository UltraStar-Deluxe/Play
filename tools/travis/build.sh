#!/usr/bin/env bash

set -e
set -x

echo "Building for $BUILD_TARGET"

export BUILD_PATH=/project/Builds/$BUILD_TARGET/
mkdir -p $BUILD_PATH

/opt/Unity/Editor/Unity \
    -projectPath "$(pwd)/UltraStar Play" \
    -quit \
    -batchmode \
    -nographics \
    -buildTarget $BUILD_TARGET \
    -customBuildTarget $BUILD_TARGET \
    -customBuildName "$BUILD_NAME" \
    -customBuildPath $BUILD_PATH \
    -customBuildOptions AcceptExternalModificationsToPlayer \
    -executeMethod BuildCommand.PerformBuild \
    -logFile /dev/stdout

UNITY_EXIT_CODE=$?

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred";
elif [ $UNITY_EXIT_CODE -eq 2 ]; then
  echo "Run succeeded, some tests failed";
elif [ $UNITY_EXIT_CODE -eq 3 ]; then
  echo "Run failure (other failure)";
else
  echo "Unexpected exit code $UNITY_EXIT_CODE";
fi

ls -la "$BUILD_PATH"


tar -zcf /project/UltraStarPlay-build.tar.gz /project/Builds/
