using UnityEngine;
using UnityEngine.Events;

namespace SteamOnlineMultiplayer
{
    public class DisconnectReason
    {
        public event UnityAction<ConnectStatus> OnReasonChanged;

        public ConnectStatus Reason { get; private set; } = ConnectStatus.Undefined;

        public void SetDisconnectReason(ConnectStatus reason)
        {
            Debug.Log($"New reason: {reason}");
            Reason = reason;
            OnReasonChanged?.Invoke(reason);
        }

        public void Clear() => Reason = ConnectStatus.Undefined;

        public bool HasTransitionReason => Reason != ConnectStatus.Undefined;
    }
}
