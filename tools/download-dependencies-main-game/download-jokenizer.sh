#!/bin/sh

old_dir=$(pwd)

# Jokenizer is a dependency of DynamicQueryable from the same author. Jokenizer is for parsing expressions.
cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old Jokenizer.Net folder..."
rm -rf Jokenizer.Net
mkdir Jokenizer.Net
cd Jokenizer.Net

echo "Cloning Jokenizer.Net from remote..."
git init
git remote add origin https://github.com/umutozel/Jokenizer.Net.git
git config core.sparsecheckout true
echo "src/Jokenizer.Net/*" >> .git/info/sparse-checkout
# Commit from 10 Feb 2020: 8fb6452eea609f5de46911be73d982056080e658
git pull --depth=1 origin 8fb6452eea609f5de46911be73d982056080e658

echo "Moving downloaded files to correct position for this project..."
mv -v src/Jokenizer.Net/* ./
rm -rf ./src
rm -rf .git

# Remove Properties/AssemblyInfo.cs
rm -rf Properties

cd "$old_dir"
echo "Downloading Jokenizer.Net done"
echo ""
