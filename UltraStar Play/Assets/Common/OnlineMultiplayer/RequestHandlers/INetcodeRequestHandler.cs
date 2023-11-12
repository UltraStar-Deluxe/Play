using System.Collections.Generic;

namespace CommonOnlineMultiplayer
{
    public interface INetcodeRequestHandler
    {
        public IReadOnlyList<ENetcodeMessageType> HandledMessageTypes { get; }
        public string GetResponse(NetcodeRequestDto requestDto);
        public int Priority { get; }
    }
}
