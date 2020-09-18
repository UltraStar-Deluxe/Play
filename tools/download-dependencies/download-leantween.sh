#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old LeanTween folder..."
rm -rf LeanTween
mkdir LeanTween
cd LeanTween

echo "Cloning LeanTween from remote..."
git init
git remote add origin https://github.com/achimmihca/LeanTween.git
git config core.sparsecheckout true
echo "Assets/LeanTween/Framework/*" >> .git/info/sparse-checkout
echo "Assets/LeanTween/Editor/*" >> .git/info/sparse-checkout
echo "Assets/LeanTween/Documentation/*" >> .git/info/sparse-checkout
echo "Assets/LeanTween/License.txt" >> .git/info/sparse-checkout
echo "Assets/LeanTween/ReadMe.txt" >> .git/info/sparse-checkout
# commit of 18th September 2020: ea745c3f94d8682327c912030dfc6b65cbe1ced5
git pull --depth=100 origin master
git checkout ea745c3f94d8682327c912030dfc6b65cbe1ced5

echo "Moving downloaded files to correct position for this project..."
mv -v ./Assets/LeanTween/* ./
rm -rf ./Assets

cd "$old_dir"
echo "Downloading LeanTween done"
echo ""
