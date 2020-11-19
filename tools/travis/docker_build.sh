#!/usr/bin/env bash

set -e

travis_fold start "unity.docker.build"

docker run \
  -e UNITY_LICENSE_CONTENT \
  -e BUILD_NAME \
  -e BUILD_TARGET \
  -w /project/ \
  -v $(pwd):/project/ \
  $IMAGE_NAME \
  /bin/bash -c "/project/tools/travis/before_script.sh && /project/tools/travis/build.sh"
  
  travis_fold end "unity.docker.build"
