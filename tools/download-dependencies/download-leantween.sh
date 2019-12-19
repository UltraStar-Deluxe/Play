#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old LeanTween folder..."
rm --recursive --force LeanTween
mkdir LeanTween
cd LeanTween

echo "Cloning LeanTween from remote..."
git init
git remote add origin https://github.com/dentedpixel/LeanTween.git
git config core.sparsecheckout true
echo "Assets/LeanTween/Framework/*" >> .git/info/sparse-checkout
echo "Assets/LeanTween/Editor/*" >> .git/info/sparse-checkout
echo "Assets/LeanTween/Documentation/*" >> .git/info/sparse-checkout
echo "Assets/LeanTween/License.txt" >> .git/info/sparse-checkout
echo "Assets/LeanTween/ReadMe.txt" >> .git/info/sparse-checkout
# commit of 10th November 2018: f387a18039f1eae62ab3e0a706c3e1002a4dcb22
git pull --depth=100 origin master
git checkout f387a18039f1eae62ab3e0a706c3e1002a4dcb22

echo "Moving downloaded files to correct position for this project..."
mv --verbose ./Assets/LeanTween/* ./
rm --recursive ./Assets

cd "$old_dir"
echo "Downloading LeanTween done"
echo ""
