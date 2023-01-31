using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InputSimulationControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    [Inject(UxmlName = R.UxmlNames.simulateLeftButton)]
    private Button simulateLeftButton;

    [Inject(UxmlName = R.UxmlNames.simulateRightButton)]
    private Button simulateRightButton;

    [Inject(UxmlName = R.UxmlNames.simulateUpButton)]
    private Button simulateUpButton;

    [Inject(UxmlName = R.UxmlNames.simulateDownButton)]
    private Button simulateDownButton;

    [Inject(UxmlName = R.UxmlNames.simulateEnterButton)]
    private Button simulateEnterButton;

    [Inject(UxmlName = R.UxmlNames.simulateEscapeButton)]
    private Button simulateEscapeButton;

    [Inject(UxmlName = R.UxmlNames.simulateSpaceButton)]
    private Button simulateSpaceButton;

    public void OnInjectionFinished()
    {
        RegisterCallbackToSendSimulationInputRequest(simulateLeftButton, "left");
        RegisterCallbackToSendSimulationInputRequest(simulateRightButton, "right");
        RegisterCallbackToSendSimulationInputRequest(simulateUpButton, "up");
        RegisterCallbackToSendSimulationInputRequest(simulateDownButton, "down");
        RegisterCallbackToSendSimulationInputRequest(simulateEnterButton, "enter");
        RegisterCallbackToSendSimulationInputRequest(simulateEscapeButton, "escape");
        RegisterCallbackToSendSimulationInputRequest(simulateSpaceButton, "space");
    }

    private void RegisterCallbackToSendSimulationInputRequest(Button uiButton, string keyboardButton)
    {
        uiButton.RegisterCallbackButtonTriggered(() => SendSimulateInputRequest(keyboardButton));
    }

    public void SendSimulateInputRequest(string keyboardButton)
    {
        string path = $"api/rest/input/{keyboardButton}";
        mainGameHttpClient.PostRequest(path);
    }
}
