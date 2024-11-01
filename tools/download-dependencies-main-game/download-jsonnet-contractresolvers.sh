#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Packages/playshared/Runtime/Plugins"
echo "Removing old JsonNet.ContractResolvers folder..."
rm -rf JsonNet.ContractResolvers
mkdir JsonNet.ContractResolvers
cd JsonNet.ContractResolvers

echo "Cloning JsonNet.ContractResolvers from remote..."
git init
git remote add origin https://github.com/danielwertheim/jsonnet-contractresolvers.git
git config core.sparsecheckout true
echo "src/main/JsonNet.ContractResolvers/*" >> .git/info/sparse-checkout
git pull --depth=100 origin master
# Commit from 28 March 2021: 91f03b4b302db94f695d98ba8fc9ed25efa616c5
git checkout 91f03b4b302db94f695d98ba8fc9ed25efa616c5

echo "Moving downloaded files to correct position for this project..."
mv -v src/main/JsonNet.ContractResolvers/* ./
rm -rf ./src
rm -rf .git

echo "Create assembly definition (asmdef)"
echo "{ \"name\": \"JsonNet.ContractResolvers\", \"includePlatforms\": [], \"references\": [] }" >  "./JsonNet.ContractResolvers.asmdef"

cd "$old_dir"
echo "Downloading JsonNet.ContractResolvers done"
echo ""

