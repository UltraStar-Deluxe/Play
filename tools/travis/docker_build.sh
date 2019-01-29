#!/usr/bin/env bash

set -e

docker run \
  -e BUILD_NAME \
  -e UNITY_LICENSE_CONTENT \
  -e BUILD_TARGET \
  -e UNITY_USERNAME \
  -e UNITY_PASSWORD \
  -w /project/ \
  -v $(pwd):/project/ \
  $IMAGE_NAME \
  /bin/bash -c "/project/tools/travis/before_script.sh && /project/tools/travis/build.sh"