old_dir=$(pwd)

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
