old_dir=$(pwd)


echo "Creating SharedRepo symlink"
echo "==========================="
echo ""
sh create-shared-repo-symlink.sh
cd "$old_dir"

if [ ! -d "../UltraStar Play Companion/Assets/Common/SharedRepo" ] 
then
    echo "SharedRepo folder was not created successfully. Aborting setup script."
    exit 1    
fi


echo "Downloading dependencies of main game"
echo "==========================="
echo ""
cd download-dependencies-main-game
sh download-dependencies-main-game.sh
cd "$old_dir"


echo "Downloading dependencies of companion app"
echo "==========================="
echo ""
cd download-dependencies-companion-app
sh download-dependencies-companion-app.sh
cd "$old_dir"
