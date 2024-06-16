#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Packages/playshared/Runtime/Plugins"
echo "Removing old LiteNetLib folder..."
rm -rf LiteNetLib
mkdir LiteNetLib
cd LiteNetLib

echo "Cloning LiteNetLib from remote..."
git init
git remote add origin https://github.com/RevenantX/LiteNetLib.git
git config core.sparsecheckout true
echo "LiteNetLib/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 24 April 2023: 47d57213baa96d4b343741c6ff3c2eedde961d46
git checkout 47d57213baa96d4b343741c6ff3c2eedde961d46

echo "Moving downloaded files to correct position for this project..."
mv -v LiteNetLib/* ./
rm -rf ./LiteNetLib
rm -rf .git

cd "$old_dir"
echo "Downloading LiteNetLib done"
echo ""
