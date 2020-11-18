#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old UniInject folder..."
rm -rf UniInject
mkdir UniInject
cd UniInject

echo "Cloning UniInject from remote..."
git init
git remote add origin https://github.com/achimmihca/UniInject.git
git config core.sparsecheckout true
echo "UniInject/Assets/Scripts/*" >> .git/info/sparse-checkout
echo "UniInject/Assets/Editor/UniInjectEditor.asmdef" >> .git/info/sparse-checkout
echo "UniInject/Assets/Editor/MenuItems/*" >> .git/info/sparse-checkout
echo "README.md" >> .git/info/sparse-checkout
echo "LICENSE" >> .git/info/sparse-checkout
# commit from 18th November 2020: b12f45828c081b03a6decaf8ca10058098f736f5
git pull --depth=100 origin main
git checkout b12f45828c081b03a6decaf8ca10058098f736f5

echo "Moving downloaded files to correct position for this project..."
mv -v UniInject/Assets/* ./
rm -rf ./UniInject
rm -rf .git

cd "$old_dir"
echo "Downloading UniInject done"
echo ""
