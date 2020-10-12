#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old CSharpSynthForUnity folder..."
rm -rf CSharpSynthForUnity
mkdir CSharpSynthForUnity
cd CSharpSynthForUnity

echo "Cloning CSharpSynthForUnity from remote..."
git init
git remote add origin https://github.com/UltraStar-Deluxe/CSharpSynthForUnity.git
git config core.sparsecheckout true
echo "Assets/*" >> .git/info/sparse-checkout
# UltraStar-Play-subset is a dedicated branch for the UltraStar Play project
git pull --depth=100 origin UltraStar-Play-subset
# Commit from 12 October 2020: 0e1163517a9e5561cd6862ecd0d032803dea8f51
git checkout 0e1163517a9e5561cd6862ecd0d032803dea8f51

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/* ./
rm -rf ./Assets

cd "$old_dir"
echo "Downloading CSharpSynthForUnity done"
echo ""
