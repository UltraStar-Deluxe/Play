using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class ModListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    public Settings settings;

    [Inject]
    public RuntimeLoadedScriptManager runtimeLoadedScriptManager;

    [Inject]
    public UiManager uiManager;

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

    [Inject(Key = "modFolder")]
    public string ModFolder { get; private set; }

    [Inject(Key = nameof(modInfoDialogUi))]
    private VisualTreeAsset modInfoDialogUi;

    private string modName;
    private string ModName
    {
        get
        {
            if (modName.IsNullOrEmpty())
            {
                modName = RuntimeLoadedScriptManager.GetModName(ModFolder);
            }

            return modName;
        }
    }

    private bool IsModEnabled => runtimeLoadedScriptManager.IsModEnabled(ModFolder);

    private MessageDialogControl modInfoDialogControl;
    private MessageDialogControl modSettingsDialogControl;

    public void OnInjectionFinished()
    {
        modListEntryInactiveOverlay.ShowByDisplay();
        modNameLabel.text = ModName;

        enabledToggle.value = IsModEnabled;
        enabledToggle.RegisterValueChangedCallback(evt =>
        {
            settings.EnabledRuntimeLoadedMods.Remove(ModName);
            if (evt.newValue)
            {
                settings.EnabledRuntimeLoadedMods.Add(ModName);
            }
            UpdateInactiveOverlay();
        });

        modInfoButton.RegisterCallbackButtonTriggered(_ => ShowModInfoDialog());

        UpdateInactiveOverlay();
    }

    private void ShowModInfoDialog()
    {
        if (modInfoDialogControl != null)
        {
            return;
        }

        modInfoDialogControl = uiManager.CreateDialogControl($"{modName}");
        modInfoDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.close),
            _ => modInfoDialogControl.CloseDialog());
        modInfoDialogControl.DialogClosedEventStream.Subscribe(_ =>
        {
            modInfoDialogControl = null;
        });

        ModInfoJson modInfoJson = RuntimeLoadedScriptManager.GetModInfo(ModFolder);
        if (modInfoJson == null)
        {
            string noModInfoText = "No description available." +
                                   "\nAdd a modinfo.json file to provide information about the mod.";
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

        SetTextOrHideLabel(descriptionLabel, "",modInfoJson.description);
        SetTextOrHideLabel(versionLabel, "Version: " , modInfoJson.version);
        SetTextOrHideLabel(websiteLabel, "Website: " , modInfoJson.website);
        SetTextOrHideLabel(authorsLabel, "Authors: " , modInfoJson.authors.ToCsv(", ", "", ""));

        modDependenciesContainer.Clear();
        foreach (string require in modInfoJson.requires)
        {
            modDependenciesContainer.Add(new Label($"• {require}"));
        }
    }

    private void SetTextOrHideLabel(Label label, string prefix, string text)
    {
        if (text.IsNullOrEmpty())
        {
            label.HideByDisplay();
            return;
        }

        label.ShowByDisplay();
        label.text = $"{prefix}{text}";
    }

    private void UpdateInactiveOverlay()
    {
        modListEntryInactiveOverlay.SetInClassList("hidden", IsModEnabled);
    }
}
