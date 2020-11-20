#!/usr/bin/env bash

set -e
set -x
mkdir -p /root/.cache/unity3d
mkdir -p /root/.local/share/unity3d/Unity
set +x

echo 'Writing Unity license file to /root/.local/share/unity3d/Unity/Unity_lic.ulf'
# The license file is stored in Travis as base64 encoded environment variable.
# tr -d '\r' is used to remove Windows line ending because Unix line ending is required by <SignatureValue> content.
echo "$UNITY_LICENSE_CONTENT" | base64 --decode | tr -d '\r' > /root/.local/share/unity3d/Unity/Unity_lic.ulf
