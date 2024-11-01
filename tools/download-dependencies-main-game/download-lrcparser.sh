#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old Opportunity.LrcParser folder..."
rm -rf Opportunity.LrcParser
mkdir Opportunity.LrcParser
cd Opportunity.LrcParser

echo "Cloning Opportunity.LrcParser from remote..."
git init
git remote add origin https://github.com/OpportunityLiu/LrcParser.git
git config core.sparsecheckout true
echo "Opportunity.LrcParser/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 15 May 2018: a10a8422a53552e7870e41f6a1e751e9ae65956c
git checkout a10a8422a53552e7870e41f6a1e751e9ae65956c

echo "Moving downloaded files to correct position for this project..."
mv -v Opportunity.LrcParser/* ./
rm -rf ./Opportunity.LrcParser
rm -rf .git

#echo "Create assembly definition (asmdef)"
#echo "{ \"name\": \"Opportunity.LrcParser\", \"includePlatforms\": [], \"references\": [] }" >  "./Opportunity.LrcParser.asmdef"

cd "$old_dir"
echo "Downloading Opportunity.LrcParser done"
echo ""

