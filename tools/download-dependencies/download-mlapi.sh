#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old MLAPI folder..."
rm -rf MLAPI
mkdir MLAPI
cd MLAPI

echo "Cloning MLAPI from remote..."
git init
git remote add origin https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi
git config core.sparsecheckout true
echo "MLAPI/*" >> .git/info/sparse-checkout
echo "MLAPI-Editor/*" >> .git/info/sparse-checkout
echo "LICENSE" >> .git/info/sparse-checkout
# commit from 8 Oct 2020 (v12.1.7): 6bb51310c26a72da6b30d65d1731055471ea171c
git pull --depth=100 origin master
git checkout 6bb51310c26a72da6b30d65d1731055471ea171c

echo "Moving downloaded files to correct position for this project..."
rm -rf MLAPI/Properties/AssemblyInfo.cs
rm -rf MLAPI-Editor/Properties/AssemblyInfo.cs
rm -rf docs/
rm -rf .git

echo "Creating asmdef"
echo "{ \"name\": \"MLAPI\" }" >> MLAPI/MLAPI.asmdef
echo "{ \"name\": \"MLAPI-Editor\", \"references\": [ \"MLAPI\" ], \"includePlatforms\": [ \"Editor\" ]}" >> MLAPI-Editor/MLAPI-Editor.asmdef

cd "$old_dir"
echo "Downloading MLAPI done"
echo ""
