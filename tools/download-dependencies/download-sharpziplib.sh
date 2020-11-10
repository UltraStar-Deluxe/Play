#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old SharpZipLib folder..."
rm -rf SharpZipLib
mkdir SharpZipLib
cd SharpZipLib

echo "Cloning SharpZipLib from remote..."
git init
git remote add origin https://github.com/icsharpcode/SharpZipLib.git
git config core.sparsecheckout true
echo "LICENSE.txt" >> .git/info/sparse-checkout
echo "src/ICSharpCode.SharpZipLib/Core/*" >> .git/info/sparse-checkout
echo "src/ICSharpCode.SharpZipLib/Checksum/*" >> .git/info/sparse-checkout
echo "src/ICSharpCode.SharpZipLib/Encryption/*" >> .git/info/sparse-checkout
echo "src/ICSharpCode.SharpZipLib/BZip2/*" >> .git/info/sparse-checkout
echo "src/ICSharpCode.SharpZipLib/Zip/*" >> .git/info/sparse-checkout
echo "src/ICSharpCode.SharpZipLib/Tar/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 19 October 2020: d0efee019d5c1b21f8c6957cb6742d28fc60eef7
git checkout d0efee019d5c1b21f8c6957cb6742d28fc60eef7

echo "Moving downloaded files to correct position for this project..."
mv -v src/* ./
rm -rf ./src
rm -rf .git

cd "$old_dir"
echo "Downloading SharpZipLib done"
echo ""
