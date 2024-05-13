using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class OnlineMultiplayerConnectionDialogControl : AbstractModalDialogControl, INeedInjection, IInjectionFinishedListener
    {
        [Inject(UxmlName = R.UxmlNames.hostOnlineGameTabButton)]
        private ToggleButton hostOnlineGameTabButton;

        [Inject(UxmlName = R.UxmlNames.joinOnlineGameTabButton)]
        private ToggleButton joinOnlineGameTabButton;

        [Inject(UxmlName = R.UxmlNames.currentOnlineGameTabButton)]
        private ToggleButton currentOnlineGameTabButton;

        [Inject(UxmlName = R.UxmlNames.hostOnlineGameControlsContainer)]
        private VisualElement hostOnlineGameControlsContainer;

        [Inject(UxmlName = R.UxmlNames.joinOnlineGameControlsContainer)]
        private VisualElement joinOnlineGameControlsContainer;

        [Inject(UxmlName = R.UxmlNames.currentOnlineGameControlsContainer)]
        private VisualElement currentOnlineGameControlsContainer;

        [Inject(UxmlName = R.UxmlNames.toggleOnlineMultiplayerBackendButton)]
        private Button toggleOnlineMultiplayerBackendButton;

        [Inject]
        private Injector injector;

        [Inject]
        private OnlineMultiplayerManager onlineMultiplayerManager;

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private Settings settings;

        private readonly TabGroupControl tabGroupControl = new();

        private IHostLobbyUiControl hostLobbyUiControl;
        private IJoinLobbyUiControl joinLobbyUiControl;
        private ICurrentLobbyUiControl currentLobbyUiControl;

        public override void OnInjectionFinished()
        {
            base.OnInjectionFinished();

            toggleOnlineMultiplayerBackendButton.SetTranslatedText(Translation.Get(R.Messages.onlineGame_backendWithName,
                "name", Translation.Get(settings.EOnlineMultiplayerBackend)));
            toggleOnlineMultiplayerBackendButton.RegisterCallbackButtonTriggered(_ => ToggleOnlineMultiplayerBackend());

            hostLobbyUiControl = onlineMultiplayerManager.BackendManager.CurrentBackend.HostLobbyUiControlFactory();
            joinLobbyUiControl = onlineMultiplayerManager.BackendManager.CurrentBackend.JoinLobbyUiControlFactory();
            currentLobbyUiControl = onlineMultiplayerManager.BackendManager.CurrentBackend.CurrentLobbyUiControlFactory();

            VisualElement hostLobbyControls = hostLobbyUiControl.CreateVisualElement();
            hostOnlineGameControlsContainer.Clear();
            hostOnlineGameControlsContainer.Add(hostLobbyControls);

            VisualElement joinLobbyControls = joinLobbyUiControl.CreateVisualElement();
            joinOnlineGameControlsContainer.Clear();
            joinOnlineGameControlsContainer.Add(joinLobbyControls);

            VisualElement currentLobbyControls = currentLobbyUiControl.CreateVisualElement();
            currentOnlineGameControlsContainer.Clear();
            currentOnlineGameControlsContainer.Add(currentLobbyControls);

            injector
                .WithRootVisualElement(hostLobbyControls)
                .Inject(hostLobbyUiControl);
            injector
                .WithRootVisualElement(joinLobbyControls)
                .Inject(joinLobbyUiControl);
            injector
                .WithRootVisualElement(currentLobbyControls)
                .Inject(currentLobbyUiControl);

            tabGroupControl.AddTabGroupButton(hostOnlineGameTabButton, hostOnlineGameControlsContainer);
            tabGroupControl.AddTabGroupButton(joinOnlineGameTabButton, joinOnlineGameControlsContainer);
            tabGroupControl.AddTabGroupButton(currentOnlineGameTabButton, currentOnlineGameControlsContainer);
            UpdateVisibleContainer();

            disposables.Add(networkManager
                .ObserveEveryValueChanged(it => it.IsServer || it.IsClient)
                .Subscribe(_ => UpdateVisibleContainer()));
            disposables.Add(hostLobbyUiControl);
            disposables.Add(joinLobbyUiControl);
            disposables.Add(currentLobbyUiControl);
        }

        private void ToggleOnlineMultiplayerBackend()
        {
            switch (settings.EOnlineMultiplayerBackend)
            {
                case EOnlineMultiplayerBackend.Steam:
                    settings.EOnlineMultiplayerBackend = EOnlineMultiplayerBackend.Netcode;
                    break;
                case EOnlineMultiplayerBackend.Netcode:
                    settings.EOnlineMultiplayerBackend = EOnlineMultiplayerBackend.Steam;
                    break;
            }

            // Close dialog to load different UI for different backend.
            CloseDialog();
        }

        private void UpdateVisibleContainer()
        {
            if (networkManager.IsServer || networkManager.IsClient)
            {
                tabGroupControl.ShowContainer(currentOnlineGameControlsContainer);
                hostOnlineGameTabButton.HideByDisplay();
                joinOnlineGameTabButton.HideByDisplay();
                currentOnlineGameTabButton.ShowByDisplay();
            }
            else
            {
                tabGroupControl.ShowContainer(joinOnlineGameControlsContainer);
                hostOnlineGameTabButton.ShowByDisplay();
                joinOnlineGameTabButton.ShowByDisplay();
                currentOnlineGameTabButton.HideByDisplay();
            }
        }
    }
}
