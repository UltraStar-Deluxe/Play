#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets"
echo "Removing old ThirdPartyAssets..."
rm -rf "Background Bokeh VFX"
mkdir "Background Bokeh VFX"

rm -rf "CartoonVFX9X"
mkdir "CartoonVFX9X"

rm -rf "Confetti FX Pro"
mkdir "Confetti FX Pro"

rm -rf "Hovl Studio"
mkdir "Hovl Studio"

rm -rf "JMO Assets"
mkdir "JMO Assets"

rm -rf ThirdPartyAssets
mkdir ThirdPartyAssets
cd ThirdPartyAssets

echo "Cloning ThirdPartyAssets from remote..."
git init
git remote add origin https://github.com/achimmihca/MelodyManiaThirdPartyAssets
git config core.sparsecheckout true
echo Assets/* >> .git/info/sparse-checkout
git pull --depth=100 origin main
# Commit from 28 March 2023: ec47fc679c35eea58aaecdb284c24fb9a3e81ccc
git checkout ec47fc679c35eea58aaecdb284c24fb9a3e81ccc

echo "Moving downloaded files to correct position for this project..."
mv -v "Assets/Background Bokeh VFX" "../"
mv -v "Assets/CartoonVFX9X" "../"
mv -v "Assets/Confetti FX Pro" "../"
mv -v "Assets/Hovl Studio" "../"
mv -v "Assets/JMO Assets" "../"
cd ..
rm -rf ThirdPartyAssets

cd "$old_dir"
echo "Downloading ThirdPartyAssets done"
echo ""
