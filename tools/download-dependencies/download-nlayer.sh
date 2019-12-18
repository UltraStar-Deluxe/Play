#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old NLayer folder..."
rm --recursive --force NLayer
mkdir NLayer
cd NLayer

echo "Cloning NLayer from remote..."
git init
git remote add origin https://github.com/naudio/NLayer.git
git config core.sparsecheckout true
echo "NLayer/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# commit from 31 Mar 2018: 51ca3ec1304f0e2bbaa3cadca69013f4af8ae6f1
git checkout 51ca3ec1304f0e2bbaa3cadca69013f4af8ae6f1

echo "Moving downloaded files to correct position for this project..."
mv --verbose NLayer/* ./
rm --recursive ./NLayer

cd "$old_dir"
echo "Downloading NLayer done"
