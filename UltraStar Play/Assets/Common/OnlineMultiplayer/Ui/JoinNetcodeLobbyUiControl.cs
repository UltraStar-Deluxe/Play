using System.Text;
using UniInject;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace CommonOnlineMultiplayer
{
    public class JoinNetcodeLobbyUiControl : INeedInjection, IInjectionFinishedListener, IJoinLobbyUiControl
    {
        [Inject(UxmlName = R.UxmlNames.unityTransportIpAddressField)]
        private TextField unityTransportIpAddressField;

        [Inject(UxmlName = R.UxmlNames.unityTransportPortField)]
        private IntegerField unityTransportPortField;

        [Inject(UxmlName = R.UxmlNames.joinOnlineGameDirectlyButton)]
        private Button joinOnlineGameDirectlyButton;

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private Settings settings;

        public void OnInjectionFinished()
        {
            FieldBindingUtils.Bind(unityTransportIpAddressField,
                () => settings.UnityTransportIpAddress,
                newValue => settings.UnityTransportIpAddress = newValue);

            FieldBindingUtils.Bind(unityTransportPortField,
                () => settings.UnityTransportPort,
                newValue => settings.UnityTransportPort = (ushort)newValue);

            joinOnlineGameDirectlyButton.RegisterCallbackButtonTriggered(_ => JoinGameDirectly());
        }

        private void JoinGameDirectly()
        {
            CommonOnlineMultiplayerUtils.ConfigureUnityTransport(networkManager, settings);

            LobbyConnectionRequestDto requestDto = new("ClientPlayer");
            string payload = requestDto.ToJson();
            networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
            networkManager.StartClient();
        }

        public void Dispose()
        {
        }

        public VisualElement CreateVisualElement()
        {
            return Resources.Load<VisualTreeAsset>("JoinNetcodeLobbyUi").CloneTreeAndGetFirstChild();
        }
    }
}
