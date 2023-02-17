#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old UnityStandaloneFileBrowser folder..."
rm -rf UnityStandaloneFileBrowser
mkdir UnityStandaloneFileBrowser
cd UnityStandaloneFileBrowser

echo "Cloning UnityStandaloneFileBrowser from remote..."
git init
git remote add origin https://github.com/gkngkc/UnityStandaloneFileBrowser.git
git config core.sparsecheckout true
echo "Assets/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 6 November 2018: 04a5d49ed2545556da8a7192e86c69bd47641f10
git checkout 04a5d49ed2545556da8a7192e86c69bd47641f10

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/* ./
rm -rf ./Assets
rm -rf .git

echo "Create assembly definition (asmdef)"
echo "{ \"name\": \"UnityStandaloneFileBrowser\", \"includePlatforms\": [], \"references\": [] }" >  "./UnityStandaloneFileBrowser.asmdef"

cd "$old_dir"
echo "Downloading UnityStandaloneFileBrowser done"
echo ""
