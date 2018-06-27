# UltraStar Play Development README

[![Travis Build Status](https://travis-ci.org/UltraStar-Deluxe/Play.svg?branch=master)](https://travis-ci.org/UltraStar-Deluxe/Play)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/5eeefc3773e8405aac7332ce0e57ec86)](https://www.codacy.com/app/UltraStar-Deluxe/Play?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=UltraStar-Deluxe/Play&amp;utm_campaign=Badge_Grade)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/UltraStar-Deluxe/Play/blob/master/LICENSE) 
[![Join the chat at https://gitter.im/UltraStar-Deluxe/Play](https://badges.gitter.im/UltraStar-Deluxe/Play.svg)](https://gitter.im/UltraStar-Deluxe/Play?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### 1. About
UltraStar Play is a free and open source karaoke singing game for Windows, Linux, Android, Xbox, PlayStation and other modern platforms. The game plays the audio file, displays singing lyrics, notes and optionally a background video or picture, while the singers sing the songs and thus try to hit the notes to get points depending on how close they get to the perfect pitches.
The game uses the Unity 2D framework and mostly c# (mono/.Net) as software development language.

### 2. Minimal Game Requirements
- Windows Vista SP1 or Android OS 4.1 or iOS 7.0 or Mac OS X 10.9, Ubuntu 12.04, SteamOS or any newer
- DirectX 10 with Shader Model 4.0 or OpenGL ES 2.0 or any newer
- speakers, USB microphones (or similar), big screen
- plenty gigabytes of storage space for songs, videos and other game content - varies depending on quantity and quality

### 3. Development Requirements
- Windows 7 SP1+, 8, 10, only 64-bit; Mac OS X 10.9+.
- DX10 with Shader Model 4.0+
- plenty of free time
- see [How to Compile the Game](https://github.com/UltraStar-Deluxe/Play/wiki/Compiling-the-game)

### 4. Documentation
see [the project wiki](https://github.com/UltraStar-Deluxe/Play/wiki)

### 5. How to contribute
see [CONTRIBUTING.md](https://github.com/UltraStar-Deluxe/Play/blob/master/CONTRIBUTING.md) and also [How to Compile the Game](https://github.com/UltraStar-Deluxe/Play/wiki/Compiling-the-game)

### 6. Support
- if you just want to play sing-along karaoke, please use UltraStar Deluxe or Performous or Vocaluxe instead.
- see documentation sources mentioned above
- [Issue Tracker](https://github.com/UltraStar-Deluxe/Play/issues)
- "the code is documentation enough" << sorry, this project just started, there will be more places for support later on

### 7. Repository Folder Structure
The current folder structure is just a first draft, and you are encouraged to improve it, if you have extensive knowledge of / experience in open source unity games.

| Where | What |
|---|---|
| / | Main repo folder. Try to not add any new files here, but instead place them in a fitting subfolder |
| /tools/ | any build scripts, templates, helper stuff for devs, code checking stuff, lint templates |
| /UltraStar Play/ | Unity project |
| /UltraStar Play/Assets/Editor/ | unit tests or integration tests go here |
| /UltraStar Play/Assets/Materials/theme/ | theme content: image files, animations, click sounds, background music |
| /UltraStar Play/Assets/src/ | actual code of this project |
| ./src/audio/ | any audio input / output / pitch detection / microphone related code goes here |
| ./src/model/ | code related to the data model, static classes for songs-manager, players-manager, settings-manager |
| ./src/util/ |  rather generic utility code that is not specific to this karaoke game |
| ./src/view/ | all the screens/views and any code that is specific for these screens, currently also contains all the "in-game logic" |
