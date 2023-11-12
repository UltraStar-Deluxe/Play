using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class OnlineMultiplayerConnectionDialogControl : AbstractModalDialogControl, INeedInjection,
        IInjectionFinishedListener
    {
        [Inject(UxmlName = R.UxmlNames.hostOnlineGameTabButton)]
        private ToggleButton hostOnlineGameTabButton;

        [Inject(UxmlName = R.UxmlNames.joinOnlineGameTabButton)]
        private ToggleButton joinOnlineGameTabButton;

        [Inject(UxmlName = R.UxmlNames.hostOnlineGameControlsContainer)]
        private VisualElement hostOnlineGameControlsContainer;

        [Inject(UxmlName = R.UxmlNames.joinOnlineGameControlsContainer)]
        private VisualElement joinOnlineGameControlsContainer;

        [Inject(UxmlName = R.UxmlNames.currentConnectionControlsContainer)]
        private VisualElement currentConnectionControlsContainer;

        [Inject] private Injector injector;

        [Inject] private NetworkManager networkManager;

        private readonly TabGroupControl tabGroupControl = new();
        private readonly HostOnlineGameUiControl hostOnlineGameUiControl = new();
        private readonly JoinOnlineGameUiControl joinOnlineGameUiControl = new();
        private readonly CurrentLobbyUiControl currentLobbyUiControl = new();

        public override void OnInjectionFinished()
        {
            base.OnInjectionFinished();
            injector.Inject(hostOnlineGameUiControl);
            injector.Inject(joinOnlineGameUiControl);
            injector.Inject(currentLobbyUiControl);

            tabGroupControl.AddTabGroupButton(hostOnlineGameTabButton, hostOnlineGameControlsContainer);
            tabGroupControl.AddTabGroupButton(joinOnlineGameTabButton, joinOnlineGameControlsContainer);

            ToggleButton showCurrentConnectionsTabButton = new ToggleButton();
            showCurrentConnectionsTabButton.name = "showCurrentConnectionsTabButton";
            showCurrentConnectionsTabButton.text = "Current Connection";
            tabGroupControl.AddTabGroupButton(showCurrentConnectionsTabButton, currentConnectionControlsContainer);
            UpdateVisibleContainer();

            disposables.Add(networkManager
                .ObserveEveryValueChanged(it => it.IsServer || it.IsClient)
                .Subscribe(_ => UpdateVisibleContainer()));
            disposables.Add(hostOnlineGameUiControl);
            disposables.Add(joinOnlineGameUiControl);
            disposables.Add(currentLobbyUiControl);
        }

        private void UpdateVisibleContainer()
        {
            if (networkManager.IsServer || networkManager.IsClient)
            {
                tabGroupControl.ShowContainer(currentConnectionControlsContainer);
                hostOnlineGameTabButton.HideByDisplay();
                joinOnlineGameTabButton.HideByDisplay();
            }
            else
            {
                tabGroupControl.ShowContainer(joinOnlineGameControlsContainer);
                hostOnlineGameTabButton.ShowByDisplay();
                joinOnlineGameTabButton.ShowByDisplay();
            }
        }
    }
}
