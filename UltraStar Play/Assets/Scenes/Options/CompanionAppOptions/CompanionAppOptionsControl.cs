using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CompanionAppOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset connectedClientListEntryAsset;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.connectedClientCountLabel)]
    private Label connectedClientCountLabel;

    [Inject(UxmlName = R.UxmlNames.connectedClientList)]
    private ScrollView connectedClientList;

    [Inject]
    private Settings settings;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    private void Start()
    {
        UpdateConnectedClients();
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(_ => UpdateConnectedClients())
            .AddTo(gameObject);
    }

    public void UpdateTranslation()
    {
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
        VisualElement result = connectedClientListEntryAsset.CloneTree();
        result.Q<Label>(R.UxmlNames.nameLabel).text = clientHandler.ClientName;
        return result;
    }
}
