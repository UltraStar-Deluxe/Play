using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CompanionAppOptionsControl : AbstractOptionsSceneControl, INeedInjection
{
    [FormerlySerializedAs("connectedClientListEntryAsset")] [InjectedInInspector]
    public VisualTreeAsset connectedClientListEntryUi;

    [Inject(UxmlName = R.UxmlNames.connectedClientCountLabel)]
    private Label connectedClientCountLabel;

    [Inject(UxmlName = R.UxmlNames.connectedClientList)]
    private ScrollView connectedClientList;

    [Inject(UxmlName = R.UxmlNames.noConnectedClientsContainer)]
    private VisualElement noConnectedClientsContainer;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UiManager uiManager;

    private readonly List<ConnectedClientListEntryControl> connectedClientListEntryControls = new();

    protected override void Start()
    {
        base.Start();

        UpdateConnectedClients();
        serverSideConnectRequestManager.ClientConnectionChangedEventStream
            .Subscribe(_ => UpdateConnectedClients())
            .AddTo(gameObject);
    }

    private void UpdateConnectedClients()
    {
        connectedClientList.Clear();
        List<IConnectedClientHandler> allConnectedClientHandlers = serverSideConnectRequestManager.GetAllConnectedClientHandlers();
        allConnectedClientHandlers.Sort((a, b) => string.Compare(a.ClientName, b.ClientName, StringComparison.InvariantCultureIgnoreCase));
        allConnectedClientHandlers.ForEach(clientHandler =>
            {
                connectedClientList.Add(CreateClientEntry(clientHandler));
            });

        connectedClientCountLabel.SetTranslatedText(Translation.Get(R.Messages.options_connectedClientCount,
            "count", serverSideConnectRequestManager.ConnectedClientCount));

        bool noConnectedClients = serverSideConnectRequestManager.ConnectedClientCount <= 0;
        noConnectedClientsContainer.SetVisibleByDisplay(noConnectedClients);
        if (noConnectedClients)
        {
            HighlightHelpIcon();
        }

        ThemeManager.ApplyThemeSpecificStylesToVisualElements(connectedClientList);
    }

    private VisualElement CreateClientEntry(IConnectedClientHandler clientHandler)
    {
        VisualElement visualElement = connectedClientListEntryUi.CloneTreeAndGetFirstChild();
        ConnectedClientListEntryControl control = injector
            .WithRootVisualElement(visualElement)
            .WithBindingForInstance(clientHandler)
            .CreateAndInject<ConnectedClientListEntryControl>();
        connectedClientListEntryControls.Add(control);
        return visualElement;
    }

    public override string HelpUri => Translation.Get(R.Messages.uri_howToCompanionApp);
}
