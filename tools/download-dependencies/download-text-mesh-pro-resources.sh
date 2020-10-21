#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old TextMesh Pro folder..."
rm -rf "TextMesh Pro"
mkdir "TextMesh Pro"
cd "TextMesh Pro"

echo "Cloning TextMesh Pro from remote..."
git init
git remote add origin https://github.com/UltraStar-Deluxe/text-mesh-pro-resources.git
git config core.sparsecheckout true
echo "Assets/*" >> .git/info/sparse-checkout
# UltraStar-Play-subset is a dedicated branch for the UltraStar Play project
git pull --depth=100 origin master
# Commit from 12 October 2020: 24e67a6bc789d3e40946322ea7d253032e82541d
git checkout 24e67a6bc789d3e40946322ea7d253032e82541d

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/TextMesh\ Pro/* ./
rm -rf ./Assets

cd "$old_dir"
echo "Downloading TextMesh Pro done"
echo ""
