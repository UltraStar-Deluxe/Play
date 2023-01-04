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

public class DesignOptionsControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.themeContainer)]
    private VisualElement themeContainer;

    [Inject(UxmlName = R.UxmlNames.noteDisplayModeContainer)]
    private VisualElement noteDisplayModeContainer;

    [Inject(UxmlName = R.UxmlNames.lyricsOnNotesContainer)]
    private VisualElement lyricsOnNotesContainer;

    [Inject(UxmlName = R.UxmlNames.staticLyricsContainer)]
    private VisualElement staticLyricsContainer;

    [Inject(UxmlName = R.UxmlNames.pitchIndicatorContainer)]
    private VisualElement pitchIndicatorContainer;

    [Inject(UxmlName = R.UxmlNames.imageAsCursorContainer)]
    private VisualElement imageAsCursorContainer;

    [Inject(UxmlName = R.UxmlNames.animateSceneChangeContainer)]
    private VisualElement animateSceneChangeContainer;

    [Inject]
    private Settings settings;

    private void Start()
    {
        new NoteDisplayModeItemPickerControl(noteDisplayModeContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.noteDisplayMode,
                newValue => settings.GraphicSettings.noteDisplayMode = newValue);

        new BoolPickerControl(lyricsOnNotesContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.showLyricsOnNotes,
                newValue => settings.GraphicSettings.showLyricsOnNotes = newValue);

        new BoolPickerControl(staticLyricsContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.showStaticLyrics,
                newValue => settings.GraphicSettings.showStaticLyrics = newValue);

        new BoolPickerControl(pitchIndicatorContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.showPitchIndicator,
                newValue => settings.GraphicSettings.showPitchIndicator = newValue);

        new BoolPickerControl(imageAsCursorContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.useImageAsCursor,
                newValue => settings.GraphicSettings.useImageAsCursor = newValue);

        new BoolPickerControl(animateSceneChangeContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.AnimateSceneChange,
                newValue => settings.GraphicSettings.AnimateSceneChange = newValue);

        // Load available themes:
        string[] jsonThemes = System.Array.ConvertAll(Directory.GetFiles($"{Application.dataPath}/../themes/", "*.json", SearchOption.AllDirectories), Path.GetFileNameWithoutExtension);
        Dictionary<string, string> beautifiedToFilename = jsonThemes.ToDictionary(UIUtils.BeautifyString);

        LabeledItemPickerControl<string> themePickerControl = new(themeContainer.Q<ItemPicker>(), beautifiedToFilename.Keys.ToList());
        themePickerControl.Bind(() => UIUtils.BeautifyString(settings.GraphicSettings.CurrentThemeName),
            newValue => settings.GraphicSettings.CurrentThemeName = !beautifiedToFilename.ContainsKey(newValue) ? ThemeManager.DEFAULT_THEME : beautifiedToFilename[newValue]
        );

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        themeContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_theme);
        noteDisplayModeContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_noteDisplayMode);
        staticLyricsContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_showStaticLyrics);
        lyricsOnNotesContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_showLyricsOnNotes);
        pitchIndicatorContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_showPitchIndicator);
        imageAsCursorContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_useImageAsCursor);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_design_title);
    }
}
