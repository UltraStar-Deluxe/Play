#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old SpleeterSharp folder..."
rm -rf SpleeterSharp
mkdir SpleeterSharp
cd SpleeterSharp

echo "Cloning SpleeterSharp from remote..."
git init
git remote add origin https://github.com/achimmihca/SpleeterSharp
git config core.sparsecheckout true
echo Source/SpleeterSharp/* >> .git/info/sparse-checkout
git pull --depth=100 origin anst/SpleeterMsvcExe
# Commit from 08 May 2023: dac1900eb7bce4e637a06eb18693fe246d4e843e
git checkout dac1900eb7bce4e637a06eb18693fe246d4e843e

echo "Moving downloaded files to correct position for this project..."
mv -v Source/SpleeterSharp/* ./
rm -rf Source
rm -rf .git

echo "Create assembly definition (asmdef)"
echo "{ \"name\": \"SpleeterSharp\", \"includePlatforms\": [], \"references\": [] }" >  "./SpleeterSharp.asmdef"

cd "$old_dir"
echo "Downloading SpleeterSharp done"
echo ""
