using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SFB;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreateSongDialogControl : AbstractModalDialogControl, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(UxmlName = R.UxmlNames.audioFileTextField)]
    private TextField audioFileTextField;

    [Inject(UxmlName = R.UxmlNames.selectAudioFileButton)]
    private Button selectAudioFileButton;

    [Inject(UxmlName = R.UxmlNames.artistTextField)]
    private TextField artistTextField;

    [Inject(UxmlName = R.UxmlNames.titleTextField)]
    private TextField titleTextField;

    [Inject(UxmlName = R.UxmlNames.lyricsTextField)]
    private TextField lyricsTextField;

    [Inject(UxmlName = R.UxmlNames.lyricsLanguageChooser)]
    private EnumField lyricsLanguageChooser;

    [Inject(UxmlName = R.UxmlNames.okButton)]
    private Button okButton;

    [Inject(UxmlName = R.UxmlNames.cancelButton)]
    private Button cancelButton;

    [Inject]
    private CreateSongFromTemplateControl createSongFromTemplateControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        selectAudioFileButton.RegisterCallbackButtonTriggered(_ => OpenSelectAudioFileDialog());
        cancelButton.RegisterCallbackButtonTriggered(_ => CloseDialog());
        okButton.RegisterCallbackButtonTriggered(_ => TryCreateNewSong());

        audioFileTextField.RegisterValueChangedCallback(evt => OnAudioFileTextFieldChanged(evt.newValue));
        artistTextField.value = "";
        artistTextField.DisableParseEscapeSequences();
        titleTextField.value = "";
        titleTextField.DisableParseEscapeSequences();
        lyricsTextField.value = "";
        lyricsTextField.DisableParseEscapeSequences();

        FieldBindingUtils.Bind(lyricsLanguageChooser,
            () => EnumUtils.Parse(settings.SongEditorSettings.LyricsLanguage, ELyricsLanguage.English),
            newValue => settings.SongEditorSettings.LyricsLanguage = newValue.ToString());

        UpdateOkButtonEnabled();
        artistTextField.RegisterValueChangedCallback(evt => UpdateOkButtonEnabled());
        titleTextField.RegisterValueChangedCallback(evt => UpdateOkButtonEnabled());

        cancelButton.Focus();
    }

    private void OnAudioFileTextFieldChanged(string newValue)
    {
        if (!FileUtils.Exists(newValue))
        {
            UpdateOkButtonEnabled();
            return;
        }

        if (!TrySetArtistAndTitleFromFileMetadata(newValue))
        {
            if (!TrySetArtistAndTitleFromFileName(newValue))
            {
                artistTextField.value = "";
                titleTextField.value = "";
            }
        }

        UpdateOkButtonEnabled();
    }

    private bool TrySetArtistAndTitleFromFileMetadata(string filePath)
    {
        try
        {
            TagLib.File file = TagLib.File.Create(filePath);
            Debug.Log($"File.Tag: {JsonConverter.ToJson(file.Tag)}");
            string artist = file.Tag.AlbumArtists.JoinWith(", ");
            string title = file.Tag.Title;
            artistTextField.value = artist;
            titleTextField.value = title;
            return !artist.IsNullOrEmpty() && !title.IsNullOrEmpty();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to read file metadata. File '{filePath}', Error message: {e.Message}");
            return false;
        }
    }

    private bool TrySetArtistAndTitleFromFileName(string filePath)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        // Expected file name: "trackNumber - artist - title", where trackNumber, artist, and separators are optional.
        Match artistDashTitleMatch = Regex.Match(fileNameWithoutExtension, @"^((?<trackNumber>\d+)(\s?[-|,~]\s?))?((?<artist>[^\-]+)(\s?[-|,~]\s?))?(?<title>[^\-]+)$");
        if (!artistDashTitleMatch.Success)
        {
            return false;
        }

        string title = artistDashTitleMatch.Groups["title"].Value.Trim();
        string artist = artistDashTitleMatch.Groups["artist"].Value.Trim();
        if (int.TryParse(artist, out int artistAsInt))
        {
            // Ignore artist part when it can be parsed to an int because in this case, it is probably a track number.
            artist = "";
            title = fileNameWithoutExtension;
        }
        artistTextField.value = artist;
        titleTextField.value = title;
        return true;
    }

    private void OpenSelectAudioFileDialog()
    {
        string folder = SettingsUtils.GetEnabledSongFolders(settings)
            .FirstOrDefault()
            .OrIfNull("./");
        ExtensionFilter[] audioFileExtensionFilters = new[] { new ExtensionFilter("Files", "*.*") };
        string path = FileSystemDialogUtils.OpenFileDialog("Select audio file", folder, audioFileExtensionFilters);
        if (FileUtils.Exists(path))
        {
            audioFileTextField.value = path;
        }
    }

    private void UpdateOkButtonEnabled()
    {
        okButton.SetEnabled(IsInputValid());
    }

    private void TryCreateNewSong()
    {
        if (!IsInputValid())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_missingArtistOrTitle));
            return;
        }

        createSongFromTemplateControl.CreateNewSongFromTemplateAndContinueToSongEditor(
            audioFileTextField.value,
            artistTextField.value,
            titleTextField.value,
            true,
            true,
            true,
            lyricsTextField.value);
        CloseDialog();
    }

    private bool IsInputValid()
    {
        return !artistTextField.value.IsNullOrEmpty()
               && !titleTextField.value.IsNullOrEmpty()
               && !audioFileTextField.value.IsNullOrEmpty()
               && FileUtils.Exists(audioFileTextField.value);
    }
}
