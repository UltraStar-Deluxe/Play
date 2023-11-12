using System;

namespace CommonOnlineMultiplayer
{
    public class NetcodeRequestHandler<REQUESTDTO> : INetcodeRequestHandler
        where REQUESTDTO : NetcodeRequestDto, new()
    {
        private readonly ENetcodeMessageType handledMessageType;
        public ENetcodeMessageType HandledMessageType => handledMessageType;

        private readonly int priority;
        public int Priority => priority;

        private readonly Func<REQUESTDTO, UnityNetcodeClientId, JsonSerializable> getResponse;

        public string GetResponse(NetcodeRequest request)
        {
            REQUESTDTO requestDto = JsonConverter.FromJson<REQUESTDTO>(request.RequestMessage);
            JsonSerializable responseDto = getResponse(requestDto, request.SenderNetcodeClientId);
            if (responseDto == null)
            {
                return "{}";
            }

            return responseDto.ToJson();
        }

        public NetcodeRequestHandler(
            ENetcodeMessageType handledMessageType,
            int priority,
            Func<REQUESTDTO, UnityNetcodeClientId, JsonSerializable> getResponse)
        {
            this.handledMessageType = handledMessageType;
            this.priority = priority;
            this.getResponse = getResponse;
        }
    }
}
