The JavaScript files in this folder are for the embedded browser
to integrate third-party websites in the game.

## File Name
- The file name must be the host of the URL that is handled by the script.
    - Example: To handle a URL `https://my-video-platform.com/?v=SomeVideoId`, the script should be named `my-video-platform.com.js`
- After adding a script, the game will assume that the corresponding host is supported.
    - Example: Adding a file `my-video-platform.com.js` will allow you to use the URL `https://my-video-platform.com/?v=SomeVideoId` (with or without `www.`) as #MP3 tag in song files.

## Loading Order
- The scripts in this folder are loaded after the page is loaded.

## Show Embedded Browser
- You can show / hide the embedded browser window by pressing `F8` or `Ctrl+B`.

## Open URL
- The game will load a URL into the embedded browser when the song file should play its audio.
- A script can indicate that it is able to load a new URL on the same host by itself.
  The game will then delegate loading a different URL to the script instead of navigating to a new URL.

## Log Messages
- The `console.log` statements in the script are redirected to the game.
- You can see the log messages in the game by looking at the log file or by opening the in-game debug console that opens with `F7`.

## Reloading Scripts
- A file system watcher is monitoring the scripts in this folder. If a script is changed, then the game will reload the script without requiring a restart.