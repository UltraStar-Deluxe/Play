#!/bin/sh

old_dir=$(pwd)

cd "../../UltraStar Play/Assets/Plugins"
echo "Removing old Soundfonts folder..."
rm -rf Soundfonts
mkdir Soundfonts
cd Soundfonts

echo "Downloading soundfonts..."
wget -O MuseScore_General.sf2.bytes https://ftp.osuosl.org/pub/musescore/soundfont/MuseScore_General/MuseScore_General.sf2

cd "$old_dir"
echo "Downloading Soundfonts done"
echo ""

