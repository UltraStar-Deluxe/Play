#-------------------------------------------
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
git pull --depth=1 origin master

echo "Moving downloaded files to correct position for this project..."
mv --verbose ./Assets/LeanTween/* ./
rm --recursive ./Assets

cd ..
echo "Downloading LeanTween done"
#-------------------------------------------