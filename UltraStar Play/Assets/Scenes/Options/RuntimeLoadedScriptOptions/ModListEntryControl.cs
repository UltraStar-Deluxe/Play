using UniInject;
using UnityEngine.UIElements;

public class ModListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    public Settings settings;

    [Inject]
    public RuntimeLoadedScriptManager runtimeLoadedScriptManager;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.enabledToggle)]
    private SlideToggle enabledToggle;

    [Inject(UxmlName = R.UxmlNames.modNameLabel)]
    private Label modNameLabel;

    [Inject(UxmlName = R.UxmlNames.modListEntryInactiveOverlay)]
    private VisualElement modListEntryInactiveOverlay;

    [Inject(Key = "modFolder")]
    public string ModFolder { get; private set; }

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

        UpdateInactiveOverlay();
    }

    private void UpdateInactiveOverlay()
    {
        modListEntryInactiveOverlay.SetInClassList("hidden", IsModEnabled);
    }
}
