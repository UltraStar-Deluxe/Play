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
echo "Assets/Resources/GM Bank - Piano/*" >> .git/info/sparse-checkout
echo "Assets/ThirdParty/*" >> .git/info/sparse-checkout
# UltraStar-Play-subset is a dedicated branch for the UltraStar Play project
git pull --depth=100 origin UltraStar-Play-subset
# Commit from 6 February 2023: 082f7acba885a4e620e6c7f490666059befdd932
git checkout 082f7acba885a4e620e6c7f490666059befdd932

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/* ./
rm -rf ./Assets
rm -rf .git

cd "$old_dir"
echo "Downloading CSharpSynthForUnity done"
echo ""
