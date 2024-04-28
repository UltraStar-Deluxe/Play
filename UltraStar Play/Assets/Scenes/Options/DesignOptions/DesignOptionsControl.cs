using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DesignOptionsControl : AbstractOptionsSceneControl, INeedInjection
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

        LabeledItemPickerControl<float> audioPreviewFadeInDurationChooserControl = new(previewFadeInDurationChooser,
            NumberUtils.CreateFloatList(0.5f, 5f, 0.5f),
            newValue => Translation.Of($"{newValue.ToStringInvariantCulture("0.00")} s"));
        audioPreviewFadeInDurationChooserControl.Bind(() => settings.PreviewFadeInDurationInSeconds,
            newValue => settings.PreviewFadeInDurationInSeconds = newValue);

        EnumItemPickerControl<ESongBackgroundScaleMode> songBackgroundScaleModePickerControl = new(songBackgroundScaleModePicker);
        songBackgroundScaleModePickerControl.Bind(() => settings.SongBackgroundScaleMode,
            newValue => settings.SongBackgroundScaleMode = newValue);

        LabeledItemPickerControl<float> sceneChangeDurationPickerControl = new(sceneChangeDurationPicker,
            NumberUtils.CreateFloatList(0, 0.9f, 0.05f),
            newValue => Translation.Of($"{newValue.ToStringInvariantCulture("0.00")} s"));
        sceneChangeDurationPickerControl.Bind(() => settings.SceneChangeDurationInSeconds,
                newValue => settings.SceneChangeDurationInSeconds = newValue);

        new LabeledItemPickerControl<int>(backgroundLightItemPicker,
                NumberUtils.CreateIntList(0, backgroundLightManager.BackgroundLightInstancesCount),
                item => Translation.Of(item.ToString()))
            .Bind(() => settings.BackgroundLightIndex,
                newValue => settings.BackgroundLightIndex = newValue);

        // Load available themes:
        List<ThemeMeta> themeMetas = themeManager.GetThemeMetas();
        LabeledItemPickerControl<ThemeMeta> themePickerControl = new(themePicker, themeMetas,
            themeMeta => Translation.Of(ThemeMetaUtils.GetDisplayName(themeMeta)));
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

    public override string SteamWorkshopUri => "https://steamcommunity.com/workshop/browse/?appid=2394070&requiredtags[]=Theme";

    public override string HelpUri => Translation.Get(R.Messages.uri_howToThemes);
}
