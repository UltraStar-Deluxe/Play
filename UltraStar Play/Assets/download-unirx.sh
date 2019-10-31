#!/bin/sh
#-------------------------------------------
old_dir=$(PWD)

echo "Removing old UniRx folder..."
rm --recursive --force Plugins/UniRx
mkdir Plugins/UniRx
cd Plugins/UniRx

echo "Cloning UniRx from remote..."
git init
git remote add origin https://github.com/neuecc/UniRx.git
git config core.sparsecheckout true
echo "Assets/Plugins/UniRx/Scripts/*" >> .git/info/sparse-checkout
echo "Assets/Plugins/UniRx/ReadMe.txt" >> .git/info/sparse-checkout
git pull --depth=1 origin master

echo "Moving downloaded files to correct position for this project..."
mv --verbose Assets/Plugins/UniRx/* ./
rm --recursive ./Assets

cd "$old_dir"
echo "Downloading UniRx done"
#-------------------------------------------