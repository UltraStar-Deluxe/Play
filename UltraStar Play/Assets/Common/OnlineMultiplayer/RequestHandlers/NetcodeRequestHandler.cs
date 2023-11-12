using System;
using System.Collections.Generic;

namespace CommonOnlineMultiplayer
{
    public class NetcodeRequestHandler : INetcodeRequestHandler
    {
        private readonly List<ENetcodeMessageType> handledMessageTypes;
        public IReadOnlyList<ENetcodeMessageType> HandledMessageTypes => handledMessageTypes;

        private readonly int priority;
        public int Priority => priority;

        private readonly Func<NetcodeRequestDto, string> getResponse;
        public string GetResponse(NetcodeRequestDto requestDto)
        {
            return getResponse(requestDto);
        }

        public NetcodeRequestHandler(
            List<ENetcodeMessageType> handledMessageTypes,
            int priority,
            Func<NetcodeRequestDto, string> getResponse)
        {
            this.handledMessageTypes = handledMessageTypes;
            this.priority = priority;
            this.getResponse = getResponse;
        }

        public NetcodeRequestHandler(
            ENetcodeMessageType handledMessageType,
            int priority,
            Func<NetcodeRequestDto, string> getResponse)
            : this(
                new List<ENetcodeMessageType>() { handledMessageType },
                priority,
                getResponse)
        {
        }

        public NetcodeRequestHandler(
            ENetcodeMessageType handledMessageType,
            int priority,
            Func<NetcodeRequestDto, JsonSerializable> getResponse)
            : this(
                handledMessageType,
                priority,
                requestDto => getResponse(requestDto).ToJson())
        {
        }
    }
}
