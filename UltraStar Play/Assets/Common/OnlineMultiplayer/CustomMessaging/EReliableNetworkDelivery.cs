namespace CommonOnlineMultiplayer
{
    public enum EReliableNetworkDelivery
    {
        /// <summary>
        /// Reliable message
        /// </summary>
        Reliable,

        /// <summary>
        /// Reliable message where messages are guaranteed to be in the right order
        /// </summary>
        ReliableSequenced,

        /// <summary>
        /// A reliable message with guaranteed order with fragmentation support
        /// </summary>
        ReliableFragmentedSequenced
    }
}
