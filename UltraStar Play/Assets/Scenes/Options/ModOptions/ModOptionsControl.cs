using System.Collections.Generic;
using ProTrans;
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

        List<string> modFolders = modManager.GetModFolders();
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

    private ModListEntryControl CreateModListEntry(string modFolder)
    {
        VisualElement modEntryVisualElement = modEntryUi.CloneTreeAndGetFirstChild();
        ModListEntryControl modListEntryControl = injector
            .CreateChildInjector()
            .WithRootVisualElement(modEntryVisualElement)
            .WithBinding(new UniInjectBinding("modFolder", new ExistingInstanceProvider<string>(modFolder)))
            .CreateAndInject<ModListEntryControl>();

        return modListEntryControl;
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_intro_title),
                TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_intro) },
            { TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_install_title),
                TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_install,
                    "modsRootFolderPath", ModManager.GetAbsoluteUserDefinedModsRootFolder()) },
            { TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_developMods_title),
                TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_developMods) },
            { TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_modLoading_title),
                TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_modLoading) },
        };
        MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_mod_helpDialog_title),
            titleToContentMap);
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.action_openModsRootFolder),
            _ => ApplicationUtils.OpenDirectory(ModManager.GetAbsoluteUserDefinedModsRootFolder()));
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToMods)));
        return helpDialogControl;
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.Bind(nameof(modInfoDialogUi)).ToExistingInstance(modInfoDialogUi);
        return bb.GetBindings();
    }
}
