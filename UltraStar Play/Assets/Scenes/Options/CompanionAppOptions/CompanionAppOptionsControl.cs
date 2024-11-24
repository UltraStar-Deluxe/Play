using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CompanionAppOptionsControl : AbstractOptionsSceneControl, INeedInjection
{
    [InjectedInInspector]
    public VisualTreeAsset companionClientListEntryUi;

    [Inject(UxmlName = R.UxmlNames.requireCompanionClientPermissionsToggle)]
    private Toggle requireCompanionClientPermissionsToggle;

    [Inject(UxmlName = R.UxmlNames.companionClientCountLabel)]
    private Label companionClientCountLabel;

    [Inject(UxmlName = R.UxmlNames.companionClientList)]
    private ScrollView companionClientList;

    [Inject(UxmlName = R.UxmlNames.noCompanionClientsContainer)]
    private VisualElement noCompanionClientsContainer;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject]
    private Injector injector;

    [Inject]
    private UiManager uiManager;

    private readonly List<CompanionClientListEntryControl> companionClientListEntryControls = new();

    protected override void Start()
    {
        base.Start();

        FieldBindingUtils.Bind(requireCompanionClientPermissionsToggle,
            () => settings.RequireCompanionClientPermission,
            newValue =>
            {
                settings.RequireCompanionClientPermission = newValue;
                companionClientListEntryControls.ForEach(it => it.UpdatePermissions());
                // Disconnect all such that they reconnect with new permissions.
                serverSideCompanionClientManager.DisconnectAll();
            });

        UpdateCompanionClients();
        serverSideCompanionClientManager.ClientConnectionChangedEventStream
            .Subscribe(_ => UpdateCompanionClients())
            .AddTo(gameObject);
    }

    private void UpdateCompanionClients()
    {
        List<ICompanionClientHandler> allCompanionClientHandlers = serverSideCompanionClientManager.GetAllCompanionClientHandlers();
        allCompanionClientHandlers.Sort((a, b) => string.Compare(a.ClientName, b.ClientName, StringComparison.InvariantCultureIgnoreCase));

        requireCompanionClientPermissionsToggle.SetVisibleByDisplay(!allCompanionClientHandlers.IsNullOrEmpty());
        noCompanionClientsContainer.SetVisibleByDisplay(allCompanionClientHandlers.IsNullOrEmpty());
        if (allCompanionClientHandlers.IsNullOrEmpty())
        {
            HighlightHelpIcon();
        }

        companionClientCountLabel.SetTranslatedText(Translation.Get(R.Messages.options_companionClientCount,
            "count", serverSideCompanionClientManager.CompanionClientCount));

        companionClientList.Clear();
        allCompanionClientHandlers.ForEach(clientHandler => companionClientList.Add(CreateClientEntry(clientHandler)));
    }

    private VisualElement CreateClientEntry(ICompanionClientHandler clientHandler)
    {
        VisualElement visualElement = companionClientListEntryUi.CloneTreeAndGetFirstChild();
        CompanionClientListEntryControl control = injector
            .WithRootVisualElement(visualElement)
            .WithBindingForInstance(clientHandler)
            .CreateAndInject<CompanionClientListEntryControl>();
        companionClientListEntryControls.Add(control);
        return visualElement;
    }

    public override string HelpUri => Translation.Get(R.Messages.uri_howToCompanionApp);
}
