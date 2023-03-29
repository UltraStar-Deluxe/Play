using System.Collections.Generic;
using ProTrans;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DesignOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject]
    private ThemeManager themeManager;

    [Inject(UxmlName = R.UxmlNames.themePicker)]
    private ItemPicker themePicker;

    [Inject(UxmlName = R.UxmlNames.noteDisplayModePicker)]
    private ItemPicker noteDisplayModePicker;

    [Inject(UxmlName = R.UxmlNames.lyricsOnNotesPicker)]
    private ItemPicker lyricsOnNotesPicker;

    [Inject(UxmlName = R.UxmlNames.staticLyricsPicker)]
    private ItemPicker staticLyricsPicker;

    [Inject(UxmlName = R.UxmlNames.pitchIndicatorPicker)]
    private ItemPicker pitchIndicatorPicker;

    [Inject(UxmlName = R.UxmlNames.imageAsCursorPicker)]
    private ItemPicker imageAsCursorPicker;

    [Inject(UxmlName = R.UxmlNames.sceneChangeAnimationPicker)]
    private ItemPicker sceneChangeAnimationPicker;

    [Inject(UxmlName = R.UxmlNames.sceneChangeDurationPicker)]
    private ItemPicker sceneChangeDurationPicker;
    
    [Inject(UxmlName = R.UxmlNames.showPlayerNamePicker)]
    private ItemPicker showPlayerNamePicker;
    
    [Inject(UxmlName = R.UxmlNames.showScoreNumberPicker)]
    private ItemPicker showScoreNumberPicker;
    
    [Inject(UxmlName = R.UxmlNames.animatedBackgroundItemPicker)]
    private ItemPicker animatedBackgroundItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.backgroundLightItemPicker)]
    private ItemPicker backgroundLightItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.vfxEnabledPicker)]
    private ItemPicker vfxEnabledPicker;

    [Inject]
    private UiManager uiManager;
    
    [Inject]
    private BackgroundLightManager backgroundLightManager;

    protected override void Start()
    {
        base.Start();
        
        new NoteDisplayModeItemPickerControl(noteDisplayModePicker)
            .Bind(() => settings.GraphicSettings.noteDisplayMode,
                newValue => settings.GraphicSettings.noteDisplayMode = newValue);

        new BoolPickerControl(lyricsOnNotesPicker)
            .Bind(() => settings.GraphicSettings.showLyricsOnNotes,
                newValue => settings.GraphicSettings.showLyricsOnNotes = newValue);

        new BoolPickerControl(staticLyricsPicker)
            .Bind(() => settings.GraphicSettings.showStaticLyrics,
                newValue => settings.GraphicSettings.showStaticLyrics = newValue);

        new BoolPickerControl(pitchIndicatorPicker)
            .Bind(() => settings.GraphicSettings.showPitchIndicator,
                newValue => settings.GraphicSettings.showPitchIndicator = newValue);

        new BoolPickerControl(imageAsCursorPicker)
            .Bind(() => settings.GraphicSettings.useImageAsCursor,
                newValue => settings.GraphicSettings.useImageAsCursor = newValue);

        new LabeledItemPickerControl<ESceneChangeAnimation>(sceneChangeAnimationPicker, EnumUtils.GetValuesAsList<ESceneChangeAnimation>())
            .Bind(() => settings.GraphicSettings.sceneChangeAnimation,
                newValue => settings.GraphicSettings.sceneChangeAnimation = newValue);

        new BoolPickerControl(vfxEnabledPicker)
            .Bind(() => settings.GraphicSettings.enableVfx, 
                newValue => settings.GraphicSettings.enableVfx = newValue);

        LabeledItemPickerControl<float> sceneChangeDurationPickerControl = new(sceneChangeDurationPicker, NumberUtils.CreateFloatList(0, 0.55f, 0.05f));
        sceneChangeDurationPickerControl.Bind(() => settings.GraphicSettings.sceneChangeDurationInSeconds,
                newValue => settings.GraphicSettings.sceneChangeDurationInSeconds = newValue);
        sceneChangeDurationPickerControl.GetLabelTextFunction = newValue => $"{newValue.ToStringInvariantCulture("0.00")} s";
        
        new BoolPickerControl(showPlayerNamePicker)
            .Bind(() => settings.GraphicSettings.showPlayerNames,
                newValue => settings.GraphicSettings.showPlayerNames = newValue);
        
        new BoolPickerControl(showScoreNumberPicker)
            .Bind(() => settings.GraphicSettings.showScoreNumbers,
                newValue => settings.GraphicSettings.showScoreNumbers = newValue);
        
        new BoolPickerControl(animatedBackgroundItemPicker)
            .Bind(() => settings.GraphicSettings.animatedBackground,
                newValue => settings.GraphicSettings.animatedBackground = newValue);
        
        new LabeledItemPickerControl<int>(backgroundLightItemPicker, NumberUtils.CreateIntList(0, backgroundLightManager.BackgroundLightInstancesCount))
            .Bind(() => settings.GraphicSettings.backgroundLightIndex,
                newValue => settings.GraphicSettings.backgroundLightIndex = newValue);
        
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
        themeManager.SetCurrentTheme(themeMeta);
    }

    public void UpdateTranslation()
    {
        themePicker.Label = TranslationManager.GetTranslation(R.Messages.options_design_theme);
        noteDisplayModePicker.Label = TranslationManager.GetTranslation(R.Messages.options_noteDisplayMode);
        staticLyricsPicker.Label = TranslationManager.GetTranslation(R.Messages.options_showStaticLyrics);
        lyricsOnNotesPicker.Label = TranslationManager.GetTranslation(R.Messages.options_showLyricsOnNotes);
        pitchIndicatorPicker.Label = TranslationManager.GetTranslation(R.Messages.options_showPitchIndicator);
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
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddCustomThemes)));
        helpDialogControl.AddButton("Themes Folder",
            _ => ApplicationUtils.OpenDirectory(ThemeManager.GetAbsoluteUserDefinedThemesFolder()));
        return helpDialogControl;
    }
}
