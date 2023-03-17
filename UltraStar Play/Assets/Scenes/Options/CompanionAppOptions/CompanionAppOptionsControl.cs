using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
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

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private Injector injector;

    private readonly List<ConnectedClientListEntryControl> connectedClientListEntryControls = new();
    
    protected override void Start()
    {
        base.Start();
        
        UpdateConnectedClients();
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(_ => UpdateConnectedClients())
            .AddTo(gameObject);
    }

    private void UpdateConnectedClients()
    {
        connectedClientList.Clear();
        serverSideConnectRequestManager.GetAllConnectedClientHandlers()
            .ForEach(clientHandler =>
            {
                connectedClientList.Add(CreateClientEntry(clientHandler));
            });

        connectedClientCountLabel.text = TranslationManager.GetTranslation(R.Messages.options_connectedClientCount,
            "count", ServerSideConnectRequestManager.ConnectedClientCount);
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
}
