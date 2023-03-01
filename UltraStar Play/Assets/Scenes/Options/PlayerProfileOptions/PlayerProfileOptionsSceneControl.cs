using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerProfileOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset playerProfileListEntryAsset;

    [Inject(UxmlName = R.UxmlNames.playerProfileList)]
    private ScrollView playerProfileList;

    [Inject(UxmlName = R.UxmlNames.addButton)]
    private Button addButton;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private WebCamManager webCamManager;

    protected override void Start()
    {
        base.Start();
        
        UpdatePlayerProfileList();
        settings.ObserveEveryValueChanged(s => s.PlayerProfiles)
            .Subscribe(onNext => UpdatePlayerProfileList())
            .AddTo(gameObject);

        addButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.PlayerProfiles.Add(new PlayerProfile());
            UpdatePlayerProfileList();

            // Focus on the name of the newly added player to directly allow changing its name
            TextField nameTextField = playerProfileList[playerProfileList.childCount-1].Q<TextField>("nameTextField");
            nameTextField.Focus();
        });
    }

    public void UpdateTranslation()
    {
    }

    private void UpdatePlayerProfileList()
    {
        playerProfileList.Clear();
        int index = 0;
        settings.PlayerProfiles.ForEach(playerProfile =>
        {
            playerProfileList.Add(CreatePlayerProfileEntry(playerProfile, index));
            index++;
        });

        ThemeManager.ApplyThemeSpecificStylesToVisualElementsInScene();
    }

    private VisualElement CreatePlayerProfileEntry(PlayerProfile playerProfile, int indexInList)
    {
        VisualElement result = playerProfileListEntryAsset.CloneTree().Children().FirstOrDefault();

        VisualElement playerProfileInactiveOverlay = result.Q<VisualElement>(R.UxmlNames.playerProfileInactiveOverlay);

        void UpdatePlayerProfileInactiveOverlay()
        {
            playerProfileInactiveOverlay.ShowByDisplay();
            if (playerProfile.IsEnabled)
            {
                playerProfileInactiveOverlay.style.backgroundColor = new StyleColor(Colors.clearBlack);
            }
            else
            {
                playerProfileInactiveOverlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.5f));
            }
        }

        Button deleteButton = result.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.RegisterCallbackButtonTriggered(() =>
            {
                settings.PlayerProfiles.RemoveAt(indexInList);
                UpdatePlayerProfileList();
            });

        TextField nameTextField = result.Q<TextField>(R.UxmlNames.nameTextField);
        nameTextField.value = playerProfile.Name;
        nameTextField.RegisterValueChangedCallback(evt => playerProfile.Name = evt.newValue);

        SlideToggle enabledToggle = result.Q<SlideToggle>(R.UxmlNames.enabledToggle);
        enabledToggle.value = playerProfile.IsEnabled;
        enabledToggle.RegisterValueChangedCallback(evt =>
        {
            playerProfile.IsEnabled = evt.newValue;
            UpdatePlayerProfileInactiveOverlay();
        });
        UpdatePlayerProfileInactiveOverlay();

        new PlayerProfileImagePickerControl(result.Q<ItemPicker>(R.UxmlNames.playerProfileImagePicker), indexInList, uiManager, webCamManager)
            .Bind(() => playerProfile.ImagePath,
                newValue => playerProfile.ImagePath = newValue);

        new DifficultyPicker(result.Q<ItemPicker>(R.UxmlNames.difficultyPicker))
            .Bind(() => playerProfile.Difficulty,
                newValue => playerProfile.Difficulty = newValue);

        return result;
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_activateProfile_title),
                TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_activateProfile) },
            { TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_difficulty_title),
                TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_difficulty) },
            { TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_webcamProfileImages_title),
                TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_webcamProfileImages) },
            { TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_customProfileImages_title),
                TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_customProfileImages,
                    "path", ApplicationUtils.ReplacePathsWithDisplayString(PlayerProfileUtils.GetAbsolutePlayerProfileImagesFolder())) },
        };
        MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_title),
            titleToContentMap);
        helpDialogControl.AddButton("Images Folder",
            () => ApplicationUtils.OpenDirectory(PlayerProfileUtils.GetAbsolutePlayerProfileImagesFolder()));
        return helpDialogControl;
    }
}
