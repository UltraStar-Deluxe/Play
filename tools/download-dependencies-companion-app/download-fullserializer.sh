#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play Companion/Assets/Plugins"
echo "Removing old FullSerializer folder..."
rm -rf FullSerializer
mkdir FullSerializer
cd FullSerializer

echo "Cloning FullSerializer from remote..."
git init
git remote add origin https://github.com/jacobdufault/fullserializer.git
git config core.sparsecheckout true
echo "Assets/FullSerializer/Source/*" >> .git/info/sparse-checkout
# commit from 30 Mar 2017: c01db302f337205696585daa72e7d7baea922e44
git pull --depth=1 origin c01db302f337205696585daa72e7d7baea922e44

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/FullSerializer/* ./
rm -rf ./Assets
rm -rf .git

echo "Creating assembly definition for editor scripts"
echo "{ \"name\": \"FullSerializerEditor\", \"includePlatforms\": [\"Editor\"], \"references\": [\"Plugins\"] }" >  "./Source/Aot/Editor/FullSerializerEditor.asmdef"

cd "$old_dir"
echo "Downloading FullSerializer done"
