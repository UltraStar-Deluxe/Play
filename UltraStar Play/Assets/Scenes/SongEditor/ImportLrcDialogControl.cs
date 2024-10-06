using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class ImportLrcDialogControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SentenceFitToNoteAction sentenceFitToNoteAction;

    [Inject(UxmlName = R.UxmlNames.importLrcDialogOverlay)]
    private VisualElement importLrcDialogOverlay;

    [Inject(UxmlName = R.UxmlNames.importLrcTextField)]
    private TextField importLrcTextField;

    [Inject(UxmlName = R.UxmlNames.importLrcIssueContainer)]
    private VisualElement importLrcIssueContainer;

    [Inject(UxmlName = R.UxmlNames.importLrcIssueLabel)]
    private Label importLrcIssueLabel;

    [Inject(UxmlName = R.UxmlNames.openImportLrcDialogButton)]
    private Button openImportLrcDialogButton;

    [Inject(UxmlName = R.UxmlNames.closeImportLrcDialogButton)]
    private Button closeImportLrcDialogButton;

    [Inject(UxmlName = R.UxmlNames.importLrcFormatButton)]
    private Button importLrcFormatButton;

    [Inject(UxmlName = R.UxmlNames.importLrcFormatToP1Button)]
    private Button importLrcFormatToP1Button;

    [Inject(UxmlName = R.UxmlNames.importLrcFormatToP2Button)]
    private Button importLrcFormatToP2Button;

    [Inject(UxmlName = R.UxmlNames.lrcImportHelpButton)]
    private Button lrcImportHelpButton;

    private readonly LrcFormatImporter lrcFormatImporter = new();

    private readonly Subject<ChangeEvent<string>> lrcTextChangedEventStream = new();

    public void OnInjectionFinished()
    {
        injector.Inject(lrcFormatImporter);

        importLrcTextField.DisableParseEscapeSequences();
        importLrcTextField.value = "";
        importLrcTextField.RegisterValueChangedCallback(evt => lrcTextChangedEventStream.OnNext(evt));
        lrcTextChangedEventStream.Throttle(new TimeSpan(0, 0, 0, 0, 200))
            .Subscribe(_ => UpdateErrorMessage());

        importLrcFormatButton.RegisterCallbackButtonTriggered(_ =>
        {
            ImportLrcFormat();
            CloseDialog();
        });
        importLrcFormatToP1Button.RegisterCallbackButtonTriggered(_ =>
        {
            ImportLrcFormat(EVoiceId.P1);
            CloseDialog();
        });
        importLrcFormatToP2Button.RegisterCallbackButtonTriggered(_ =>
        {
            ImportLrcFormat(EVoiceId.P2);
            CloseDialog();
        });
        lrcImportHelpButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenUrl(Translation.Get(R.Messages.uri_howToSongEditor)));
        openImportLrcDialogButton.RegisterCallbackButtonTriggered(_ => OpenDialog());
        closeImportLrcDialogButton.RegisterCallbackButtonTriggered(_ => CloseDialog());
        VisualElementUtils.RegisterDirectClickCallback(importLrcDialogOverlay, CloseDialog);

        CloseDialog();
    }

    private void ImportLrcFormat()
    {
        if (importLrcTextField.value.IsNullOrEmpty())
        {
            return;
        }

        // Remove old notes
        editorNoteDisplayer.ClearNotesInLayer(ESongEditorLayer.Import);
        layerManager.ClearEnumLayer(ESongEditorLayer.Import);

        // Import new notes
        List<Sentence> importedSentences = lrcFormatImporter.ImportLrcFormat(importLrcTextField.value, songMeta, settings);
        List<Note> importedNotes = importedSentences.SelectMany(sentence => sentence.Notes).ToList();
        if (importedNotes.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
        }
        else
        {
            importedNotes.ForEach(note => note.SetSentence(null));
            importedNotes.ForEach(note => layerManager.AddNoteToEnumLayer(ESongEditorLayer.Import, note));
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_lrcImportDialog_success,
                "count", importedNotes.Count));
        }

        songMetaChangeEventStream.OnNext(new ImportedNotesEvent());
    }

    private void ImportLrcFormat(EVoiceId voiceId)
    {
        if (importLrcTextField.value.IsNullOrEmpty())
        {
            return;
        }

        // Remove old notes
        songMeta.RemoveVoice(voiceId);
        editorNoteDisplayer.ClearNoteControls();

        // Import new notes
        List<Sentence> importedSentences = lrcFormatImporter.ImportLrcFormat(importLrcTextField.value, songMeta, settings);
        List<Note> importedNotes = importedSentences.SelectMany(sentence => sentence.Notes).ToList();
        if (importedSentences.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
        }
        else
        {
            songMeta.AddVoice(new Voice(voiceId, importedSentences));
            sentenceFitToNoteAction.Execute(importedSentences);

            NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_lrcImportDialog_success,
                "count", importedNotes.Count));
        }

        songMetaChangeEventStream.OnNext(new ImportedNotesEvent());
    }

    public void OpenDialog()
    {
        importLrcDialogOverlay.ShowByDisplay();
        UpdateErrorMessage();
    }

    public void CloseDialog()
    {
        importLrcDialogOverlay.HideByDisplay();
    }

    private void UpdateErrorMessage()
    {
        Translation errorMessage = lrcFormatImporter.GetLrcFormatErrorMessage(importLrcTextField.text);
        SetErrorMessage(errorMessage);
    }

    private void SetErrorMessage(Translation errorMessage)
    {
        bool hasError = !errorMessage.Value.IsNullOrEmpty();
        importLrcIssueContainer.SetVisibleByDisplay(hasError);
        importLrcIssueLabel.SetTranslatedText(errorMessage);

        bool canImport = !hasError
                         && !importLrcTextField.value.IsNullOrEmpty();
        importLrcFormatButton.SetEnabled(canImport);
        importLrcFormatToP1Button.SetEnabled(canImport);
        importLrcFormatToP2Button.SetEnabled(canImport);
    }
}
