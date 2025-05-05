using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class OptionsOverviewSceneControl : MonoBehaviour, INeedInjection, IBinder
{
    private const EScene DefaultOptionsScene = EScene.OptionsGameScene;

    public List<SceneRecipe> optionSceneRecipes = new();

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.titleContainer)]
    private VisualElement titleContainer;

    [Inject(UxmlName = R.UxmlNames.loadedSceneTitle)]
    private Label loadedSceneTitle;

    [Inject(UxmlName = R.UxmlNames.gameOptionsButton)]
    private ToggleButton gameOptionsButton;

    [Inject(UxmlName = R.UxmlNames.songsOptionsButton)]
    private ToggleButton songsOptionsButton;

    [Inject(UxmlName = R.UxmlNames.graphicsOptionsButton)]
    private ToggleButton graphicsOptionsButton;

    [Inject(UxmlName = R.UxmlNames.soundOptionsButton)]
    private ToggleButton soundOptionsButton;

    [Inject(UxmlName = R.UxmlNames.recordingOptionsButton)]
    private ToggleButton recordingOptionsButton;

    [Inject(UxmlName = R.UxmlNames.profileOptionsButton)]
    private ToggleButton profileOptionsButton;

    [Inject(UxmlName = R.UxmlNames.designOptionsButton)]
    private ToggleButton designOptionsButton;

    [Inject(UxmlName = R.UxmlNames.appOptionsButton)]
    private ToggleButton appOptionsButton;

    [Inject(UxmlName = R.UxmlNames.developerOptionsButton)]
    private ToggleButton developerOptionsButton;

    [Inject(UxmlName = R.UxmlNames.webcamOptionsButton)]
    private ToggleButton webcamOptionsButton;

    [Inject(UxmlName = R.UxmlNames.modOptionsButton)]
    private ToggleButton modOptionsButton;

    [Inject(UxmlName = R.UxmlNames.songSettingsProblemHintIcon)]
    private VisualElement songSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.recordingSettingsProblemHintIcon)]
    private VisualElement recordingSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.playerProfileSettingsProblemHintIcon)]
    private VisualElement playerProfileSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.modSettingsProblemHintIcon)]
    private VisualElement modSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.loadedSceneContent)]
    private VisualElement loadedSceneContent;

    [Inject(UxmlName = R.UxmlNames.optionsSceneScrollView)]
    private VisualElement optionsSceneScrollView;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.helpButton)]
    private Button helpButton;

    [Inject(UxmlName = R.UxmlNames.openSteamWorkshopButton)]
    private Button openSteamWorkshopButton;

    [Inject(UxmlName = R.UxmlNames.updateSteamWorkshopItemsButton)]
    private Button updateSteamWorkshopItemsButton;

    [Inject(UxmlName = R.UxmlNames.issuesButton)]
    private Button issuesButton;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private Settings settings;

    [Inject]
    private ModManager modManager;

    [Inject]
    private SongIssueManager songIssueManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private OptionsSceneData sceneData;

    [Inject]
    private SteamWorkshopManager steamWorkshopManager;

    private SceneRecipe loadedSceneRecipe;
    private readonly List<GameObject> loadedGameObjects = new();
    private readonly Dictionary<EScene, ToggleButton> sceneToButtonMap = new();
    private readonly Dictionary<EScene, Translation> sceneToShortNameMap = new();
    private readonly Dictionary<EScene, Translation> sceneToLongNameMap = new();

    private AbstractOptionsSceneControl LoadedOptionsSceneControl => loadedGameObjects
        .Select(it => it.GetComponentInChildren<AbstractOptionsSceneControl>())
        .FirstOrDefault();

    private MessageDialogControl issuesDialogControl;

    private void Start()
    {
        UpdateSceneToButtonMap();
        UpdateSceneToNameMap();

        sceneToButtonMap.ForEach(entry =>
        {
           entry.Value.RegisterCallbackButtonTriggered(_ => LoadScene(entry.Key));
        });

        InitSettingsProblemHints();

        LoadOptionsScene(sceneData.scene);

        // Options scene scroll view should be as wide as the title.
        titleContainer.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            float targetMinWidth = evt.newRect.width;
            if (Math.Abs(optionsSceneScrollView.resolvedStyle.minWidth.value - targetMinWidth) > 1f)
            {
                optionsSceneScrollView.style.minWidth = targetMinWidth;
            }
        });

        openSteamWorkshopButton.RegisterCallbackButtonTriggered(_ => OpenSteamWorkshop());

        updateSteamWorkshopItemsButton.RegisterCallbackButtonTriggered(_ => UpdateSteamWorkshopItems());
        new TooltipControl(updateSteamWorkshopItemsButton, Translation.Get(R.Messages.steamWorkshop_updateTooltip));

        helpButton.RegisterCallbackButtonTriggered(_ => ShowHelp());
        issuesButton.RegisterCallbackButtonTriggered(_ => ShowIssuesDialog());

        backButton.RegisterCallbackButtonTriggered(_ => OnBack());
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
    }

    private void OpenSteamWorkshop()
    {
        if (LoadedOptionsSceneControl.SteamWorkshopUri.IsNullOrEmpty())
        {
            return;
        }

        steamWorkshopManager.OpenSteamWorkshopOverlay(LoadedOptionsSceneControl.SteamWorkshopUri);
    }

    private async void UpdateSteamWorkshopItems()
    {
        await steamWorkshopManager.DownloadWorkshopItemsAsync();
        if (GameObjectUtils.IsDestroyed(this))
        {
            return;
        }

        Debug.Log("Reloading current options scene because Steam Workshop items update finished");
        await Awaitable.MainThreadAsync();
        ReloadCurrentOptionsScene();
    }

    private void ReloadCurrentOptionsScene()
    {
        sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(loadedSceneRecipe.scene));
    }

    private void OnBack()
    {
        sceneNavigator.LoadScene(EScene.MainScene);
    }

    public void LoadScene(EScene scene)
    {
        if (optionSceneRecipes.AnyMatch(recipe => recipe.scene == scene))
        {
            LoadOptionsScene(scene);
        }
        else
        {
            sceneNavigator.LoadScene(scene);
        }
    }

    private void LoadOptionsScene(EScene scene)
    {
        UnloadLastOptionsScene();
        loadedSceneRecipe = optionSceneRecipes.FirstOrDefault(it => it.scene == scene);

        // Set button style
        sceneToButtonMap[scene].SetActive(true);
        sceneToButtonMap[scene].Focus();

        // Load UI
        VisualElement loadedSceneVisualElement = loadedSceneRecipe.visualTreeAsset.CloneTree().Children().FirstOrDefault();
        loadedSceneContent.Add(loadedSceneVisualElement);

        // Instantiate new game objects
        foreach (GameObject gameObjectRecipe in loadedSceneRecipe.sceneGameObjects)
        {
            GameObject loadedGameObject = Instantiate(gameObjectRecipe);
            loadedGameObjects.Add(loadedGameObject);
        }

        // Add new bindings
        Injector loadedSceneInjector = injector.CreateChildInjector();
        foreach (GameObject loadedGameObject in loadedGameObjects)
        {
            foreach (IBinder binder in loadedGameObject.GetComponentsInChildren<IBinder>())
            {
                binder.GetBindings().ForEach(binding => loadedSceneInjector.AddBinding(binding));
            }
        }

        // Inject and update translations
        foreach (GameObject loadedGameObject in loadedGameObjects)
        {
            // Inject new game object
            loadedSceneInjector
                .WithRootVisualElement(loadedSceneVisualElement)
                .InjectAllComponentsInChildren(loadedGameObject, true);
        }

        // Set loaded scene title
        loadedSceneTitle.SetTranslatedText(sceneToLongNameMap[loadedSceneRecipe.scene]);

        // Hide buttons in top row
        helpButton.SetVisibleByDisplay(!LoadedOptionsSceneControl.HelpUri.IsNullOrEmpty());
        issuesButton.SetVisibleByDisplay(LoadedOptionsSceneControl.HasIssuesDialog);
        openSteamWorkshopButton.SetVisibleByDisplay(!LoadedOptionsSceneControl.SteamWorkshopUri.IsNullOrEmpty());
        updateSteamWorkshopItemsButton.SetVisibleByDisplay(openSteamWorkshopButton.IsVisibleByDisplay());

        // Scroll with mouse drag
        MouseEventScrollControl.RegisterMouseScrollEvents();

        UpdateTranslation();
    }

    private void UnloadLastOptionsScene()
    {
        if (loadedSceneRecipe == null)
        {
            return;
        }

        sceneToButtonMap[loadedSceneRecipe.scene].SetActive(false);

        loadedGameObjects.ForEach(Destroy);
        loadedGameObjects.Clear();

        loadedSceneContent.Clear();
        loadedSceneRecipe = null;
    }

    private void InitSettingsProblemHints()
    {
        SettingsProblemHintControl songSettingsProblemHintControl = new(
            songSettingsProblemHintIcon,
            SettingsProblemHintControl.GetSongLibrarySettingsProblems(settings, songIssueManager));

        SettingsProblemHintControl recordingSettingsProblemHintControl = new(
            recordingSettingsProblemHintIcon,
            SettingsProblemHintControl.GetRecordingSettingsProblems(settings));

        SettingsProblemHintControl playerProfileSettingsProblemHintControl = new(
            playerProfileSettingsProblemHintIcon,
            SettingsProblemHintControl.GetPlayerSettingsProblems(settings));

        SettingsProblemHintControl modSettingsProblemHintControl = new(
            modSettingsProblemHintIcon,
            SettingsProblemHintControl.GetModSettingsProblems(modManager));

        AwaitableUtils.ExecuteRepeatedlyInSecondsAsync(gameObject, 0.5f, () =>
        {
            songSettingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetSongLibrarySettingsProblems(settings, songIssueManager));
            recordingSettingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetRecordingSettingsProblems(settings));
            playerProfileSettingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetPlayerSettingsProblems(settings));
            modSettingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetModSettingsProblems(modManager));
        });
    }

    public void UpdateTranslation()
    {
        sceneTitle.SetTranslatedText(Translation.Get(R.Messages.options));

        UpdateSceneToNameMap();
        sceneToButtonMap.ForEach(entry =>
        {
            Button button = entry.Value;
            Label label = button.Q<Label>(R.UxmlNames.label);
            label.SetTranslatedText(sceneToShortNameMap[entry.Key]);
        });

        if (loadedSceneRecipe != null)
        {
            loadedSceneTitle.SetTranslatedText(sceneToLongNameMap[loadedSceneRecipe.scene]);
        }
    }

    private void UpdateSceneToNameMap()
    {
        sceneToShortNameMap.Clear();
        sceneToShortNameMap.Add(EScene.OptionsGameScene, Translation.Get(R.Messages.options_game_button));
        sceneToShortNameMap.Add(EScene.SongLibraryOptionsScene, Translation.Get(R.Messages.options_songLibrary_button));
        sceneToShortNameMap.Add(EScene.OptionsSoundScene, Translation.Get(R.Messages.options_sound_button));
        sceneToShortNameMap.Add(EScene.OptionsGraphicsScene, Translation.Get(R.Messages.options_graphics_button));
        sceneToShortNameMap.Add(EScene.RecordingOptionsScene, Translation.Get(R.Messages.options_recording_button));
        sceneToShortNameMap.Add(EScene.PlayerProfileSetupScene, Translation.Get(R.Messages.options_playerProfiles_button));
        sceneToShortNameMap.Add(EScene.ThemeOptionsScene, Translation.Get(R.Messages.options_design_button));
        sceneToShortNameMap.Add(EScene.CompanionAppOptionsScene, Translation.Get(R.Messages.options_companionApp_button));
        sceneToShortNameMap.Add(EScene.WebcamOptionsSecene, Translation.Get(R.Messages.options_webcam_button));
        sceneToShortNameMap.Add(EScene.DevelopmentOptionsScene, Translation.Get(R.Messages.options_development_button));
        sceneToShortNameMap.Add(EScene.ModOptionsScene, Translation.Get(R.Messages.options_mod_button));

        sceneToLongNameMap.Clear();
        sceneToLongNameMap.Add(EScene.OptionsGameScene, Translation.Get(R.Messages.options_game_title));
        sceneToLongNameMap.Add(EScene.SongLibraryOptionsScene, Translation.Get(R.Messages.options_songLibrary_title));
        sceneToLongNameMap.Add(EScene.OptionsSoundScene, Translation.Get(R.Messages.options_sound_title));
        sceneToLongNameMap.Add(EScene.OptionsGraphicsScene, Translation.Get(R.Messages.options_graphics_title));
        sceneToLongNameMap.Add(EScene.RecordingOptionsScene, Translation.Get(R.Messages.options_recording_title));
        sceneToLongNameMap.Add(EScene.PlayerProfileSetupScene, Translation.Get(R.Messages.options_playerProfiles_title));
        sceneToLongNameMap.Add(EScene.ThemeOptionsScene, Translation.Get(R.Messages.options_design_title));
        sceneToLongNameMap.Add(EScene.CompanionAppOptionsScene, Translation.Get(R.Messages.options_companionApp_title));
        sceneToLongNameMap.Add(EScene.WebcamOptionsSecene, Translation.Get(R.Messages.options_webcam_title));
        sceneToLongNameMap.Add(EScene.DevelopmentOptionsScene, Translation.Get(R.Messages.options_development_title));
        sceneToLongNameMap.Add(EScene.ModOptionsScene, Translation.Get(R.Messages.options_mod_title));
    }

    private void UpdateSceneToButtonMap()
    {
        sceneToButtonMap.Add(EScene.OptionsGameScene, gameOptionsButton);
        sceneToButtonMap.Add(EScene.SongLibraryOptionsScene, songsOptionsButton);
        sceneToButtonMap.Add(EScene.OptionsGraphicsScene, graphicsOptionsButton);
        sceneToButtonMap.Add(EScene.OptionsSoundScene, soundOptionsButton);
        sceneToButtonMap.Add(EScene.RecordingOptionsScene, recordingOptionsButton);
        sceneToButtonMap.Add(EScene.PlayerProfileSetupScene, profileOptionsButton);
        sceneToButtonMap.Add(EScene.ThemeOptionsScene, designOptionsButton);
        sceneToButtonMap.Add(EScene.CompanionAppOptionsScene, appOptionsButton);
        sceneToButtonMap.Add(EScene.DevelopmentOptionsScene, developerOptionsButton);
        sceneToButtonMap.Add(EScene.WebcamOptionsSecene, webcamOptionsButton);
        sceneToButtonMap.Add(EScene.ModOptionsScene, modOptionsButton);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(SceneNavigator.GetSceneData(new OptionsSceneData(DefaultOptionsScene)));
        return bb.GetBindings();
    }

    public void ShowHelp()
    {
        if (LoadedOptionsSceneControl.HelpUri.IsNullOrEmpty())
        {
            return;
        }

        ApplicationUtils.OpenUrl(LoadedOptionsSceneControl.HelpUri);
    }

    public void ShowIssuesDialog()
    {
        if (issuesDialogControl != null
            || !LoadedOptionsSceneControl.HasIssuesDialog)
        {
            return;
        }

        issuesDialogControl = LoadedOptionsSceneControl.CreateIssuesDialogControl();
        issuesDialogControl.DialogClosedEventStream.Subscribe(_ =>
        {
            issuesDialogControl = null;
            issuesButton.Focus();
        });
    }

    private void OnDestroy()
    {
        UnloadLastOptionsScene();
    }
}
