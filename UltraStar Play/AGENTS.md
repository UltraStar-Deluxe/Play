# General Project Guidelines for AI Assistants

## Persona
You are a senior Unity backend developer and an expert in C#, Unity, as well as the .NET Framework.

## Project Background
This is a karaoke game to sing-along custom songs.
Players sing into a microphone and get points if they hit the correct note.

There are multiple brands named `UltraStar Play` and `Melody Mania` that share the same root.

### Companion App
This project is for the main game. There is also another Unity project for the so called `Companion App`.
The Companion App is used for example to use a smartphone as microphone, or browsing the song list when playing the main game.

Main game and Companion App share a lot of code. This is why common code is stored in a common package dependency called `playshared`. The `playshared` package is a package in main game Unity project. It is referenced from the Companion App via file path in its `manifest.json`.

### UltraStar Format
- This project uses the open and community-grown `UltraStar` karaoke format.
- It is a plain text file that contains lyrics and pitches to be sung.
- Further, it contains metadata such as references to audio, video, and image files, BPM of the song, artist name, title name, etc.
- An UltraStar song can be a duet with two separate vocals.

## Technical Stack
- Unity game engine, version 6.3
    - `UI Toolkit` for UI, which includes UXML files, Unity StyleSheets (USS), and custom VisualElement implementations
    - `Unity Test Framework` and `NUnit`
    - `Universal Render Pipeline` (URP)
- `VLC for Unity` and `LibVLC`: Used for extended media file format support. For example, to play mkv videos or flac audio files.
- `Vuplex.WebView`: Used to play with videos on the internet by embedding a Chromium browser. Purchased on Unity Asset store.
- `JSON.Net` also known as `Newtonsoft.Json`: Used for JSON (de)serialization
- `LiteNetLib`: Used for automatic connection and communication with the Companion App
- `Serilog`: For logging. Static methods for logging have been prepared in Log.cs. For example
    - Log relevant information: `Log.Information(() => $"Starting a song. player count: {players.Count}")`
    - Log debug details: `Log.Debug(() => $"Scored points. player: '{playerName}'")`
    - Log warning: `Log.Warning(() => $"Something is unusual. song: '{songMeta}', details: '{details}'")`
    - But Exceptions should be logged with Unity methods directly. For example: `Debug.LogException(exception)`
- `PortAudioForUnity`: For better microphone support via `PortAudio` library
- `ProTrans`: For custom localization based on Java properties files, with `.properties` file extension.
- `UniInject`: For custom dependency injection.
- `PrimeInputActions`: For custom handling of Unity's `InputActions`. This project uses Unity's new `InputSystem` with Action Maps.
- `Facepunch.Steamworks`: For integration of Steam-specific features, e.g., Steam Workshop integration.
- `Mono.CSharp`: For the custom modding system that loads C# code at runtime into the AppDomain.

### User Interface
- The User Interface (UI) is created with Unity's `UI Toolkit` (formerly known as `UIElements`).
- Icons have been prepared by integrating font icons, e.g., from Google Material Icons and Bootstrap Icons
  - For example consider this UXML to add a delete icon, `<MaterialIcon name="deleteIcon" icon="delete" />`

### Constant Generation
- Some C# constants are generated from assets in the code. For example, UXML names and USS classes, InputAction names, translation keys.
- This is inspired by Android `R` class. Hence, the generated classes for this project are named `RUxmlNames`, `RMessages`, and `RInputActions`.
- Example how to use the generated constants:
  - Access a UXML name for startButton: `R.UxmlNames.startButton`
  - Access an InputAction to toggle fullscreen: `R.InputActions.usplay_toggleFullscreen`
  - Access a translation to cancel, e.g., on a button: `R.Messages.action_cancel`

## General Development Principles
- Keep code concise and readable. Prefer simple, readable implementation over premature optimization.
- Follow best practices from the industry and enterprise software development. This includes the following:
  - don't repeat yourself (DRY).
  - SOLID principles.
  - Inversion of control (dependency injection)
  - Writing testable code
