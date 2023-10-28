using System;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class NetcodeMessagingManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static NetcodeMessagingManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<NetcodeMessagingManager>();

        private const string CustomJsonMessageName = "CustomJsonMessage";

        private readonly Subject<NetcodeCustomMessageDto> messageEventStream = new();
        public IObservable<NetcodeCustomMessageDto> MessageEventStream => messageEventStream
            .ObserveOnMainThread();

        [Inject]
        private NetworkManager networkManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            RegisterCustomMessageHandlers();
        }

        protected override void OnDestroySingleton()
        {
            UnregisterClientMessageHandlers();
        }

        private void RegisterCustomMessageHandlers()
        {
            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                CustomJsonMessageName,
                (senderClientId, messagePayload) =>
                {
                    messagePayload.ReadValueSafe(out string json);
                    Log.Debug(() => $"Received message from Unity remote client {senderClientId}: {json}");

                    try
                    {
                        NetcodeCustomMessageDto messageDto = JsonConverter.FromJson<NetcodeCustomMessageDto>(json);
                        messageEventStream.OnNext(messageDto);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"Failed to deserialize message from Unity remote client {senderClientId}: {ex.Message}");
                    }
                });
        }

        private void UnregisterClientMessageHandlers()
        {
            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(CustomJsonMessageName);
        }

        public void SendMessage(ulong netId, NetcodeCustomMessageDto messageDto)
        {
            string json = messageDto.ToJson();
            FastBufferWriter writer = new FastBufferWriter();
            writer.WriteValueSafe(json);

            networkManager.CustomMessagingManager.SendNamedMessage(CustomJsonMessageName, netId, writer);
        }
    }
}
