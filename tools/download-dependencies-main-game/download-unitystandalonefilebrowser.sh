#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old UnityStandaloneFileBrowser folder..."
rm -rf UnityStandaloneFileBrowser
mkdir UnityStandaloneFileBrowser
cd UnityStandaloneFileBrowser

echo "Cloning UnityStandaloneFileBrowser from remote..."
git init
git remote add origin https://github.com/achimmihca/UnityStandaloneFileBrowser.git
git config core.sparsecheckout true
echo "Assets/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 12 March 2024: e75bcc71eb721b8979de6f4653fb507b5dbc2172
git checkout e75bcc71eb721b8979de6f4653fb507b5dbc2172

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/* ./
rm -rf ./Assets
rm -rf .git

echo "Create assembly definition (asmdef)"
echo "{ \"name\": \"UnityStandaloneFileBrowser\", \"includePlatforms\": [], \"references\": [] }" >  "./UnityStandaloneFileBrowser.asmdef"

cd "$old_dir"
echo "Downloading UnityStandaloneFileBrowser done"
echo ""

