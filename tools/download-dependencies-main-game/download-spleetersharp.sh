#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old SpleeterSharp folder..."
rm -rf SpleeterSharp
mkdir SpleeterSharp
cd SpleeterSharp

echo "Cloning SpleeterSharp from remote..."
git init
git remote add origin https://github.com/achimmihca/SpleeterSharp
git config core.sparsecheckout true
echo Source/SpleeterSharp/* >> .git/info/sparse-checkout
git pull --depth=100 origin main
# Commit from 21 December 2022: e0d9bb574223b5266e81fa9c02fe6df39cfce5d8
git checkout e0d9bb574223b5266e81fa9c02fe6df39cfce5d8

echo "Moving downloaded files to correct position for this project..."
mv -v Source/SpleeterSharp/* ./
rm -rf Source
rm -rf .git

echo "Create assembly definition (asmdef)"
echo "{ \"name\": \"SpleeterSharp\", \"includePlatforms\": [], \"references\": [] }" >  "./SpleeterSharp.asmdef"

cd "$old_dir"
echo "Downloading SpleeterSharp done"
echo ""
