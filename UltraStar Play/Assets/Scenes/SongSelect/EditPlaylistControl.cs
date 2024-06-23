using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditPlaylistControl : MonoBehaviour, INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.editPlaylistButton)]
    private Button editPlaylistButton;

    [Inject(UxmlName = R.UxmlNames.createPlaylistButton)]
    private Button createPlaylistButton;

    [Inject(UxmlName = R.UxmlNames.editPlaylistOverlay)]
    private VisualElement editPlaylistOverlay;

    [Inject(UxmlName = R.UxmlNames.searchPropertyDropdownOverlay)]
    private VisualElement searchPropertyDropdownOverlay;

    [Inject(UxmlName = R.UxmlNames.submitEditPlaylistButton)]
    private Button submitEditPlaylistButton;

    [Inject(UxmlName = R.UxmlNames.deletePlaylistButton)]
    private Button deletePlaylistButton;

    [Inject(UxmlName = R.UxmlNames.confirmDeletePlaylistButton)]
    private Button confirmDeletePlaylistButton;

    [Inject(UxmlName = R.UxmlNames.cancelDeletePlaylistButton)]
    private Button cancelDeletePlaylistButton;

    [Inject(UxmlName = R.UxmlNames.playlistNameTextField)]
    private TextField playlistNameTextField;

    [Inject(UxmlName = R.UxmlNames.validationWarningContainer)]
    private VisualElement validationWarningContainer;

    [Inject(UxmlName = R.UxmlNames.invalidValueLabel)]
    private Label invalidValueLabel;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Injector injector;

    private IPlaylist currentPlaylist;

    private MessageDialogControl createPlaylistDialogControl;

    private void Start()
    {
        songSelectSceneControl.SongSelectionPlaylistChooserControl.Selection
            .Subscribe(newValue => currentPlaylist = newValue);

        editPlaylistButton.RegisterCallbackButtonTriggered(_ => ShowEditCurrentPlaylistDialog());
        createPlaylistButton.RegisterCallbackButtonTriggered(_ => OpenCreatePlaylistDialog());
        submitEditPlaylistButton.RegisterCallbackButtonTriggered(_ => SubmitEditPlaylistDialog());
        playlistNameTextField.RegisterValueChangedCallback(evt => OnPlaylistNameTextFieldChanged(evt.newValue));
        playlistNameTextField.DisableParseEscapeSequences();

        deletePlaylistButton.RegisterCallbackButtonTriggered(_ =>
        {
            ShowConfirmAndCancelDeleteButtons();
            cancelDeletePlaylistButton.Focus();
        });
        cancelDeletePlaylistButton.RegisterCallbackButtonTriggered(_ =>
        {
            ShowDeleteAndSubmitButtons();
            submitEditPlaylistButton.Focus();
        });
        confirmDeletePlaylistButton.RegisterCallbackButtonTriggered(_ =>
        {
            Translation errorMessage = playlistManager.TryRemovePlaylist(currentPlaylist);
            if (!errorMessage.Value.IsNullOrEmpty())
            {
                Debug.LogError(errorMessage);
                NotificationManager.CreateNotification(errorMessage);
            }
            HideEditPlaylistDialog();
        });
    }

    private void OpenCreatePlaylistDialog()
    {
        if (createPlaylistDialogControl != null)
        {
            return;
        }

        createPlaylistDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_editPlaylistDialog_title));
        createPlaylistDialogControl.DialogClosedEventStream.Subscribe(_ => createPlaylistDialogControl = null);

        TextField newPlaylistNameTextField = new();
        newPlaylistNameTextField.name = "newPlaylistNameTextField";
        newPlaylistNameTextField.value = "New Playlist";
        createPlaylistDialogControl.AddVisualElement(newPlaylistNameTextField);

        createPlaylistDialogControl.AddButton(Translation.Get(R.Messages.action_cancel), R.Messages.action_cancel, _ => createPlaylistDialogControl.CloseDialog());
        createPlaylistDialogControl.AddButton(Translation.Get(R.Messages.common_ok), R.Messages.common_ok, _ =>
        {
            if (newPlaylistNameTextField.value.IsNullOrEmpty())
            {
                createPlaylistDialogControl.CloseDialog();
                return;
            }

            string newPlaylistName = newPlaylistNameTextField.value;
            if (playlistManager.HasPlaylist(newPlaylistName))
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_editPlaylistDialog_error_duplicateName));
                return;
            }

            createPlaylistDialogControl.CloseDialog();
            playlistManager.CreateNewPlaylist(newPlaylistName);
        });
    }

    private void OnPlaylistNameTextFieldChanged(string newPlaylistName)
    {
        EPlaylistNameIssue playlistNameIssue = playlistManager.GetPlaylistNameIssue(currentPlaylist, newPlaylistName);
        switch (playlistNameIssue)
        {
            case EPlaylistNameIssue.Invalid:
                validationWarningContainer.ShowByDisplay();
                invalidValueLabel.SetTranslatedText(Translation.Get(R.Messages.songSelectScene_editPlaylistDialog_error_invalidName));
                submitEditPlaylistButton.SetTranslatedText(Translation.Get(R.Messages.action_cancel));
                break;
            case EPlaylistNameIssue.Duplicate:
                validationWarningContainer.ShowByDisplay();
                invalidValueLabel.SetTranslatedText(Translation.Get(R.Messages.songSelectScene_editPlaylistDialog_error_duplicateName));
                submitEditPlaylistButton.SetTranslatedText(Translation.Get(R.Messages.action_cancel));
                break;
            default:
                validationWarningContainer.HideByDisplay();
                invalidValueLabel.SetTranslatedText(Translation.Empty);
                submitEditPlaylistButton.SetTranslatedText(Translation.Get(R.Messages.action_continue));
                break;
        }
    }

    public void HideEditPlaylistDialog()
    {
        editPlaylistOverlay.HideByDisplay();
    }

    public void ShowEditCurrentPlaylistDialog()
    {
        if (currentPlaylist == null
            || currentPlaylist is UltraStarAllSongsPlaylist
            || playlistManager.IsFavoritesPlaylist(currentPlaylist))
        {
            return;
        }

        validationWarningContainer.HideByDisplay();
        invalidValueLabel.SetTranslatedText(Translation.Empty);
        playlistNameTextField.value = currentPlaylist.Name;
        editPlaylistOverlay.ShowByDisplay();
        searchPropertyDropdownOverlay.HideByDisplay();

        ShowDeleteAndSubmitButtons();
    }

    private void ShowDeleteAndSubmitButtons()
    {
        deletePlaylistButton.ShowByDisplay();
        submitEditPlaylistButton.ShowByDisplay();
        confirmDeletePlaylistButton.HideByDisplay();
        cancelDeletePlaylistButton.HideByDisplay();
        submitEditPlaylistButton.Focus();
    }

    private void ShowConfirmAndCancelDeleteButtons()
    {
        deletePlaylistButton.HideByDisplay();
        submitEditPlaylistButton.HideByDisplay();
        confirmDeletePlaylistButton.ShowByDisplay();
        cancelDeletePlaylistButton.ShowByDisplay();
        cancelDeletePlaylistButton.Focus();
    }

    private void SubmitEditPlaylistDialog()
    {
        string newPlaylistName = playlistNameTextField.value;
        if (playlistManager.GetPlaylistNameIssue(currentPlaylist, newPlaylistName) != EPlaylistNameIssue.None)
        {
            // Submit works as cancel button
            HideEditPlaylistDialog();
            return;
        }

        // Try to rename playlist
        if (!playlistManager.TrySetPlaylistName(currentPlaylist, newPlaylistName, out Translation errorMessage))
        {
            // Show error in UI
            Debug.LogError(errorMessage);
            NotificationManager.CreateNotification(errorMessage);
        }
        HideEditPlaylistDialog();
    }
}
