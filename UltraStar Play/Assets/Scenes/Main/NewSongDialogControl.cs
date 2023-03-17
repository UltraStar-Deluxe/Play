using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NewSongDialogControl : AbstractModalDialogControl, IInjectionFinishedListener
{
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

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        
        cancelButton.RegisterCallbackButtonTriggered(_ => CloseDialog());
        okButton.RegisterCallbackButtonTriggered(_ => TryCreateNewSong());

        artistTextField.value = "";
        titleTextField.value = "";
        createCoverToggle.value = true;
        createBackgroundToggle.value = true;
        createVideoToggle.value = false;

        UpdateOkButtonEnabled();
        artistTextField.RegisterValueChangedCallback(evt => UpdateOkButtonEnabled());
        titleTextField.RegisterValueChangedCallback(evt => UpdateOkButtonEnabled());

        cancelButton.Focus();
    }

    private void UpdateOkButtonEnabled()
    {
        okButton.SetEnabled(IsInputValid());
    }

    private void TryCreateNewSong()
    {
        if (!IsInputValid())
        {
            UiManager.CreateNotification("Please specify the artist and title");
            return;
        }

        createSongFromTemplateControl.CreateNewSongFromTemplateAndContinueToSongEditor(
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
               && !titleTextField.value.IsNullOrEmpty();
    }
}
