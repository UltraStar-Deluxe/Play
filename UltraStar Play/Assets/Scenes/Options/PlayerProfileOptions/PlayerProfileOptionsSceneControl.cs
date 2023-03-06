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

        addButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.PlayerProfiles.Add(new PlayerProfile());
            CreatePlayerProfileEntry(settings.PlayerProfiles.FirstOrDefault(), settings.PlayerProfiles.Count - 1);

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
            CreatePlayerProfileEntry(playerProfile, index);
            index++;
        });

        ThemeManager.ApplyThemeSpecificStylesToVisualElements(playerProfileList);
    }

    private void UpdatePlayerProfileInactiveOverlay(PlayerProfile playerProfile, VisualElement playerProfileInactiveOverlay)
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
    
    private void CreatePlayerProfileEntry(PlayerProfile playerProfile, int indexInList)
    {
        VisualElement visualElement = playerProfileListEntryAsset.CloneTree().Children().FirstOrDefault();

        VisualElement playerProfileInactiveOverlay = visualElement.Q<VisualElement>(R.UxmlNames.playerProfileInactiveOverlay);

        Button deleteButton = visualElement.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.PlayerProfiles.RemoveAt(indexInList);
            visualElement.RemoveFromHierarchy();
        });

        TextField nameTextField = visualElement.Q<TextField>(R.UxmlNames.nameTextField);
        nameTextField.value = playerProfile.Name;
        nameTextField.RegisterValueChangedCallback(evt => playerProfile.Name = evt.newValue);

        SlideToggle enabledToggle = visualElement.Q<SlideToggle>(R.UxmlNames.enabledToggle);
        enabledToggle.value = playerProfile.IsEnabled;
        enabledToggle.RegisterValueChangedCallback(evt =>
        {
            playerProfile.IsEnabled = evt.newValue;
            UpdatePlayerProfileInactiveOverlay(playerProfile, playerProfileInactiveOverlay);
        });
        UpdatePlayerProfileInactiveOverlay(playerProfile, playerProfileInactiveOverlay);

        new PlayerProfileImagePickerControl(visualElement.Q<ItemPicker>(R.UxmlNames.playerProfileImagePicker), indexInList, uiManager, webCamManager)
            .Bind(() => playerProfile.ImagePath,
                newValue => playerProfile.ImagePath = newValue);

        new DifficultyPicker(visualElement.Q<ItemPicker>(R.UxmlNames.difficultyPicker))
            .Bind(() => playerProfile.Difficulty,
                newValue => playerProfile.Difficulty = newValue);

        playerProfileList.Add(visualElement);
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
