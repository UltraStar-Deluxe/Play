using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class ModListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private ModManager modManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private GameObject gameObject;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.enabledToggle)]
    private SlideToggle enabledToggle;

    [Inject(UxmlName = R.UxmlNames.modNameLabel)]
    private Label modNameLabel;

    [Inject(UxmlName = R.UxmlNames.modListEntryInactiveOverlay)]
    private VisualElement modListEntryInactiveOverlay;

    [Inject(UxmlName = R.UxmlNames.modInfoButton)]
    private Button modInfoButton;

    [Inject(UxmlName = R.UxmlNames.modSettingsButton)]
    private Button modSettingsButton;

    [Inject(UxmlName = R.UxmlNames.openModFolderButton)]
    private Button openModFolderButton;

    [Inject(UxmlName = R.UxmlNames.warningContainer)]
    private VisualElement warningContainer;

    [Inject(Key = "modFolder")]
    public string ModFolder { get; private set; }

    [Inject(Key = nameof(modInfoDialogUi))]
    private VisualTreeAsset modInfoDialogUi;

    private string modDisplayName;
    private string ModDisplayName
    {
        get
        {
            if (modDisplayName.IsNullOrEmpty())
            {
                modDisplayName = ModManager.GetModDisplayName(ModFolder);
            }

            return modDisplayName;
        }
    }

    private bool IsModEnabled => modManager.IsModEnabled(ModFolder);

    private MessageDialogControl modInfoDialogControl;
    private MessageDialogControl modSettingsDialogControl;

    public void OnInjectionFinished()
    {
        modListEntryInactiveOverlay.ShowByDisplay();
        modNameLabel.SetTranslatedText(Translation.Of(ModDisplayName));

        enabledToggle.value = IsModEnabled;
        enabledToggle.RegisterValueChangedCallback(evt =>
        {
            modManager.SetModEnabled(ModFolder, evt.newValue);
            UpdateInactiveOverlay();
            UpdateWarning();
        });

        modInfoButton.RegisterCallbackButtonTriggered(_ => ShowModInfoDialog());
        modSettingsButton.RegisterCallbackButtonTriggered(_ => ShowModSettingsDialog());
        openModFolderButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(ModFolder));

        modManager.ObserveEveryValueChanged(it => it.FailedToLoadModFolders.Count)
            .Subscribe(_ => UpdateWarning())
            .AddTo(gameObject);

        UpdateInactiveOverlay();
        UpdateWarning();
    }

    private void UpdateWarning()
    {
        warningContainer.SetVisibleByDisplay(
            IsModEnabled
            && modManager.FailedToLoadModFolders.Contains(ModFolder));
    }

    private void ShowModSettingsDialog()
    {
        if (!IsModEnabled)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.mod_error_settingsNotAvailable));
            return;
        }

        if (modSettingsDialogControl != null)
        {
            return;
        }

        List<IModSettings> allModSettings = ModManager.GetModObjects<IModSettings>(ModFolder);
        if (allModSettings.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.mod_error_settingsEmpty));
            return;
        }

        List<IModSettingControl> allModSettingControls = allModSettings
            .SelectMany(modSettings => modSettings.GetModSettingControls())
            .ToList();
        if (allModSettingControls.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.mod_error_settingsEmpty));
            return;
        }

        modSettingsDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.options_mod_settingsDialog_title,
            "modName", ModDisplayName));
        modSettingsDialogControl.AddButton(Translation.Get(R.Messages.action_close),
            _ => modSettingsDialogControl.CloseDialog());
        modSettingsDialogControl.DialogClosedEventStream.Subscribe(_ => modSettingsDialogControl = null);

        VisualElement modControlContainer = new();
        modSettingsDialogControl.AddVisualElement(modControlContainer);
        modControlContainer.AddToClassList("child-mb-3");

        foreach (IModSettings currentModSettings in allModSettings)
        {
            List<IModSettingControl> modSettingControls = currentModSettings.GetModSettingControls();
            foreach (IModSettingControl modSettingControl in modSettingControls)
            {
                modControlContainer.Add(modSettingControl.CreateVisualElement());
            }
        }
    }

    private void ShowModInfoDialog()
    {
        if (modInfoDialogControl != null)
        {
            return;
        }

        modInfoDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.options_mod_infoDialog_title,
            "modName", ModDisplayName));
        modInfoDialogControl.AddButton(Translation.Get(R.Messages.action_close),
            _ => modInfoDialogControl.CloseDialog());
        modInfoDialogControl.DialogClosedEventStream.Subscribe(_ => modInfoDialogControl = null);

        ModInfo modInfo = ModManager.GetModInfo(ModFolder);
        if (modInfo == null)
        {
            string noModInfoText = "No description available." +
                                   $"\nAdd a {ModManager.ModInfoFileName} file to provide information about the mod.";
            modInfoDialogControl.AddVisualElement(new Label(noModInfoText));
            return;
        }

        VisualElement modInfoDialogVisualElement = modInfoDialogUi.CloneTreeAndGetFirstChild();
        modInfoDialogControl.AddVisualElement(modInfoDialogVisualElement);
        Label descriptionLabel = modInfoDialogVisualElement.Q<Label>(R.UxmlNames.modDescriptionLabel);
        Label versionLabel = modInfoDialogVisualElement.Q<Label>(R.UxmlNames.modVersionLabel);
        Label websiteLabel = modInfoDialogVisualElement.Q<Label>(R.UxmlNames.modWebsiteLabel);
        Label authorsLabel = modInfoDialogVisualElement.Q<Label>(R.UxmlNames.modAuthorsLabel);
        VisualElement modDependenciesContainer = modInfoDialogVisualElement.Q<VisualElement>(R.UxmlNames.modDependenciesContainer);

        SetTextAndVisibility(descriptionLabel, false, Translation.Of(modInfo.description));
        SetTextAndVisibility(versionLabel, modInfo.version.IsNullOrEmpty(),
            Translation.Get(R.Messages.options_mod_infoDialog_version, "value", modInfo.version));
        SetTextAndVisibility(websiteLabel, modInfo.website.IsNullOrEmpty(),
            Translation.Get(R.Messages.options_mod_infoDialog_website, "value", modInfo.website));
        SetTextAndVisibility(websiteLabel, modInfo.license.IsNullOrEmpty(),
            Translation.Get(R.Messages.options_mod_infoDialog_license, "value", modInfo.license));
        SetTextAndVisibility(authorsLabel, modInfo.authors.IsNullOrEmpty(),
            Translation.Get(R.Messages.options_mod_infoDialog_authors, "value", modInfo.authors.JoinWith(", ")));

        // TODO: Mod permissions like FileSystem, Networking, etc.
        modDependenciesContainer.Clear();
        // if (modInfo.requiredAssemblies.IsNullOrEmpty())
        // {
        //     modDependenciesContainer.Add(new Label($"Requires default app domain libraries."));
        // }
        // else
        // {
        //     modDependenciesContainer.Add(new Label($"Requires default app domain libraries and the following"));
        //     foreach (string require in modInfo.requiredAssemblies)
        //     {
        //         modDependenciesContainer.Add(new Label($"• {require}"));
        //     }
        // }
    }

    private void SetTextAndVisibility(Label label, bool hide, Translation text)
    {
        if (hide)
        {
            label.HideByDisplay();
            label.SetTranslatedText(Translation.Empty);
            return;
        }

        label.ShowByDisplay();
        label.SetTranslatedText(text);
    }

    private void UpdateInactiveOverlay()
    {
        modListEntryInactiveOverlay.SetInClassList("hidden", IsModEnabled);
    }
}
