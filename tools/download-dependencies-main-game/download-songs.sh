#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/StreamingAssets"
echo "Removing old demo songs folder..."
rm -rf DemoSongs
mkdir DemoSongs
cd DemoSongs

echo "Cloning demo songs from remote..."
git init
git remote add origin https://github.com/achimmihca/MelodyMania-Songs.git
git config core.sparsecheckout true
echo "Songs/*" >> .git/info/sparse-checkout
git pull --depth=100 origin main
# Commit from 9 June 2023: f5cc4d3a04345b9f080b391cec4c51f3b0654dbe
git checkout f5cc4d3a04345b9f080b391cec4c51f3b0654dbe

echo "Moving downloaded files to correct position for this project..."
mv -v Songs/* ./
rm -rf ./Songs
rm -rf .git

cd "$old_dir"
echo "Downloading demo songs done"
echo ""
