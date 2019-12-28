# Contributing
First off, thank you for considering contributing to UltraStar Play. It's people like you that make UltraStar Play such a great karaoke game.
Following these guidelines helps to communicate that you respect the time of the developers managing and developing this open source project. In return, they should reciprocate that respect in addressing your issue, assessing changes, and helping you finalize your pull requests.

We are a free/libre open source project and we love to receive contributions from our community — you! There are many ways to contribute, from writing tutorials or blog posts, improving the documentation, submitting bug reports and feature requests or writing code which can be incorporated into the game itself.

Please, use the [issue tracker](https://github.com/UltraStar-Deluxe/Play/issues) to report feature requests and bugs.
If your problem is not strictly UltraStar Play specific, there are also a couple of forums out there regarding karaoke games.

## Open Development
All work on UltraStar Play happens directly on GitHub. Both core team members and external contributors send pull requests which go through the same review process.

## Branch Organization
We will do our best to keep the master branch in good shape, with tests passing at all times. But in order to move fast, we will make changes that your custom changes might not be compatible with. We recommend that you use the latest stable version of the game. Please use feature branches when working on more complex changes, so others can follow and contribute, too.

If you send a pull request, please do it against the master branch.

## How to suggest a feature or enhancement
This project just started! Please wait for the first few versions to be released, before requesting exciting new stuff! If you just want to play some sing-along karaoke, we suggest you to use the much more stable (but old and dusty) [UltraStar Deluxe](https://github.com/UltraStar-Deluxe/USDX/releases) game.

## Responsibilities

- Ensure cross-platform compatibility for every change that's accepted. Windows, Linux, Android, iOS, Xbox, Playstation.
- Ensure that code that goes into master branch meets all requirements from the requirements list below
- Create issues for any major changes and enhancements that you wish to make. Discuss things transparently and get community feedback.
- Don't add any classes to the codebase unless needed.
- Keep feature versions as small as possible, preferably one new major feature per version.
- Be welcoming to newcomers and encourage diverse new contributors from all backgrounds. See the [Contributor Covenant](https://www.contributor-covenant.org/) Community Code of Conduct.
- Take the time to get things right. Pull Requests (PR) almost always require additional improvements to meet the bar for quality. Be very strict about quality. This usually takes several commits on top of the original PR.
- Update documentation where necessary, write documentation when required. Use this repositories' wiki when targeting users / players of this game. Write green code or text files when targeting other developers.

## Requirements for code contributions to master branch
- Try to only contribute working code, no dead code, no "soon to be used" code and no "will fix it soon" code
- No huge methods, try to reduce complexity, write readable code -> see [Clean Code Cheat Sheet](https://www.bbv.ch/images/bbv/pdf/downloads/V2_Clean_Code_V3.pdf)
- When copying others peoples/projects code, check licenses

## Code Style 
The code style is configured in a file [`.editorconfig`](https://EditorConfig.org). Thus, when using Visual Studio Code (with OmniSharp) it should show warnings on code style violations. Furthermore, code formatting is configured to be done on save
(see */UltraStar Play/.vscode/settings.json*).

- Indent using 4 spaces
- All braces get their own line

- PascalCase: NameSpace, Class, IInterface, EEnum, Method, Property, Constant, Event
- only strictly typed variables
- camelCase: parameters, all fields

- prefixes: E for enums, I for interfaces, but none for private or static fields
- suffixes: "Scene" for scenes, "Controller" for the main class handling a scene (e.g. "MainScene.unity" has an associated "MainSceneController.cs")

- no acronyms, except for the above mentioned prefixes and common abbreviations, such as Http or Xml
- In code, acronyms should be treated as words. For example: `XmlHttpRequest`

- only use public where necessary
- avoid protected
- avoid static/readonly where possible (their values are lost on [Hotswap](https://gist.github.com/cobbpg/a74c8a5359554eb3daa5))


```
public class MySceneController 
{
    public int publicField;
    int packagePrivate;
    private int myPrivate;
    protected int myProtected;
    
    public string MyProperty { get; private set; }

    public event Action<int> ValueChanged;
}

public enum EDay
{
    Today, Tomorrow
}

public interface IXmlReader {
    void ReadXml(string filePath);
}
```

## Repository Folder Structure
The current folder structure is just a first draft, and you are encouraged to improve it, if you have extensive knowledge of / experience in open source unity games.

Prefer PascalCase to name folders.

The current assets folder hierarchy is structured by scene and combines assets that implement a common idea. The goal is to have assets nearby that are closely related to another, such that a task at hand can be done with little search in the folder hierarchy.

As a result, assets that are used in only one scene are placed in the folder of this scene. Similarly, assets that implement a common idea (e.g. a dragon image, dragon script, dragon sounds) could go to a common folder (e.g. dragon).

| Where | What |
|---|---|
| / | Main repo folder. Try to not add any new files here, but instead place them in a fitting subfolder. |
| /tools/ | Any build scripts, templates, helper stuff for devs, code checking stuff, lint templates. |
| /UltraStar Play/ | Unity project |
| ./Assets/Editor/ | [Special folder](https://docs.unity3d.com/Manual/SpecialFolders.html) for code that extend the Unity Editor. Also, our unit tests and integration tests go here |
| ./Assets/Common/ | Assets that are used in multiple scenes or are not related to any scene |
| ./Assets/Scenes/ | Unity scene files with their related resources |
| ./Assets/Common/Graphics/ | Common visual assets, e.g. background images files, UI skin |
| ./Assets/Common/Audio/ | Common sound files and code related to audio processing and pitch detection |
| ./Assets/Common/Model/ | Code related to the data model and related (single instance) manager classes |
| ./Assets/Common/Util/ |  Rather generic utility code that is not specific to this karaoke game |

## Project Insights

### Tests

The tests can be run via Unity's [Test Runner](https://docs.unity3d.com/2019.1/Documentation/Manual/testing-editortestsrunner.html). Open it via **Window > General > Test Runner**.
At the moment, most of the tests are made to be run in EditMode. Thus, the tests can be found in `Assets/Editor/Tests`.

### CommonSceneObjects

There is a prefab called `CommonSceneObjects`, which should be placed in every scene.
It holds the single instances of manager classes (e.g. I18NManager), as well as the main camera and other objects that are relevant for all scenes.

### Single Instance Classes
The single instance pattern is implemented a little different in UltraStar Play than one might expect.
Static fields are invisible to Unity's [Serialization System](https://docs.unity3d.com/Manual/script-Serialization.html). Thus, these fields are reset on Hotswap.

As a result, the single instance pattern has been implemented using [tags](https://docs.unity3d.com/Manual/Tags.html) that identify the instances in the object hierarchy. An instance of the GameObject with this tag is added to the CommonSceneObjects prefab to make it available in every scene.
Afterwards, a static getter for this instance can be implemented as follows:
```
public class SceneNavigator
{
    public static SceneNavigator Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SceneNavigator>("SceneNavigator");
        }
    }
}
```

The CommonSceneObjectsBinder binds these instances such that they can be used via dependency injection (see [wiki page on UniInject](https://github.com/UltraStar-Deluxe/Play/wiki/Dependency-Injection-and-UniInject)).
Example:

```
...
using UniInject;

public class MyCoolScript : INeedInjection
{
    [Inject]
    private SceneNavigator sceneNavigator;
    
    void Start() {
        // Do something with the sceneNavigator instance.
    }
}
```

### Loading a Scene with Parameters

The SceneNavigator class holds a static collection of SceneData objects, which is used to temporarily hold SceneData objects. The Controller class of the newly opened scene can query this data and store it in a non-static field, such that it will be available also after Hotswap.

Every scene should be playable without the need to navigate other scenes before.
As a result, a Controller class should have a sensible default for its SceneData.

Example:
```
SongSelectSceneController.cs:

private void OpenSingSceneWithSelectedSong() {
    SingSceneData singSceneData = ...
    SceneNavigator.Instance.LoadScene(EScene.SingScene, singSceneData);
}
```

```
SingSceneController.cs:

void Start() {
    // Load scene data from static reference, or use default if none
    SingSceneData defaultSingSceneData = ...
    singSceneData = SceneNavigator.Instance.GetSceneData(defaultSingSceneData);
}
```

### Serializable
Model classes should have the `[Serializable]` annotation and use serializable fields if possible. The annotation will make these classes visible to Unity's [serialization system](https://docs.unity3d.com/Manual/script-Serialization.html). This means their instances will be visible in the inspector. Furthermore, serializable instance are stored / reloaded on Hotswap (but only their serializable fields).

Properties with a backing field are serialized. Such a property requires a get **and** set method.
Example:
```
[Serializable]
public class Bla
{
    public int MySerializableProperty { get; private set; }
    public int MyNonSerialzableProperty { get; }

    public Bla()
    {
        MySerializableProperty = 1;
        MyNonSerialzableProperty = 2;
    }
}
```

You can see all serializable fields in the Inspector in [Debug mode](https://docs.unity3d.com/Manual/InspectorOptions.html).

### Internationalization (I18N)

See the [wiki page for translations](https://github.com/UltraStar-Deluxe/Play/wiki/Translations,-Internationalization-(I18N)).

### Unity Callback Function Guidelines

Some methods of a MonoBehaviour such as Awake and Start are called by the Unity platform. See the manual for an [overview of the execution order of these methods](https://docs.unity3d.com/Manual/ExecutionOrder.html).

A detail is the execution order when instantiating a MonoBehaviour at runtime. In this case, Awake and OnEnable will be called before the call to Instantiate(...) returns. But Start will be called normally.

In addition to this, a MonoBehaviour can implement some interfaces to receive further callbacks. For example, ISerializationCallbackReceiver has methods that are called before and after serialization.

UltraStar Play uses a similar approach to notify scripts about certain events:

- IOnHotSwapFinishedListener are notified after hotswap finished.
    - The script OnHotSwapFinishedNotifier is taking care of calling the methods of this interface.
- ISceneInjectionFinishedListener will be notified when scene injection has been finished. This is done in the Awake method of the SceneInjectionManager and before any Start method is called.

As rule of thumb the methods should be used for the following:
- Awake: resolve references, e.g. call GetComponent or GetComponentInChildren. Do not assume that other references have been resolved yet.
- Start: perform setup code. You can assume that injected fields have been resolved.
- OnEnable: perform setup code that needs to be undone later in OnDisable. Note that OnEnable is called directly by Unity when a script is instantiated at runtime. Thus, you cannot assume that injection of fields has been done yet in this method.
- OnDisable: undo stuff that has been set up in OnEnable
- OnSceneInjectionFinished: do setup stuff on values that have been injected.
    - For example, consider you want to be notified by SenderScript about an event that is issued in SenderScript's Start method. To handle this, you can inject SenderScript and subscribe to its event stream in OnSceneInjectionFinished. Note that subscribing to the event in Start might be too late, because when SenderScript's Start method has been called before your script, then you lost the initial event.
- OnHotSwapFinished: restore stuff that is lost on hotswap. For example subscription to reactive streams are lost in hotswap. Also, methods that were triggered by InvokeRepeating are not called anymore after hotswap.
