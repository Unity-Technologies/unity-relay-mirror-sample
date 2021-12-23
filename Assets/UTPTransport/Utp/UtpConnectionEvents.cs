using Unity.Collections;

namespace UtpTransport
{
    /// <summary>
    /// Connection events seen on a connection.
    /// </summary>
    public enum UtpConnectionEventType
    {
        OnConnected,
        OnReceivedData,
        OnDisconnected
    }

    /// <summary>
    /// Struct to store events and the related data for that event.
    /// </summary>
    public struct UtpConnectionEvent
    {
        public byte eventType;
        public FixedList4096Bytes<byte> eventData;
        public int connectionId;
        public float time;
    }
}
