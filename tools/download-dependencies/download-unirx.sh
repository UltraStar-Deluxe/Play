#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old UniRx folder..."
rm --recursive --force UniRx
mkdir UniRx
cd UniRx

echo "Cloning UniRx from remote..."
git init
git remote add origin https://github.com/neuecc/UniRx.git
git config core.sparsecheckout true
echo "Assets/Plugins/UniRx/Scripts/*" >> .git/info/sparse-checkout
echo "Assets/Plugins/UniRx/ReadMe.txt" >> .git/info/sparse-checkout
# commit from 1st July 2019: 66205df49631860dd8f7c3314cb518b54c944d30
git pull --depth=100 origin master
git checkout 66205df49631860dd8f7c3314cb518b54c944d30

echo "Moving downloaded files to correct position for this project..."
mv --verbose Assets/Plugins/UniRx/* ./
rm --recursive ./Assets

cd "$old_dir"
echo "Downloading UniRx done"
echo ""
