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
# Commit from 11 June 2023: 697bb1813b65738bea1b40673b49c144846c4f37
git checkout 697bb1813b65738bea1b40673b49c144846c4f37

echo "Moving downloaded files to correct position for this project..."
mv -v Songs/* ./
rm -rf ./Songs
rm -rf .git

cd "$old_dir"
echo "Downloading demo songs done"
echo ""
