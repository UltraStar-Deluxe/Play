using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Inject(UxmlName = R.UxmlNames.animateSceneChangePicker)]
    private ItemPicker animateSceneChangePicker;

    [Inject(UxmlName = R.UxmlNames.showPlayerNamePicker)]
    private ItemPicker showPlayerNamePicker;
    
    [Inject(UxmlName = R.UxmlNames.showScoreNumberPicker)]
    private ItemPicker showScoreNumberPicker;
    
    [Inject]
    private UiManager uiManager;

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

        new BoolPickerControl(animateSceneChangePicker)
            .Bind(() => settings.GraphicSettings.AnimateSceneChange,
                newValue => settings.GraphicSettings.AnimateSceneChange = newValue);

        new BoolPickerControl(showPlayerNamePicker)
            .Bind(() => settings.GraphicSettings.showPlayerNames,
                newValue => settings.GraphicSettings.showPlayerNames = newValue);
        
        new BoolPickerControl(showScoreNumberPicker)
            .Bind(() => settings.GraphicSettings.showScoreNumbers,
                newValue => settings.GraphicSettings.showScoreNumbers = newValue);
        
        // Load available themes:
        List<ThemeMeta> themeMetas = themeManager.GetThemeMetas();
        LabeledItemPickerControl<ThemeMeta> themePickerControl = new(themePicker, themeMetas);
        themePickerControl.GetLabelTextFunction = themeMeta => ThemeMetaUtils.GetDisplayName(themeMeta);
        themePickerControl.Bind(
            () => themeManager.GetCurrentTheme(),
            newValue => themeManager.SetCurrentTheme(newValue));
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
            () => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddCustomThemes)));
        helpDialogControl.AddButton("Themes Folder",
            () => ApplicationUtils.OpenDirectory(ThemeManager.GetAbsoluteUserDefinedThemesFolder()));
        return helpDialogControl;
    }
}
