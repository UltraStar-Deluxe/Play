#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old NLayer folder..."
rm -rf NLayer
mkdir NLayer
cd NLayer

echo "Cloning NLayer from remote..."
git init
git remote add origin https://github.com/naudio/NLayer.git
git config core.sparsecheckout true
echo "NLayer/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# commit from 09 Mar 2020: 13f403f42388857fff347e6cc00824a5216ec754
git checkout 13f403f42388857fff347e6cc00824a5216ec754

echo "Moving downloaded files to correct position for this project..."
mv -v NLayer/* ./
rm -rf ./NLayer

cd "$old_dir"
echo "Downloading NLayer done"
