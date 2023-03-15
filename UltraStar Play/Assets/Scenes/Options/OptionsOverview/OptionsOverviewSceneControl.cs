using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class OptionsOverviewSceneControl : MonoBehaviour, INeedInjection, ITranslator, IBinder
{
    private const EScene DefaultOptionsScene = EScene.OptionsGameScene;
    
    public List<SceneRecipe> optionSceneRecipes = new();
    
    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;
    
    [Inject(UxmlName = R.UxmlNames.titleContainer)]
    private VisualElement titleContainer;

    [Inject(UxmlName = R.UxmlNames.loadedSceneTitle)]
    private Label loadedSceneTitle;
    
    [Inject(UxmlName = R.UxmlNames.contentDownloadButton)]
    private ToggleButton contentDownloadButton;
    
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

    [Inject(UxmlName = R.UxmlNames.internetOptionsButton)]
    private ToggleButton internetOptionsButton;

    [Inject(UxmlName = R.UxmlNames.appOptionsButton)]
    private ToggleButton appOptionsButton;

    [Inject(UxmlName = R.UxmlNames.developerOptionsButton)]
    private ToggleButton developerOptionsButton;

    [Inject(UxmlName = R.UxmlNames.webcamOptionsButton)]
    private ToggleButton webcamOptionsButton;

    [Inject(UxmlName = R.UxmlNames.songSettingsProblemHintIcon)]
    private VisualElement songSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.recordingSettingsProblemHintIcon)]
    private VisualElement recordingSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.playerProfileSettingsProblemHintIcon)]
    private VisualElement playerProfileSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.loadedSceneContent)]
    private VisualElement loadedSceneContent;
    
    [Inject(UxmlName = R.UxmlNames.optionsSceneScrollView)]
    private VisualElement optionsSceneScrollView;
    
    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;
    
    [Inject(UxmlName = R.UxmlNames.helpButton)]
    private Button helpButton;
    
    [Inject(UxmlName = R.UxmlNames.issuesButton)]
    private Button issuesButton;
    
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private OptionsSceneData sceneData;
    
    private SceneRecipe loadedSceneRecipe;
    private readonly List<GameObject> loadedGameObjects = new();
    private readonly Dictionary<EScene, ToggleButton> sceneToButtonMap = new();
    private readonly Dictionary<EScene, string> sceneToShortNameMap = new();
    private readonly Dictionary<EScene, string> sceneToLongNameMap = new();

    private AbstractOptionsSceneControl LoadedOptionsSceneControl => loadedGameObjects
        .Select(it => it.GetComponentInChildren<AbstractOptionsSceneControl>())
        .FirstOrDefault();

    private MessageDialogControl helpDialogControl;
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

        helpButton.RegisterCallbackButtonTriggered(_ => ShowHelp());
        issuesButton.RegisterCallbackButtonTriggered(_ => ShowIssuesDialog());
        
        backButton.RegisterCallbackButtonTriggered(_ => OnBack());
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
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

        // Load scene scripts.
        // Add new bindings.
        Injector loadedSceneInjector = injector.CreateChildInjector();
        foreach (GameObject gameObjectRecipe in loadedSceneRecipe.sceneGameObjects)
        {
            foreach (IBinder binder in gameObjectRecipe.GetComponentsInChildren<IBinder>())
            {
                binder.GetBindings().ForEach(binding => loadedSceneInjector.AddBinding(binding));
            }
        }

        // Inject new game objects
        foreach (GameObject gameObjectRecipe in loadedSceneRecipe.sceneGameObjects)
        {
            GameObject loadedGameObject = Instantiate(gameObjectRecipe);
            loadedGameObjects.Add(loadedGameObject);
            
            // Inject new game object
            loadedSceneInjector
                .WithRootVisualElement(loadedSceneVisualElement)
                .InjectAllComponentsInChildren(loadedGameObject);
            
            // Update translations
            loadedGameObject.GetComponentsInChildren<ITranslator>()
                .ForEach(it => it.UpdateTranslation());
        }

        // Set loaded scene title
        loadedSceneTitle.text = sceneToLongNameMap[loadedSceneRecipe.scene];

        // Hide buttons in top row
        helpButton.SetVisibleByDisplay(LoadedOptionsSceneControl.HasHelpDialog);
        issuesButton.SetVisibleByDisplay(LoadedOptionsSceneControl.HasIssuesDialog);
        
        // Apply theme to loaded UI
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(loadedSceneVisualElement);
        
        // Scroll with mouse drag
        MouseEventScrollControl.RegisterMouseScrollEvents();
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
            SettingsProblemHintControl.GetSongLibrarySettingsProblems(settings, songMetaManager),
            injector);

        SettingsProblemHintControl recordingSettingsProblemHintControl = new(
            recordingSettingsProblemHintIcon,
            SettingsProblemHintControl.GetRecordingSettingsProblems(settings),
            injector);

        SettingsProblemHintControl playerProfileSettingsProblemHintControl = new(
            playerProfileSettingsProblemHintIcon,
            SettingsProblemHintControl.GetPlayerSettingsProblems(settings),
            injector);

        StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(0.5f, () =>
        {
            songSettingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetSongLibrarySettingsProblems(settings, songMetaManager));
            recordingSettingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetRecordingSettingsProblems(settings));
            playerProfileSettingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetPlayerSettingsProblems(settings));
        }));
    }

    public void UpdateTranslation()
    {
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options);
        
        UpdateSceneToNameMap();
        sceneToButtonMap.ForEach(entry =>
        {
            Button button = entry.Value;
            Label label = button.Q<Label>(R.UxmlNames.label);
            label.text = sceneToShortNameMap[entry.Key];
        });

        if (loadedSceneRecipe != null)
        {
            loadedSceneTitle.text = sceneToLongNameMap[loadedSceneRecipe.scene];
        }
    }

    private void UpdateSceneToNameMap()
    {
        sceneToShortNameMap.Clear();
        sceneToShortNameMap.Add(EScene.ContentDownloadScene, TranslationManager.GetTranslation(R.Messages.options_downloadSongs_title));
        sceneToShortNameMap.Add(EScene.OptionsGameScene, TranslationManager.GetTranslation(R.Messages.options_game_button));
        sceneToShortNameMap.Add(EScene.SongLibraryOptionsScene, TranslationManager.GetTranslation(R.Messages.options_songLibrary_button));
        sceneToShortNameMap.Add(EScene.OptionsSoundScene, TranslationManager.GetTranslation(R.Messages.options_sound_button));
        sceneToShortNameMap.Add(EScene.OptionsGraphicsScene, TranslationManager.GetTranslation(R.Messages.options_graphics_button));
        sceneToShortNameMap.Add(EScene.RecordingOptionsScene, TranslationManager.GetTranslation(R.Messages.options_recording_button));
        sceneToShortNameMap.Add(EScene.PlayerProfileSetupScene, TranslationManager.GetTranslation(R.Messages.options_playerProfiles_button));
        sceneToShortNameMap.Add(EScene.ThemeOptionsScene, TranslationManager.GetTranslation(R.Messages.options_design_button));
        sceneToShortNameMap.Add(EScene.NetworkOptionsScene, TranslationManager.GetTranslation(R.Messages.options_internet_button));
        sceneToShortNameMap.Add(EScene.CompanionAppOptionsScene, TranslationManager.GetTranslation(R.Messages.options_companionApp_button));
        sceneToShortNameMap.Add(EScene.WebcamOptionsSecene, TranslationManager.GetTranslation(R.Messages.options_webcam_button));
        sceneToShortNameMap.Add(EScene.DevelopmentOptionsScene, TranslationManager.GetTranslation(R.Messages.options_development_button));
        
        sceneToLongNameMap.Clear();
        sceneToLongNameMap.Add(EScene.ContentDownloadScene, TranslationManager.GetTranslation(R.Messages.options_downloadSongs_title));
        sceneToLongNameMap.Add(EScene.OptionsGameScene, TranslationManager.GetTranslation(R.Messages.options_game_title));
        sceneToLongNameMap.Add(EScene.SongLibraryOptionsScene, TranslationManager.GetTranslation(R.Messages.options_songLibrary_title));
        sceneToLongNameMap.Add(EScene.OptionsSoundScene, TranslationManager.GetTranslation(R.Messages.options_sound_title));
        sceneToLongNameMap.Add(EScene.OptionsGraphicsScene, TranslationManager.GetTranslation(R.Messages.options_graphics_title));
        sceneToLongNameMap.Add(EScene.RecordingOptionsScene, TranslationManager.GetTranslation(R.Messages.options_recording_title));
        sceneToLongNameMap.Add(EScene.PlayerProfileSetupScene, TranslationManager.GetTranslation(R.Messages.options_playerProfiles_title));
        sceneToLongNameMap.Add(EScene.ThemeOptionsScene, TranslationManager.GetTranslation(R.Messages.options_design_title));
        sceneToLongNameMap.Add(EScene.NetworkOptionsScene, TranslationManager.GetTranslation(R.Messages.options_internet_title));
        sceneToLongNameMap.Add(EScene.CompanionAppOptionsScene, TranslationManager.GetTranslation(R.Messages.options_companionApp_title));
        sceneToLongNameMap.Add(EScene.WebcamOptionsSecene, TranslationManager.GetTranslation(R.Messages.options_webcam_title));
        sceneToLongNameMap.Add(EScene.DevelopmentOptionsScene, TranslationManager.GetTranslation(R.Messages.options_development_title));
    }
    
    private void UpdateSceneToButtonMap()
    {
        sceneToButtonMap.Add(EScene.ContentDownloadScene, contentDownloadButton);
        sceneToButtonMap.Add(EScene.OptionsGameScene, gameOptionsButton);
        sceneToButtonMap.Add(EScene.SongLibraryOptionsScene, songsOptionsButton);
        sceneToButtonMap.Add(EScene.OptionsGraphicsScene, graphicsOptionsButton);
        sceneToButtonMap.Add(EScene.OptionsSoundScene, soundOptionsButton);
        sceneToButtonMap.Add(EScene.RecordingOptionsScene, recordingOptionsButton);
        sceneToButtonMap.Add(EScene.PlayerProfileSetupScene, profileOptionsButton);
        sceneToButtonMap.Add(EScene.ThemeOptionsScene, designOptionsButton);
        sceneToButtonMap.Add(EScene.NetworkOptionsScene, internetOptionsButton);
        sceneToButtonMap.Add(EScene.CompanionAppOptionsScene, appOptionsButton);
        sceneToButtonMap.Add(EScene.DevelopmentOptionsScene, developerOptionsButton);
        sceneToButtonMap.Add(EScene.WebcamOptionsSecene, webcamOptionsButton);
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
        if (helpDialogControl != null)
        {
            return;
        }

        if (LoadedOptionsSceneControl.HasHelpDialog)
        {
            helpDialogControl = LoadedOptionsSceneControl.CreateHelpDialogControl();
            helpDialogControl.DialogClosedEventStream.Subscribe(_ =>
            {
                helpDialogControl = null;
                helpButton.Focus();
            });
            ThemeManager.ApplyThemeSpecificStylesToVisualElements(helpDialogControl.DialogRootVisualElement);
        }
    }

    public void ShowIssuesDialog()
    {
        if (issuesDialogControl != null)
        {
            return;
        }

        if (LoadedOptionsSceneControl.HasHelpDialog)
        {
            issuesDialogControl = LoadedOptionsSceneControl.CreateIssuesDialogControl();
            issuesDialogControl.DialogClosedEventStream.Subscribe(_ =>
            {
                issuesDialogControl = null;
                issuesButton.Focus();
            });
            ThemeManager.ApplyThemeSpecificStylesToVisualElements(issuesDialogControl.DialogRootVisualElement);
        }
    }

    private void OnDestroy()
    {
        UnloadLastOptionsScene();
    }
}
