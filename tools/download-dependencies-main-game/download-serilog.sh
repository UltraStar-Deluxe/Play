#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins/Serilog"
echo "Removing old Serilog (main) folder..."
rm -rf Serilog
mkdir Serilog
cd Serilog

echo "Cloning Serilog from remote..."
git init
git remote add origin https://github.com/serilog/serilog.git
git config core.sparsecheckout true
echo "LICENSE" >> .git/info/sparse-checkout
echo "src/Serilog/*" >> .git/info/sparse-checkout
git pull --depth=500 origin main
# Commit from 13 October 2019 (release 2.9.0): 655778f74384f682d2c8705ab4883c39ef17e44d
git checkout 655778f74384f682d2c8705ab4883c39ef17e44d

echo "Moving downloaded files to correct position for this project..."
mv -v src/Serilog/* ./
rm -rf ./src
rm -rf .git
rm Properties/AssemblyInfo.cs

cd "$old_dir"
echo "Downloading Serilog (main) done"
echo ""

#######################################################################
old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins/Serilog"
echo "Removing old Serilog.Sinks.File folder..."
rm -rf Serilog.Sinks.File
mkdir Serilog.Sinks.File
cd Serilog.Sinks.File

echo "Cloning Serilog.Sinks.File from remote..."
git init
git remote add origin https://github.com/serilog/serilog-sinks-file.git
git config core.sparsecheckout true
echo "LICENSE" >> .git/info/sparse-checkout
echo "src/Serilog.Sinks.File/*" >> .git/info/sparse-checkout
git pull --depth=500 origin main
# Commit from 17 October 2019 (release 4.1.0): 272085f4c9440e62448b65829ad35cc3dea15ab1
git checkout 272085f4c9440e62448b65829ad35cc3dea15ab1

echo "Moving downloaded files to correct position for this project..."
mv -v src/Serilog.Sinks.File/* ./
rm -rf ./src
rm -rf .git
rm Properties/AssemblyInfo.cs

cd "$old_dir"
echo "Downloading Serilog.Sinks.File done"
echo ""
