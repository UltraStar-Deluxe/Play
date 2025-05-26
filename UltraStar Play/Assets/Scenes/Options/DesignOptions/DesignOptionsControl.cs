using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DesignOptionsControl : AbstractOptionsSceneControl, INeedInjection
{
    [Inject]
    private ThemeManager themeManager;

    [Inject(UxmlName = R.UxmlNames.themeChooser)]
    private Chooser themeChooser;

    [Inject(UxmlName = R.UxmlNames.imageAsCursorToggle)]
    private Toggle imageAsCursorToggle;

    [Inject(UxmlName = R.UxmlNames.sceneChangeAnimationChooser)]
    private Chooser sceneChangeAnimationChooser;

    [Inject(UxmlName = R.UxmlNames.sceneChangeDurationChooser)]
    private Chooser sceneChangeDurationChooser;

    [Inject(UxmlName = R.UxmlNames.songBackgroundScaleModeChooser)]
    private Chooser songBackgroundScaleModeChooser;

    [Inject(UxmlName = R.UxmlNames.previewFadeInDurationChooser)]
    private Chooser previewFadeInDurationChooser;

    [Inject]
    private UiManager uiManager;

    protected override void Start()
    {
        base.Start();

        FieldBindingUtils.Bind(imageAsCursorToggle,
            () => settings.UseImageAsCursor,
            newValue => settings.UseImageAsCursor = newValue);

        new EnumChooserControl<ESceneChangeAnimation>(sceneChangeAnimationChooser)
            .Bind(() => settings.SceneChangeAnimation,
                newValue => settings.SceneChangeAnimation = newValue);

        LabeledChooserControl<float> audioPreviewFadeInDurationChooserControl = new(previewFadeInDurationChooser,
            NumberUtils.CreateFloatList(0.5f, 5f, 0.5f),
            newValue => Translation.Of($"{newValue.ToStringInvariantCulture("0.00")} s"));
        audioPreviewFadeInDurationChooserControl.Bind(() => settings.PreviewFadeInDurationInSeconds,
            newValue => settings.PreviewFadeInDurationInSeconds = newValue);

        EnumChooserControl<ESongBackgroundScaleMode> songBackgroundScaleModeChooserControl = new(songBackgroundScaleModeChooser);
        songBackgroundScaleModeChooserControl.Bind(() => settings.SongBackgroundScaleMode,
            newValue => settings.SongBackgroundScaleMode = newValue);

        LabeledChooserControl<float> sceneChangeDurationChooserControl = new(sceneChangeDurationChooser,
            NumberUtils.CreateFloatList(0, 0.9f, 0.05f),
            newValue => Translation.Of($"{newValue.ToStringInvariantCulture("0.00")} s"));
        sceneChangeDurationChooserControl.Bind(() => settings.SceneChangeDurationInSeconds,
                newValue => settings.SceneChangeDurationInSeconds = newValue);

        // Load available themes:
        List<ThemeMeta> themeMetas = themeManager.GetThemeMetas();
        LabeledChooserControl<ThemeMeta> themeChooserControl = new(themeChooser, themeMetas,
            themeMeta => Translation.Of(ThemeMetaUtils.GetDisplayName(themeMeta)));
        themeChooserControl.Bind(
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
