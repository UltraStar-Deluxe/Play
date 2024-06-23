#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old NHyphenator folder..."
rm -rf NHyphenator
mkdir NHyphenator
cd NHyphenator

echo "Cloning NHyphenator from remote..."
git init
git remote add origin https://github.com/alkozko/NHyphenator.git
git config core.sparsecheckout true
echo "NHyphenator/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 15 May 2018: a10a8422a53552e7870e41f6a1e751e9ae65956c
git checkout a10a8422a53552e7870e41f6a1e751e9ae65956c

echo "Moving downloaded files to correct position for this project..."
mv -v NHyphenator/* ./
rm -rf ./NHyphenator
rm -rf .git
yphenator\", \"includePlatforms\": [], \"references\": [] }" >  "./NHyphenator.asmdef"

cd "$old_dir"
echo "Downloading NHyphenator done"
echo ""
