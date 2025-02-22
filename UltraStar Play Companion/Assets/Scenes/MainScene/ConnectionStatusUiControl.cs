using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class ConnectionStatusUiControl : INeedInjection, IInjectionFinishedListener
{
    private const int ConnectRequestCountShowTroubleshootingHintThreshold = 3;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [Inject(UxmlName = R.UxmlNames.connectionStatusText)]
    private Label connectionStatusText;

    [Inject(UxmlName = R.UxmlNames.connectionThroubleshootingText)]
    private Label connectionThroubleshootingText;

    [Inject(UxmlName = R.UxmlNames.connectionTroubleshootingAlert)]
    private VisualElement connectionTroubleshootingAlert;

    [Inject(UxmlName = R.UxmlNames.serverErrorResponseText)]
    private Label serverErrorResponseText;

    [Inject(UxmlName = R.UxmlNames.serverErrorAlert)]
    private VisualElement serverErrorAlert;

    public void OnInjectionFinished()
    {
        // Update connection status
        connectionThroubleshootingText.HideByDisplay();
        connectionTroubleshootingAlert.HideByDisplay();

        serverErrorResponseText.HideByDisplay();
        serverErrorAlert.HideByDisplay();

        clientSideCompanionClientManager.ConnectEventStream.Subscribe(UpdateConnectionStatus);

        UpdateTranslation();
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess)
        {
            connectionStatusText.text = Translation.Get(R.Messages.companionApp_connectedTo, "remote" , connectEvent.ServerIpEndPoint.Address);

            SetErrorResponseTextAndVisibility("");
            SetThroubleshootingTextAndVisibility("");
        }
        else
        {
            connectionStatusText.text = connectEvent.ConnectRequestCount > 0
                ? Translation.Get(R.Messages.companionApp_connectingWithFailedAttempts, "count", connectEvent.ConnectRequestCount)
                : Translation.Get(R.Messages.companionApp_connecting);

            SetErrorResponseTextAndVisibility(connectEvent.ErrorMessage);
            SetThroubleshootingTextAndVisibility(
                connectEvent.ErrorMessage.IsNullOrEmpty()
                && connectEvent.ConnectRequestCount > ConnectRequestCountShowTroubleshootingHintThreshold
                    ? Translation.Get(R.Messages.companionApp_troubleShootingHints)
                    : "");
        }
    }

    private void SetThroubleshootingTextAndVisibility(string text)
    {
        connectionThroubleshootingText.text = text;
        connectionThroubleshootingText.SetVisibleByDisplay(!text.IsNullOrEmpty());
        connectionTroubleshootingAlert.SetVisibleByDisplay(!text.IsNullOrEmpty());
    }

    private void SetErrorResponseTextAndVisibility(string text)
    {
        serverErrorResponseText.text = text;
        serverErrorResponseText.SetVisibleByDisplay(!text.IsNullOrEmpty());
        serverErrorAlert.SetVisibleByDisplay(!text.IsNullOrEmpty());
    }

    private void UpdateTranslation()
    {
        connectionStatusText.text = Translation.Get(R.Messages.companionApp_connecting);
    }
}
