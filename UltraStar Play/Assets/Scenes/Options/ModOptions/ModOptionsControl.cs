using System.Collections.Generic;
using UniInject;
using UniInject.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ModOptionsControl : AbstractOptionsSceneControl, INeedInjection, IBinder
{
    [InjectedInInspector]
    public VisualTreeAsset modEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset modInfoDialogUi;

    [Inject]
    private Injector injector;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private ModManager modManager;

    [Inject(UxmlName = R.UxmlNames.modReloadOnChangeToggle)]
    private Toggle modReloadOnChangeToggle;

    [Inject(UxmlName = R.UxmlNames.modList)]
    private VisualElement modList;

    private readonly List<ModListEntryControl> modListEntryControls = new();

    protected override void Start()
    {
        base.Start();
        UpdateModList();
    }

    private void UpdateModList()
    {
        modList.Clear();

        FieldBindingUtils.Bind(modReloadOnChangeToggle,
            () => settings.ReloadModsOnFileChange,
            newValue => settings.ReloadModsOnFileChange = newValue);

        List<ModFolder> modFolders = ModManager.GetModFolders();
        if (modFolders.IsNullOrEmpty())
        {
            modList.Add(new Label("No mods found."));
            return;
        }

        modFolders.ForEach(modFolder =>
        {
            ModListEntryControl modListEntryControl = CreateModListEntry(modFolder);
            modList.Add(modListEntryControl.VisualElement);
            modListEntryControls.Add(modListEntryControl);
        });
    }

    private ModListEntryControl CreateModListEntry(ModFolder modFolder)
    {
        VisualElement modEntryVisualElement = modEntryUi.CloneTreeAndGetFirstChild();
        ModListEntryControl modListEntryControl = injector
            .CreateChildInjector()
            .WithRootVisualElement(modEntryVisualElement)
            .WithBindingForInstance(modFolder)
            .CreateAndInject<ModListEntryControl>();

        return modListEntryControl;
    }

    public override string SteamWorkshopUri => "https://steamcommunity.com/workshop/browse/?appid=2394070&requiredtags[]=Mod";

    public override string HelpUri => Translation.Get(R.Messages.uri_howToMods);

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.Bind(nameof(modInfoDialogUi)).ToExistingInstance(modInfoDialogUi);
        return bb.GetBindings();
    }
}
