#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old UniInject folder..."
rm -rf UniInject
mkdir UniInject
cd UniInject

echo "Cloning UniInject from remote..."
git init
git remote add origin https://github.com/UltraStar-Deluxe/UniInject.git
git config core.sparsecheckout true
echo "UniInject/Assets/Scripts/*" >> .git/info/sparse-checkout
echo "UniInject/Assets/Editor/UniInjectEditor.asmdef" >> .git/info/sparse-checkout
echo "UniInject/Assets/Editor/MenuItems/*" >> .git/info/sparse-checkout
echo "README.md" >> .git/info/sparse-checkout
echo "LICENSE" >> .git/info/sparse-checkout
# commit from 3th January 2021: 8e8488834e799225bd48af1a50cdf00fad950bd6
git pull --depth=100 origin main
git checkout 8e8488834e799225bd48af1a50cdf00fad950bd6

echo "Moving downloaded files to correct position for this project..."
mv -v UniInject/Assets/* ./
rm -rf ./UniInject
rm -rf .git

cd "$old_dir"
echo "Downloading UniInject done"
echo ""
