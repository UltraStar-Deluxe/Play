using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CompanionAppOptionsControl : MonoBehaviour, INeedInjection, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset connectedClientListEntryAsset;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

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

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_companionApp_title);
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
