#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old Unity-Ui-Extensions folder..."
rm -rf Unity-Ui-Extensions
mkdir Unity-Ui-Extensions
cd Unity-Ui-Extensions

echo "Cloning Unity-Ui-Extensions from remote..."
git init
git remote add origin https://bitbucket.org/UnityUIExtensions/unity-ui-extensions.git
git config core.sparsecheckout true
echo "/Scripts/Primitives/UILineRenderer.cs" >> .git/info/sparse-checkout
echo "/Scripts/Primitives/UIPrimitiveBase.cs" >> .git/info/sparse-checkout
echo "/Scripts/Utilities/BezierPath.cs" >> .git/info/sparse-checkout
echo "/Scripts/Utilities/CableCurve.cs" >> .git/info/sparse-checkout
echo "/Scripts/Utilities/SetPropertyUtility.cs" >> .git/info/sparse-checkout
echo "UnityUIExtensions.asmdef" >> .git/info/sparse-checkout
echo "LICENSE" >> .git/info/sparse-checkout
git pull --depth=20 origin master
# commit from 27 May 2020: dea069943554fd9e6f3954863756390a733139be
git checkout dea069943554fd9e6f3954863756390a733139be

cd "$old_dir"
echo "Downloading Unity-Ui-Extensions done"
