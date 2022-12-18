#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old Vosk folder..."
rm -rf Vosk
mkdir Vosk
cd Vosk

echo "Cloning Vosk from remote..."
git init
git remote add origin https://github.com/alphacep/vosk-unity-asr.git
git config core.sparsecheckout true
echo "Assets/ThirdParty/Vosk/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 26 November 2021: 31439ea2cd6f841820ee140e42b3d013186bb41b
git checkout 31439ea2cd6f841820ee140e42b3d013186bb41b

echo "Moving downloaded files to correct position for this project..."
mv -v Assets/ThirdParty/Vosk/* ./
rm -rf ./Assets
rm -rf .git

echo "Create assembly definition (asmdef)"
echo "{ \"name\": \"Vosk\", \"includePlatforms\": [], \"references\": [] }" >  "./Vosk.asmdef"

cd "$old_dir"
echo "Downloading Vosk done"
echo ""
