using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class NetcodeRequestHandlerRegistry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInit()
        {
            Instance = new();
        }

        public static NetcodeRequestHandlerRegistry Instance { get; private set; } = new();

        private readonly Dictionary<ENetcodeMessageType, List<INetcodeRequestHandler>> messageTypeToRequestHandlers = new();

        public INetcodeRequestHandler GetRequestHandler(ENetcodeMessageType messageType)
        {
            if (messageTypeToRequestHandlers.TryGetValue(messageType, out List<INetcodeRequestHandler> requestHandlers)
                && !requestHandlers.IsNullOrEmpty())
            {
                return requestHandlers
                    .OrderBy(it => -it.Priority)
                    .FirstOrDefault();
            }

            throw new OnlineMultiplayerException($"No registered handler for message type {messageType}");
        }

        public void AddRequestHandler(INetcodeRequestHandler netcodeRequestHandler)
        {
            foreach (ENetcodeMessageType messageType in netcodeRequestHandler.HandledMessageTypes)
            {
                if (messageTypeToRequestHandlers.TryGetValue(messageType, out List<INetcodeRequestHandler> existingRequestHandlers))
                {
                    if (existingRequestHandlers.AnyMatch(existingRequestHandler => existingRequestHandler.Priority == netcodeRequestHandler.Priority))
                    {
                        throw new OnlineMultiplayerException($"Failed to add request handler of type {netcodeRequestHandler.GetType().Name}." +
                                                             $"Cannot register multiple Netcode request handlers with priority {netcodeRequestHandler.Priority} and message type {messageType}.");
                    }
                    existingRequestHandlers.Add(netcodeRequestHandler);
                }
                else
                {
                    messageTypeToRequestHandlers[messageType] = new List<INetcodeRequestHandler>()
                    {
                        netcodeRequestHandler
                    };
                }
            }
        }
    }
}
