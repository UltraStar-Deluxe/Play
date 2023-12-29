#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old DynamicQueryable folder..."
rm -rf DynamicQueryable
mkdir DynamicQueryable
cd DynamicQueryable

echo "Cloning DynamicQueryable from remote..."
git init
git remote add origin https://github.com/umutozel/DynamicQueryable.git
git config core.sparsecheckout true
echo "src/DynamicQueryable/*" >> .git/info/sparse-checkout
# Commit from 19 Nov 2020: c4832d4a69127074699b8d4a6c8d897fff644bdb
git pull --depth=1 origin c4832d4a69127074699b8d4a6c8d897fff644bdb

echo "Moving downloaded files to correct position for this project..."
mv -v src/DynamicQueryable/* ./
rm -rf ./src
rm -rf .git

# Remove Properties/AssemblyInfo.cs
rm -rf Properties

cd "$old_dir"
echo "Downloading DynamicQueryable done"
echo ""