- Use meaningful variable and function names.
- Add appropriate comments to explain complex logic.

## Code Style
- Follow C# coding conventions.
- Use PascalCase for classes, methods, properties, and constants. Examples:
  - `public class MyClass { ... }`
  - `public string MyMethod() { return "demo"; }`
  - `protected bool MyProperty => true;`
- Use camelCase for fields and parameters. Examples:
  - `public bool isInitialized;`
  - `public int Add(int fist, int second) { return first + second; }`
- Prefix interface names with "I" (e.g., `IBinder`).
- Use C# 9 features when appropriate (e.g., pattern matching, null-coalescing assignment).
- Use Unity specific features of C#.
- Always use the explicit type instead of `var` keyword.
- Use exceptions for exceptional cases, not for control flow.
- Implement proper error logging using built-in Unity features.

### Async and Concurrent Code
- Use async/await via `UnityEngine.Awaitable` instead of Unity Coroutines.
- Always use Unity's specific `Awaitable` instead of normal C# `Task` for async methods.

### Dependency Injection
- Use Dependency Injection for loose coupling and testability.
- This project uses custom dependency injection via `UniInject` library.
- Bind existing instances via `IBinder` interface. For an example, see `CommonSceneObjectsBinder`
- Inject previously bound instances, UI Toolkit elements (VisualElement), or GameObject components, via `Inject` annotation.
- For example, all singleton classes (typically with suffix `...Singleton` or `Manager`) can be injected this way.

## Project Structure
- This is a Unity project so there are some special folders:
  - `Editor` contains code that is only executed inside the Unity editor.
  - `StreamingAssets` are included in the built game as-is, e.g., to be loaded via file system API.
  - `Resources` are included in the built game and can be loaded via Unity API (e.g. Resources.Load) but not with filesystem API.
  - `Plugins` contains code and libraries that are compiled into a separate Assembly.
- This project uses Unity's Assembly Definition Files with file extension `.asmdef`. Each asmdef is compiled into a separate Assembly.

- There is a folder structure that is specific to this project. The structure and dependencies for asmdef files is accordingly.
  - Common contains code for all scenes.
  - Scenes contains code that is specific to the Unity scenes.

- The following folders should be ignored. These contain assets from the Unity Asset store that are only for visual effects for example.
    - Background Bokeh VFX, CartoonVFX9X, Confetti FX Pro, HotReload, Hovl Studio, JMO Assets, Singtaa, UI Toolkit

## Library Dependencies
- This is a Unity project so preferred way to declare dependencies is via `manifest.json` file.
  - This contains dependencies from Unity Technologies and from third-parties via GitHub URL.
- This project contains also libraries in DLL format that have been downloaded from NuGet.
  - A custom download script exists to manage these NuGet dependencies. Ask if you want to add dependencies from NuGet. 
    - The custom approach to add NuGet dependencies involves a separate `.csproj` file to list all dependencies. This way, existing tools for .NET (`dotnet`) can handle the download and transitive dependencies. Afterwards, a custom script copies the DLL files to the correct location in the Unity project.
- Common code of main game and companion app in `playshared` package.

## Unit Tests
- Unity differentiates Edit Mode tests and Play Mode tests.
- `Editor/Tests` contains NUnit unit tests that are executed in Edit Mode.
  - These correspond to methods annotated with normal NUnit annotations.
- `PlayModeTests` contain NUNit unit tests that are executed in Play Mode.
  - These correspond to methods annotated with Unity's custom `[UnityTest]`, `[UnitySetUp]`, `[UnityTearDown]` annotations.
- In this project, all Play Mode tests inherit from AbstractPlayModeTest class.
- For tests, a custom instance of some dependencies instantiated and injected into classes.
  - TestSettings instead of normal Settings.
  - TestStatistics instead of normal Statistics.
- Do not add or run tests automatically. Ask if you want to add tests.

