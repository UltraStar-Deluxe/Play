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
# Commit from 16 June 2023: ab6435f13e8bf0243d009a930d0f8444ea55c320
git checkout ab6435f13e8bf0243d009a930d0f8444ea55c320

echo "Moving downloaded files to correct position for this project..."
mv -v Songs/* ./
rm -rf ./Songs
rm -rf .git

cd "$old_dir"
echo "Downloading demo songs done"
echo ""
