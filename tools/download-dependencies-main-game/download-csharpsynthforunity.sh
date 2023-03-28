#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old CSharpSynthForUnity folder..."
rm -rf CSharpSynthForUnity
mkdir CSharpSynthForUnity
cd CSharpSynthForUnity

echo "Cloning CSharpSynthForUnity from remote..."
git init
git remote add origin https://github.com/KNCarnage/CSharpSynthForUnity2.0.git
git config core.sparsecheckout true
echo "Assets/ThirdParty/*" >> .git/info/sparse-checkout
git pull --depth=100 origin main
# Commit from 21 February 2023: 806bce0820d2611f804066e06e1cd3842439addd
git checkout 806bce0820d2611f804066e06e1cd3842439addd

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/* ./
rm -rf ./Assets
rm -rf .git

cd "$old_dir"
echo "Downloading CSharpSynthForUnity done"
echo ""
