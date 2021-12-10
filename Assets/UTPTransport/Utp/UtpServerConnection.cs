using System;

using Unity.Collections;
using Unity.Networking.Transport;

namespace UtpTransport
{
	/// <summary>
	/// A UTP connection that holds server-specific implementations. Used to manage client connections on a server.
	/// </summary>
	public class UtpServerConnection : UtpConnection
	{
		// Events
		public Action<int, ArraySegment<byte>> OnReceivedData;
		public Action<int> OnDisconnected;

		public UtpServerConnection(NetworkConnection networkConnection, Action<int, ArraySegment<byte>> OnReceivedData,
			Action<int> OnDisconnected)
		{
			this.networkConnection = networkConnection;
			this.OnReceivedData = OnReceivedData;
			this.OnDisconnected = OnDisconnected;
		}

		public override void ProcessIncomingEvents(NetworkDriver driver)
		{
			DataStreamReader stream;
			NetworkEvent.Type netEvent;
			while ((netEvent = networkConnection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
			{
				m_LastReceivedTime = (uint)m_RefTime.ElapsedMilliseconds;

				if (netEvent == NetworkEvent.Type.Data)
				{
					NativeArray<byte> nativeMessage = new NativeArray<byte>(stream.Length, Allocator.Temp);
					stream.ReadBytes(nativeMessage);
					OnReceivedData.Invoke(networkConnection.GetHashCode(), new ArraySegment<byte>(nativeMessage.ToArray()));
				}
				else if (netEvent == NetworkEvent.Type.Disconnect)
				{
					UtpLog.Verbose("Client disconnected from server");
					OnDisconnected.Invoke(networkConnection.GetHashCode());
					networkConnection = default(NetworkConnection);
				}
				else
				{
					UtpLog.Warning("Received unknown event: " + netEvent);
				}
			}
		}
	}
}
