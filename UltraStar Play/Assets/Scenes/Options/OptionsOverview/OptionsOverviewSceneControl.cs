using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniInject.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class OptionsOverviewSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    public List<OptionSceneRecipe> optionSceneRecipes = new();
    
    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

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

    [Inject(UxmlName = R.UxmlNames.languageChooser)]
    private ItemPicker languageChooser;

    [Inject(UxmlName = R.UxmlNames.songSettingsProblemHintIcon)]
    private VisualElement songSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.recordingSettingsProblemHintIcon)]
    private VisualElement recordingSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.playerProfileSettingsProblemHintIcon)]
    private VisualElement playerProfileSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.loadedSceneUi)]
    private VisualElement loadedSceneUi;
    
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
    
    private readonly List<GameObject> loadedGameObjects = new();
    private readonly Dictionary<EScene, ToggleButton> sceneToButtonMap = new();
    private OptionSceneRecipe loadedSceneRecipe;
    
    private void Start()
    {
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

        sceneToButtonMap.ForEach(entry =>
        {
           entry.Value.RegisterCallbackButtonTriggered(() => LoadScene(entry.Key)); 
        });

        gameOptionsButton.Focus();

        InitSettingsProblemHints();
        InitLanguageChooser();
        
        LoadOptionsScene(optionSceneRecipes.FirstOrDefault());
    }

    private void LoadScene(EScene scene)
    {
        OptionSceneRecipe sceneRecipe = optionSceneRecipes.FirstOrDefault(recipe => recipe.scene == scene);
        if (sceneRecipe != null)
        {
            LoadOptionsScene(sceneRecipe);
        }
        else
        {
            sceneNavigator.LoadScene(scene);
        }
    }

    private void LoadOptionsScene(OptionSceneRecipe sceneRecipe)
    {
        // Set button style
        if (loadedSceneRecipe != null)
        {
            sceneToButtonMap[loadedSceneRecipe.scene].SetActive(false);
        }
        sceneToButtonMap[sceneRecipe.scene].SetActive(true);
        
        // Remove objects of old scene
        loadedGameObjects.ForEach(Destroy);
        loadedGameObjects.Clear();
        
        // Load scene UI
        loadedSceneUi.Clear();
        VisualElement loadedSceneVisualElement = sceneRecipe.visualTreeAsset.CloneTree().Children().FirstOrDefault();
        loadedSceneUi.Add(loadedSceneVisualElement);

        VisualElement scoreModeContainer = loadedSceneVisualElement.Q<VisualElement>(R.UxmlNames.scoreModeContainer);

        // Load scene scripts.
        // Add new bindings.
        Injector loadedSceneInjector = injector.CreateChildInjector();
        foreach (GameObject gameObjectRecipe in sceneRecipe.sceneGameObjects)
        {
            foreach (IBinder binder in gameObjectRecipe.GetComponentsInChildren<IBinder>())
            {
                binder.GetBindings().ForEach(binding => loadedSceneInjector.AddBinding(binding));
            }
        }

        // Inject new game objects
        foreach (GameObject gameObjectRecipe in sceneRecipe.sceneGameObjects)
        {
            GameObject loadedGameObject = Instantiate(gameObjectRecipe);
            loadedGameObjects.Add(loadedGameObject);
            
            // Inject new game object
            loadedSceneInjector
                .WithRootVisualElement(loadedSceneVisualElement)
                .InjectAllComponentsInChildren(loadedGameObject);
        }

        loadedSceneRecipe = sceneRecipe;
    }

    private void InitSettingsProblemHints()
    {
        new SettingsProblemHintControl(
            songSettingsProblemHintIcon,
            SettingsProblemHintControl.GetSongLibrarySettingsProblems(settings, songMetaManager),
            injector);

        new SettingsProblemHintControl(
            recordingSettingsProblemHintIcon,
            SettingsProblemHintControl.GetRecordingSettingsProblems(settings),
            injector);

        new SettingsProblemHintControl(
            playerProfileSettingsProblemHintIcon,
            SettingsProblemHintControl.GetPlayerSettingsProblems(settings),
            injector);
    }

    private void InitLanguageChooser()
    {
        new LabeledItemPickerControl<SystemLanguage>(
                languageChooser,
                translationManager.GetTranslatedLanguages())
            .Bind(() => translationManager.currentLanguage,
                newValue => SetLanguage(newValue));
    }

    public void UpdateTranslation()
    {
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options);
        gameOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_game_button);
        songsOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_button);
        soundOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_sound_button);
        graphicsOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_graphics_button);
        recordingOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_recording_button);
        profileOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_playerProfiles_button);
        designOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_design_button);
        internetOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_internet_button);
        appOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_companionApp_button);
        developerOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_development_button);
        webcamOptionsButton.Q<Label>(R.UxmlNames.label).text = TranslationManager.GetTranslation(R.Messages.options_webcam_button);
    }

    private void SetLanguage(SystemLanguage newValue)
    {
        if (settings.GameSettings.language == newValue
            && translationManager.currentLanguage == newValue)
        {
            return;
        }

        settings.GameSettings.language = newValue;
        translationManager.currentLanguage = settings.GameSettings.language;
        translationManager.ReloadTranslationsAndUpdateScene();
    }
}
