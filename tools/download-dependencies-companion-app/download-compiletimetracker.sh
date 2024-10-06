#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play Companion/Assets/Plugins"
echo "Removing old CompileTimeTracker folder..."
rm -rf CompileTimeTracker
mkdir CompileTimeTracker
cd CompileTimeTracker

echo "Cloning CompileTimeTracker from remote..."
git init
git remote add origin https://github.com/DarrenTsung/DTCompileTimeTracker
git config core.sparsecheckout true
echo "CompileTimeTracker/*" >> .git/info/sparse-checkout
echo "README.md" >> .git/info/sparse-checkout
# commit from 1st October 2019: 276095b3b212d7c33106b53d71b93b5a72d1e1d3
git pull --depth=100 origin master
git checkout 276095b3b212d7c33106b53d71b93b5a72d1e1d3

echo "Moving downloaded files to correct position for this project..."
mv -v CompileTimeTracker/* ./
rm -rf ./CompileTimeTracker
rm -rf .git

echo "Creating assembly definition for editor scripts"
echo "{ \"name\": \"CompileTimeTrackerEditor\", \"includePlatforms\": [\"Editor\"], \"references\": [\"Plugins\"] }" >  "./Editor/CompileTimeTrackerEditor.asmdef"

cd "$old_dir"
echo "Downloading CompileTimeTracker done"
echo ""
