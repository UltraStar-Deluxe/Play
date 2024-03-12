using System.Collections.Generic;
using ProTrans;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DesignOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject]
    private ThemeManager themeManager;

    [Inject(UxmlName = R.UxmlNames.themePicker)]
    private ItemPicker themePicker;

    [Inject(UxmlName = R.UxmlNames.imageAsCursorToggle)]
    private Toggle imageAsCursorToggle;

    [Inject(UxmlName = R.UxmlNames.sceneChangeAnimationPicker)]
    private ItemPicker sceneChangeAnimationPicker;

    [Inject(UxmlName = R.UxmlNames.sceneChangeDurationPicker)]
    private ItemPicker sceneChangeDurationPicker;

    [Inject(UxmlName = R.UxmlNames.backgroundLightItemPicker)]
    private ItemPicker backgroundLightItemPicker;

    [Inject(UxmlName = R.UxmlNames.showScrollBarInSongSelectToggle)]
    private Toggle showScrollBarInSongSelectToggle;

    [Inject(UxmlName = R.UxmlNames.showSongIndexInSongSelectToggle)]
    private Toggle showSongIndexInSongSelectToggle;

    [Inject(UxmlName = R.UxmlNames.navigateFoldersInSongSelectToggle)]
    private Toggle navigateFoldersInSongSelectToggle;

    [Inject(UxmlName = R.UxmlNames.songBackgroundScaleModePicker)]
    private ItemPicker songBackgroundScaleModePicker;

    [Inject(UxmlName = R.UxmlNames.previewFadeInDurationChooser)]
    private ItemPicker previewFadeInDurationChooser;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private BackgroundLightManager backgroundLightManager;

    protected override void Start()
    {
        base.Start();

        FieldBindingUtils.Bind(imageAsCursorToggle,
            () => settings.UseImageAsCursor,
            newValue => settings.UseImageAsCursor = newValue);

        new EnumItemPickerControl<ESceneChangeAnimation>(sceneChangeAnimationPicker)
            .Bind(() => settings.SceneChangeAnimation,
                newValue => settings.SceneChangeAnimation = newValue);

        FieldBindingUtils.Bind(showScrollBarInSongSelectToggle,
            () => settings.ShowScrollBarInSongSelect,
            newValue => settings.ShowScrollBarInSongSelect = newValue);

        FieldBindingUtils.Bind(showSongIndexInSongSelectToggle,
            () => settings.ShowSongIndexInSongSelect,
            newValue => settings.ShowSongIndexInSongSelect = newValue);

        FieldBindingUtils.Bind(navigateFoldersInSongSelectToggle,
            () => settings.NavigateByFoldersInSongSelect,
            newValue => settings.NavigateByFoldersInSongSelect = newValue);

        LabeledItemPickerControl<float> audioPreviewFadeInDurationChooserControl = new(previewFadeInDurationChooser, NumberUtils.CreateFloatList(0.5f, 5f, 0.5f));
        audioPreviewFadeInDurationChooserControl.Bind(() => settings.PreviewFadeInDurationInSeconds,
            newValue => settings.PreviewFadeInDurationInSeconds = newValue);
        audioPreviewFadeInDurationChooserControl.GetLabelTextFunction = newValue => $"{newValue.ToStringInvariantCulture("0.00")} s";

        EnumItemPickerControl<ESongBackgroundScaleMode> songBackgroundScaleModePickerControl = new EnumItemPickerControl<ESongBackgroundScaleMode>(songBackgroundScaleModePicker);
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
    }

    public override string SteamWorkshopUri => "https://steamcommunity.com/workshop/browse/?appid=2394070&requiredtags[]=Theme";

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_customThemes_title),
                TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_customThemes,
                    "path", ApplicationUtils.ReplacePathsWithDisplayString(ThemeFolderUtils.GetUserDefinedThemesFolderAbsolutePath())) },
        };
         MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_title),
            titleToContentMap);

        helpDialogControl.AddButton("Custom Themes Folder",
            _ => ApplicationUtils.OpenDirectory(ThemeFolderUtils.GetUserDefinedThemesFolderAbsolutePath()));

        if (PlatformUtils.IsStandalone)
        {
            helpDialogControl.AddButton("Default Themes Folder",
                        _ => ApplicationUtils.OpenDirectory(ThemeFolderUtils.GetUserDefinedThemesFolderAbsolutePath()));
        }
        return helpDialogControl;
    }
}
