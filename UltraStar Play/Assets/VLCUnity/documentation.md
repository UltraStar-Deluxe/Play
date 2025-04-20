Welcome to VLC for Unity on Windows!

## Docs reference

See the [LibVLCSharp documentation](https://code.videolan.org/videolan/LibVLCSharp/-/blob/master/docs/home.md).

It includes [best practices](https://code.videolan.org/videolan/LibVLCSharp/blob/master/docs/best_practices.md), [Q&A guide](https://code.videolan.org/videolan/LibVLCSharp/blob/master/docs/how_do_I_do_X.md), [libvlc specific information](https://code.videolan.org/videolan/LibVLCSharp/blob/master/docs/libvlc_documentation.md) and [tutorials](https://code.videolan.org/videolan/LibVLCSharp/blob/master/docs/tutorials.md).

## Components included

For reference, you need to a bunch of components to get this working. On Windows, for example:
- libvlc.dll, libvlccore.dll (and its plugins in /plugins folder): These are nightly build DLLs of the VLC player libraries https://code.videolan.org/videolan/vlc
- Custom build of libvlcsharp, the official VideoLAN C# binding to libvlc https://code.videolan.org/videolan/LibVLCSharp
- VLCUnityPlugin.dll, the VLC-Unity native plugin https://code.videolan.org/videolan/vlc-unity

This is all included in this package and it all works automatically for you.

LibVLCSharp docs (not Unity specific) https://code.videolan.org/videolan/LibVLCSharp/blob/master/docs/getting_started.md

## Windows

!! You need to set your Unity target platform to "PC, Mac & Linux Standalone" to target Windows classic. Go for the x86_64 architecture.

## Android

For the Unity Android target, we support:
- armeabi-v7a,
- arm64-v8a,
- x86,
- x86_64.

/!\ OpenGL ES MUST be selected in the project settings as a target graphics API. Vulkan is NOT supported at this time.

/!\ If the plugin complains about missing binaries and there is an error about this, make sure you have each architecture properly setup in the inspector for each binaries in each VLCUnity/Plugins/Android/libs folders. Select a libvlc.so in any given folder, check the Inspector window of the Unity Editor and make sure the selected "Platform" and "CPU" are correct.

/!\ If the scene starts but no video plays, make sure you set internet access to "required" in Unity player settings. The demo scenes play HTTP videos so your app needs the Android Internet permission.

/!\ If running on Oculus, you may need to disable "Low Overhead Mode" which is not yet supported by vlc-unity. https://code.videolan.org/videolan/vlc-unity/-/issues/164

Reminded: If you want to target arm64-v8a CPU architecture, you must select it in the player settings. To be able to select it, you need to switch the scripting backend to IL2CPP (as opposed to Mono). This is a Unity requirement unrelated to libvlc.

VLC for Unity requires Android 16 minimum.

## UWP

For the Unity UWP target, we support:
- x86_64,
- ARM64.

If you need 32 bit versions, feel free to email us with information regarding your use case at unity@videolabs.io

> In the publisher manifest, make sure the 'InternetClient' capability is enabled so that VLC can access remote streams.

For Hololens support and HTTPS, be aware that the Hololens device has very few SSL certificate by default. This means some HTTPS streams may not work since gnutls cannot find the required certificate. You have 2 options:
1. Install the certificat globally on the device. This is a viable option only if you own the distribution (e.g. can install the required certificate on all client's Hololens devices). This guide should prove helpful: https://learn.microsoft.com/en-us/hololens/certificate-manager
2. Ship your game with the required certificate and tell LibVLC to load using `--gnutls-dir-trust=PATH_TO_CERT_FOLDER` where `PATH_TO_CERT_FOLDER` is a folder inside your appx that contains the cert.

You can export the global certificate from your Windows machine using the certlm.exe app. Look for `GlobalSign Root CA` and export the .per.

## iOS

For the initial release, only ARM64 device builds are provided. Simulator support is not yet included.

It is possible to test things via the Editor on macOS beforehand though, on both Apple Silicon and Apple Intel macOS. And for Apple Silicon users, iOS apps can be ran on the mac.

> For Apple validation errors regarding OS Minimal version of the plugin when pushing to the AppStore, see this issue for a solution: https://code.videolan.org/videolan/vlc-unity/-/issues/227

## macOS

Both Apple Silicon (ARM64) and Intel Macs builds are supported, in Editor and through XCode.

The following build scenario is currently unsupported for the beta release:
- Universal builds (binaries with both Intel64 and Apple Silicon binaries) currently are not supported nor tested.

## General

The scenes are located in `Assets/VLCUnity/Demos/Scenes` and provide a way to get started quickly. 

Select any scene (*.unity) and press play in the Unity Editor (or make a standalone build), and the video will start playing.

## Getting started with the minimal sample script 

The following information is to give you more context regarding what happens in the Demo scenes provided in the Asset.
Understanding the scripts will allow you to know where to look when customizing your own player.

Regarding the basic integration code that you need, you will find it in MinimalPlayback.cs.

First, you need to load the native libraries (libvlc):
```
Core.Initialize(UnityEngine.Application.dataPath);
```

You can then create LibVLCSharp objects like so:
```
LibVLC = new LibVLC();
MediaPlayer = new MediaPlayer(LibVLC);
```

The frame updating is done in a Unity coroutine or Update() function:
```
IntPtr texptr = MediaPlayer.GetFrame(width, height, out bool updated);
if (updated)
{
    tex.UpdateExternalTexture(texptr);
}
```

See Update() function for more details.

Once that is all setup, you can create a new Media and start playback like so
```
MediaPlayer.Play(new Media(new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4")));
```

To get up and running easily and quickly, I recommend you to load the Assets/VLCUnity/Demos/Scenes/MinimalPlayback.unity scene and press "Play". 
The few lines of setup code I mentioned above are located in Assets/VLCUnity/Demos/Scripts/MinimalPlayback.cs.

## Scenes

- A minimal playback example with buttons,
- 360 playback with keyboard navigation built-in,
- A video with subtitles showcasing support,
- The VLCPlayerExample provides a great base with more controls,
- 3D scene you can move around in with a movie screen and chairs in a cinema room.

For more API usage information, explore our [online docs](https://code.videolan.org/videolan/LibVLCSharp/-/blob/master/docs/home.md).

For VLC Unity specific questions and support, open an issue on our GitLab and browse our opensource plugin code at https://code.videolan.org/videolan/vlc-unity

We also provide support through StackOverflow, you may browse the [libvlcsharp](https://stackoverflow.com/questions/tagged/libvlcsharp) and [vlc-unity](https://stackoverflow.com/questions/tagged/vlc-unity) tags.

## Misc

/!\ There seems to be issues when using VLC Unity and MRTK in the same Unity project. Both LibVLCSharp and MRTK rely on System.Numerics.Vectors and this triggers an issue with IL2CPP. 
Removing one of the System.Numerics.Vectors.dll in your project seems to be a valid workaround (but be aware of versioning mismatch).

## Asset import management

All releases include binaries for all platforms.

The free trial contains binaries that are watermarked on all platforms.

The Windows paid version contains binaries that are watermarked on all platforms, _except_ on Windows. The iOS paid version contains binaries that are watermarked on all platforms, _except_ iOS. And so on...

VLC Unity is provided as standalone .unitypackage files, that you can import using the Unity Editor. 

In the scenario that you have bought multiple plugins, let's say Android and Windows, you want to have no watermark on Android and Windows inside your single project.
This can be achieved by selectively importing binaries of the plugins during the import process. Let's walk through the necessary steps:

1. So first, import the Windows asset for example, but untick the Android specific binaries. All binaries will be included in your project except for the Android ones from the Windows asset (which are watermarked).
2. Then, import the Android asset, and untick all binaries except for the Android ones (which are watermark-free).

This way you will have imported your paid, watermark-free binaries for Windows and Android into your project but still retain the free, watermark binaries for all other platforms, such as UWP, macOS and iOS.