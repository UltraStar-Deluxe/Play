#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/StreamingAssets"
echo "Removing old SpleeterMsvcExe folder..."
rm -rf SpleeterMsvcExe
mkdir SpleeterMsvcExe
cd SpleeterMsvcExe

echo "Downloading SpleeterMsvcExe.zip..."
wget -O SpleeterMsvcExe.zip https://github.com/achimmihca/SpleeterMsvcExe/releases/download/v1.0/SpleeterMsvcExe-v1.0-2stems-only.zip

echo "Extracting SpleeterMsvcExe.zip..."
unzip SpleeterMsvcExe.zip

echo "Removing SpleeterMsvcExe.zip..."
rm SpleeterMsvcExe.zip

cd "$old_dir"
echo "Downloading SpleeterMsvcExe done"
echo ""
