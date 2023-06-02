using System.Collections.Generic;
using ProTrans;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DesignOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject]
    private ThemeManager themeManager;

    [Inject(UxmlName = R.UxmlNames.themePicker)]
    private ItemPicker themePicker;

    [Inject(UxmlName = R.UxmlNames.imageAsCursorPicker)]
    private ItemPicker imageAsCursorPicker;

    [Inject(UxmlName = R.UxmlNames.sceneChangeAnimationPicker)]
    private ItemPicker sceneChangeAnimationPicker;

    [Inject(UxmlName = R.UxmlNames.sceneChangeDurationPicker)]
    private ItemPicker sceneChangeDurationPicker;
    
    [Inject(UxmlName = R.UxmlNames.backgroundLightItemPicker)]
    private ItemPicker backgroundLightItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.vfxEnabledPicker)]
    private ItemPicker vfxEnabledPicker;

    [Inject(UxmlName = R.UxmlNames.showScrollBarInSongSelectPicker)]
    private ItemPicker showScrollBarInSongSelectPicker;
    
    [Inject(UxmlName = R.UxmlNames.showSongIndexInSongSelectPicker)]
    private ItemPicker showSongIndexInSongSelectPicker;
    
    [Inject(UxmlName = R.UxmlNames.songBackgroundScaleModePicker)]
    private ItemPicker songBackgroundScaleModePicker;
    
    [Inject]
    private UiManager uiManager;
    
    [Inject]
    private BackgroundLightManager backgroundLightManager;

    protected override void Start()
    {
        base.Start();
        
        new BoolPickerControl(imageAsCursorPicker)
            .Bind(() => settings.UseImageAsCursor,
                newValue => settings.UseImageAsCursor = newValue);

        new LabeledItemPickerControl<ESceneChangeAnimation>(sceneChangeAnimationPicker, EnumUtils.GetValuesAsList<ESceneChangeAnimation>())
            .Bind(() => settings.SceneChangeAnimation,
                newValue => settings.SceneChangeAnimation = newValue);

        new BoolPickerControl(vfxEnabledPicker)
            .Bind(() => settings.EnableVfx, 
                newValue => settings.EnableVfx = newValue);

        new BoolPickerControl(showScrollBarInSongSelectPicker)
            .Bind(() => settings.ShowScrollBarInSongSelect, 
                newValue => settings.ShowScrollBarInSongSelect = newValue);
        
        new BoolPickerControl(showSongIndexInSongSelectPicker)
            .Bind(() => settings.ShowSongIndexInSongSelect, 
                newValue => settings.ShowSongIndexInSongSelect = newValue);

        LabeledItemPickerControl<ESongBackgroundScaleMode> songBackgroundScaleModePickerControl = new LabeledItemPickerControl<ESongBackgroundScaleMode>(songBackgroundScaleModePicker, EnumUtils.GetValuesAsList<ESongBackgroundScaleMode>());
        songBackgroundScaleModePickerControl.Bind(() => settings.SongBackgroundScaleMode,
            newValue => settings.SongBackgroundScaleMode = newValue);
        songBackgroundScaleModePickerControl.GetLabelTextFunction = item => StringUtils.ToTitleCase(ObjectUtils.NullableToString(item, ""));

        LabeledItemPickerControl<float> sceneChangeDurationPickerControl = new(sceneChangeDurationPicker, NumberUtils.CreateFloatList(0, 0.9f, 0.05f));
        sceneChangeDurationPickerControl.Bind(() => settings.SceneChangeDurationInSeconds,
                newValue => settings.SceneChangeDurationInSeconds = newValue);
        sceneChangeDurationPickerControl.GetLabelTextFunction = newValue => $"{newValue.ToStringInvariantCulture("0.00")} s";
        
        new LabeledItemPickerControl<int>(backgroundLightItemPicker, NumberUtils.CreateIntList(0, backgroundLightManager.BackgroundLightInstancesCount))
            .Bind(() => settings.BackgroundLightIndex,
                newValue => settings.BackgroundLightIndex = newValue);
        
        // Load available themes:
        List<ThemeMeta> themeMetas = themeManager.GetThemeMetas();
        LabeledItemPickerControl<ThemeMeta> themePickerControl = new(themePicker, themeMetas);
        themePickerControl.GetLabelTextFunction = themeMeta => ThemeMetaUtils.GetDisplayName(themeMeta);
        themePickerControl.Bind(
            () => themeManager.GetCurrentTheme(),
            newValue => ChangeTheme(newValue));
    }

    private void ChangeTheme(ThemeMeta themeMeta)
    {
        if (themeManager.GetCurrentTheme() == themeMeta)
        {
            return;
        }
        ApplyThemeStyleUtils.ClearCache();
        themeManager.SetCurrentTheme(themeMeta);
    }

    public void UpdateTranslation()
    {
        themePicker.Label = TranslationManager.GetTranslation(R.Messages.options_design_theme);
        imageAsCursorPicker.Label = TranslationManager.GetTranslation(R.Messages.options_useImageAsCursor);
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_customThemes_title),
                TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_customThemes,
                    "path", ApplicationUtils.ReplacePathsWithDisplayString(ThemeManager.GetAbsoluteUserDefinedThemesFolder())) },
        };
         MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_title),
            titleToContentMap);

        helpDialogControl.AddButton("Custom Themes Folder",
            _ => ApplicationUtils.OpenDirectory(ThemeManager.GetAbsoluteUserDefinedThemesFolder()));

        if (PlatformUtils.IsStandalone)
        {
            helpDialogControl.AddButton("Default Themes Folder",
                        _ => ApplicationUtils.OpenDirectory(ThemeManager.GetAbsoluteDefaultThemesFolder()));
        }
        return helpDialogControl;
    }
}
