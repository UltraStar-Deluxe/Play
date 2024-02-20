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
# Commit from 10 Nov 2023: 4bac23ea37d778000bfaea5e3a79a7ea93b28d8e
git checkout 4bac23ea37d778000bfaea5e3a79a7ea93b28d8e

echo "Moving downloaded files to correct position for this project..."
mv -v Songs/* ./
rm -rf ./Songs
rm -rf .git

cd "$old_dir"
echo "Downloading demo songs done"
echo ""
