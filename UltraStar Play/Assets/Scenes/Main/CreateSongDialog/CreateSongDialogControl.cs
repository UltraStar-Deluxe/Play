using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SFB;
using UniInject;
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

    [Inject(UxmlName = R.UxmlNames.createCoverToggle)]
    private Toggle createCoverToggle;

    [Inject(UxmlName = R.UxmlNames.createBackgroundToggle)]
    private Toggle createBackgroundToggle;

    [Inject(UxmlName = R.UxmlNames.createVideoToggle)]
    private Toggle createVideoToggle;

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
        createCoverToggle.value = true;
        createBackgroundToggle.value = true;
        createVideoToggle.value = false;

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

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(newValue);
        // Expected file name: "trackNumber - artist - title", where trackNumber, artist, and separators are optional.
        Match artistDashTitleMatch = Regex.Match(fileNameWithoutExtension, @"^((?<trackNumber>\d+)(\s?[-|,~]\s?))?((?<artist>[^\-]+)(\s?[-|,~]\s?))?(?<title>[^\-]+)$");
        if (artistDashTitleMatch.Success)
        {
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
        }

        UpdateOkButtonEnabled();
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
            createCoverToggle.value,
            createBackgroundToggle.value,
            createVideoToggle.value);
        CloseDialog();
    }

    private bool IsInputValid()
    {
        return !artistTextField.value.IsNullOrEmpty()
               && !titleTextField.value.IsNullOrEmpty()
               && (audioFileTextField.value.IsNullOrEmpty()
                   || FileUtils.Exists(audioFileTextField.value));
    }
}
