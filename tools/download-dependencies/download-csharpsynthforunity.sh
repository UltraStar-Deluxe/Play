#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old CSharpSynthForUnity folder..."
rm -rf CSharpSynthForUnity
mkdir CSharpSynthForUnity
cd CSharpSynthForUnity

echo "Cloning CSharpSynthForUnity from remote..."
git init
git remote add origin https://github.com/achimmihca/CSharpSynthForUnity.git
git config core.sparsecheckout true
echo "Assets/*" >> .git/info/sparse-checkout
# UltraStar-Play-subset is a dedicated branch for the UltraStar Play project
git pull --depth=100 origin UltraStar-Play-subset
git checkout UltraStar-Play-subset

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/* ./
rm -rf ./Assets

cd "$old_dir"
echo "Downloading CSharpSynthForUnity done"
echo ""
