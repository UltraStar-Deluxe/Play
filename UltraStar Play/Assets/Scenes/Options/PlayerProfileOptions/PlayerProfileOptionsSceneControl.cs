using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerProfileOptionsSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset playerProfileListEntryAsset;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.profileList)]
    private ScrollView profileList;

    [Inject(UxmlName = R.UxmlNames.addButton)]
    private Button addButton;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject]
    private Settings settings;

    [Inject]
    private UiManager uiManager;

    private void Start()
    {
        UpdatePlayerProfileList();
        settings.ObserveEveryValueChanged(s => s.PlayerProfiles)
            .Subscribe(onNext => UpdatePlayerProfileList())
            .AddTo(gameObject);

        addButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.PlayerProfiles.Add(new PlayerProfile());
            UpdatePlayerProfileList();
        });

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_playerProfiles_title);
    }

    private void UpdatePlayerProfileList()
    {
        profileList.Clear();
        int index = 0;
        settings.PlayerProfiles.ForEach(playerProfile =>
        {
            profileList.Add(CreatePlayerProfileEntry(playerProfile, index));
            index++;
        });

        StyleSheetControl.Instance.UpdateThemeSpecificStyleSheets();
    }

    private VisualElement CreatePlayerProfileEntry(PlayerProfile playerProfile, int indexInList)
    {
        VisualElement result = playerProfileListEntryAsset.CloneTree();

        Button deleteButton = result.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.text = TranslationManager.GetTranslation(R.Messages.delete);
        deleteButton.RegisterCallbackButtonTriggered(() =>
            {
                settings.PlayerProfiles.RemoveAt(indexInList);
                UpdatePlayerProfileList();
                backButton.Focus();
            });

        TextField nameTextField = result.Q<TextField>(R.UxmlNames.nameTextField);
        nameTextField.value = playerProfile.Name;
        nameTextField.RegisterValueChangedCallback(evt => playerProfile.Name = evt.newValue);

        Toggle enabledToggle = result.Q<Toggle>(R.UxmlNames.enabledToggle);
        enabledToggle.value = playerProfile.IsEnabled;
        enabledToggle.RegisterValueChangedCallback(evt => playerProfile.IsEnabled = evt.newValue);
        result.Q<Label>(R.UxmlNames.enabledLabel).text = TranslationManager.GetTranslation(R.Messages.active);

        new AvatarPickerControl(result.Q<ItemPicker>(R.UxmlNames.avatarPicker), uiManager)
            .Bind(() => playerProfile.Avatar,
                newValue => playerProfile.Avatar = newValue);

        new DifficultyPicker(result.Q<ItemPicker>(R.UxmlNames.difficultyPicker))
            .Bind(() => playerProfile.Difficulty,
                newValue => playerProfile.Difficulty = newValue);

        return result;
    }
}
