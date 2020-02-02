#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old CSharpSynthForUnity folder..."
rm --recursive --force CSharpSynthForUnity
mkdir CSharpSynthForUnity
cd CSharpSynthForUnity

echo "Cloning CSharpSynthForUnity from remote..."
git init
git remote add origin https://github.com/achimmihca/CSharpSynthForUnity.git
git config core.sparsecheckout true
echo "Assets/*" >> .git/info/sparse-checkout
# commit from 2nd February 2020: 05d40843524537fe5159e277717a74f3f5720475
git pull --depth=100 origin UltraStar-Play-subset
git checkout UltraStar-Play-subset

echo "Moving downloaded files to correct position for this project..."
mv --verbose Assets/* ./
rm --recursive ./Assets

cd "$old_dir"
echo "Downloading CSharpSynthForUnity done"
echo ""
