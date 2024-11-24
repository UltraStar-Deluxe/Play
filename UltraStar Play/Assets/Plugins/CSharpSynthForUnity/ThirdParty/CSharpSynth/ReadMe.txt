C# Digital Audio Synth 
Version 1.2
Copyright Alex Veltsistas 2014  

To view the source open the 'Source' folder. Additionally there is a demo called DirectSoundDemo which should help you get started.
If you want to begin using the library in your own project the file AudioSynthesis.dll has already been built in release mode and is ready to be referenced.


Whats New

- Moved project to visual studio 2013
	- converted solution to portable library
	- resources now handled indirectly via streams (IResource interface, see MyFile.cs for sample implementation)

- Fixed midi parameters
	- hold pedal issue resolved (hold pedal now only effects the channel its on instead of all channels)
	- added parameter for legato pedal (for future use, I havent decided how to implement it yet)
	- volume cc is no longer realtime (this follows gm spec)
	- fixed reset cc message (before it reset some controllers that were not supposed to be)
	- fixed issue with loading midis with invalid tracks

- Fixed SF2
	- added support for some initial modulators (mostly volume)
	- fixed issue with initial attenuation which caused some instruments to play very loud
	- fixed issue with volume envelope (it now operates directly in dB)

- Other tweaks and fixes
	- sample assets are now stored at their native bit depth to save memory (eg. 16bit audio stored using 16bits instead of 32)
	- removed various allocations in voice processing loop