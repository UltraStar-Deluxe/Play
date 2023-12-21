#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets"
echo "Removing old OneJS folder..."
rm -rf OneJS
mkdir OneJS
cd OneJS

echo "Cloning OneJS from remote..."
git init
git remote add origin https://github.com/achimmihca/OneJsRuntimeLoadedStyleSheets.git
git config core.sparsecheckout true
echo "Assets/OneJS/*" >> .git/info/sparse-checkout
echo "Assets/OneJS.meta" >> .git/info/sparse-checkout
# commit of 05 September 2023: 8259391b6bbdbd6a445515151bf4aae87e0ba57b
git pull --depth=100 origin main
git checkout 8259391b6bbdbd6a445515151bf4aae87e0ba57b

echo "Moving downloaded files to correct position for this project..."
mv -v ./Assets/OneJS/* ./
mv -v ./Assets/OneJS.meta ./
rm -rf ./Assets
rm -rf .git

cd "$old_dir"
echo "Downloading OneJS done"
echo ""
