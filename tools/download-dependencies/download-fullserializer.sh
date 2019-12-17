#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old FullSerializer folder..."
rm --recursive --force FullSerializer
mkdir FullSerializer
cd FullSerializer

echo "Cloning FullSerializer from remote..."
git init
git remote add origin https://github.com/jacobdufault/fullserializer.git
git config core.sparsecheckout true
echo "Assets/FullSerializer/Source/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# commit from 30 Mar 2017: c01db302f337205696585daa72e7d7baea922e44
git checkout c01db302f337205696585daa72e7d7baea922e44

echo "Moving downloaded files to correct position for this project..."
mv --verbose Assets/FullSerializer/* ./
rm --recursive ./Assets

cd "$old_dir"
echo "Downloading FullSerializer done"
