using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerProfileOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection
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

        addButton.RegisterCallbackButtonTriggered(_ =>
        {
            PlayerProfile newPlayerProfile = new PlayerProfile();
            newPlayerProfile.Name = GetNewPlayerProfileName();
            settings.PlayerProfiles.Add(newPlayerProfile);
            VisualElement playerProfileEntryVisualElement = CreatePlayerProfileEntry(newPlayerProfile);
            playerProfileEntryVisualElement.RegisterHasGeometryCallbackOneShot(_ => playerProfileEntryVisualElement.ScrollToSelf());

            // Focus on the name of the newly added player to directly allow changing its name
            TextField nameTextField = playerProfileList[playerProfileList.childCount - 1].Q<TextField>("nameTextField");
            nameTextField.DisableParseEscapeSequences();
            nameTextField.Focus();
            
            ThemeManager.ApplyThemeSpecificStylesToVisualElements(playerProfileList);
        });
    }

    private string GetNewPlayerProfileName()
    {
        bool ExistsPlayerProfileWithName(string newName)
        {
            return settings.PlayerProfiles.Any(playerProfile =>
            {
                string nameWithoutWhiteSpace = playerProfile.Name.Replace(" ", "");
                string newNameWithoutWhiteSpace = newName.Replace(" ", "");
                return nameWithoutWhiteSpace.Equals(newNameWithoutWhiteSpace, StringComparison.InvariantCultureIgnoreCase);
            });
        }
        
        int index = 1;
        string playerProfileName = $"Player{index:00}";
        while (ExistsPlayerProfileWithName(playerProfileName))
        {
            index++;
            playerProfileName = $"Player{index:00}";
        }

        return playerProfileName;
    }

    private void UpdatePlayerProfileList()
    {
        playerProfileList.Clear();
        settings.PlayerProfiles.ForEach(playerProfile => CreatePlayerProfileEntry(playerProfile));

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
    
    private int GetIndexInList(PlayerProfile playerProfile)
    {
        // Dynamically return index in list because the list can change while the scene is open.
        return settings.PlayerProfiles.IndexOf(playerProfile);
    }
    
    private VisualElement CreatePlayerProfileEntry(PlayerProfile playerProfile)
    {
        VisualElement visualElement = playerProfileListEntryAsset.CloneTree().Children().FirstOrDefault();

        VisualElement playerProfileInactiveOverlay = visualElement.Q<VisualElement>(R.UxmlNames.playerProfileInactiveOverlay);

        Button deleteButton = visualElement.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (GetIndexInList(playerProfile) < settings.PlayerProfiles.Count)
            {
                settings.PlayerProfiles.RemoveAt(GetIndexInList(playerProfile));
            }
            visualElement.RemoveFromHierarchy();
        });

        TextField nameTextField = visualElement.Q<TextField>(R.UxmlNames.nameTextField);
        nameTextField.DisableParseEscapeSequences();
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

        new PlayerProfileImagePickerControl(visualElement.Q<ItemPicker>(R.UxmlNames.playerProfileImagePicker), GetIndexInList(playerProfile), uiManager, webCamManager)
            .Bind(() => playerProfile.ImagePath,
                newValue => playerProfile.ImagePath = newValue);

        new DifficultyPicker(visualElement.Q<ItemPicker>(R.UxmlNames.difficultyPicker))
            .Bind(() => playerProfile.Difficulty,
                newValue => playerProfile.Difficulty = newValue);

        playerProfileList.Add(visualElement);

        return visualElement;
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_activateProfile_title),
                TranslationManager.GetTranslation(R.Messages.options_playerProfiles_helpDialog_activateProfile) },
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
            _ => ApplicationUtils.OpenDirectory(PlayerProfileUtils.GetAbsolutePlayerProfileImagesFolder()));
        return helpDialogControl;
    }
}
