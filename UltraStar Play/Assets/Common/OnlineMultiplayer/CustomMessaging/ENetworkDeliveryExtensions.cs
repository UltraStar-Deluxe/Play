using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public static class ENetworkDeliveryExtensions
    {
        public static NetworkDelivery ToUnityNetworkDelivery(this ENetworkDelivery networkDelivery)
        {
            switch (networkDelivery)
            {
                case ENetworkDelivery.Unreliable: return NetworkDelivery.Unreliable;
                case ENetworkDelivery.UnreliableSequenced: return NetworkDelivery.UnreliableSequenced;
                case ENetworkDelivery.Reliable: return NetworkDelivery.Reliable;
                case ENetworkDelivery.ReliableSequenced: return NetworkDelivery.ReliableSequenced;
                case ENetworkDelivery.ReliableFragmentedSequenced: return NetworkDelivery.ReliableFragmentedSequenced;
                default:
                    throw new ArgumentOutOfRangeException(nameof(networkDelivery), networkDelivery, null);
            }
        }

        public static ENetworkDelivery ToCustomNetworkDelivery(this NetworkDelivery networkDelivery)
        {
            switch (networkDelivery)
            {
                case NetworkDelivery.Unreliable: return ENetworkDelivery.Unreliable;
                case NetworkDelivery.UnreliableSequenced: return ENetworkDelivery.UnreliableSequenced;
                case NetworkDelivery.Reliable: return ENetworkDelivery.Reliable;
                case NetworkDelivery.ReliableSequenced: return ENetworkDelivery.ReliableSequenced;
                case NetworkDelivery.ReliableFragmentedSequenced: return ENetworkDelivery.ReliableFragmentedSequenced;
                default:
                    throw new ArgumentOutOfRangeException(nameof(networkDelivery), networkDelivery, null);
            }
        }
    }
}
