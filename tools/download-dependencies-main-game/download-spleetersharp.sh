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
# Commit from 18 July 2023: 3949952a7eef90c31c86eeb0f59fb103abd4138f
git checkout 3949952a7eef90c31c86eeb0f59fb103abd4138f

echo "Moving downloaded files to correct position for this project..."
mv -v Source/SpleeterSharp/* ./
rm -rf Source
rm -rf .git

echo "Create assembly definition (asmdef)"
echo "{ \"name\": \"SpleeterSharp\", \"includePlatforms\": [], \"references\": [] }" >  "./SpleeterSharp.asmdef"

cd "$old_dir"
echo "Downloading SpleeterSharp done"
echo ""
