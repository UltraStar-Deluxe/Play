using UniInject;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace CommonOnlineMultiplayer
{
    public class HostNetcodeLobbyUiControl : INeedInjection, IInjectionFinishedListener, IHostLobbyUiControl
    {
        [Inject(UxmlName = R.UxmlNames.hostOnlineGameDirectlyButton)]
        private Button hostOnlineGameDirectlyButton;

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private Settings settings;

        public void OnInjectionFinished()
        {
            hostOnlineGameDirectlyButton.RegisterCallbackButtonTriggered(evt => HostGameDirectly());
        }

        private void HostGameDirectly()
        {
            CommonOnlineMultiplayerUtils.ConfigureUnityTransport(networkManager, settings);
            networkManager.StartHost();
        }

        public void Dispose()
        {
        }

        public VisualElement CreateVisualElement()
        {
            return Resources.Load<VisualTreeAsset>("HostNetcodeLobbyUi").CloneTreeAndGetFirstChild();
        }
    }
}
