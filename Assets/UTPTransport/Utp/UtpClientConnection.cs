using System;

using Unity.Collections;
using Unity.Networking.Transport;

namespace Utp
{
	/// <summary>
	/// A UTP connection that holds client-specific implementations. Used to manage a client's connection to a server.
	/// </summary>
	public class UtpClientConnection : UtpConnection
	{
		// Events
		public Action OnConnected;
		public Action<ArraySegment<byte>> OnReceivedData;
		public Action OnDisconnected;

		public UtpClientConnection(Action OnConnected, Action<ArraySegment<byte>> OnReceivedData, Action OnDisconnected)
		{
			this.OnConnected = OnConnected;
			this.OnReceivedData = OnReceivedData;
			this.OnDisconnected = OnDisconnected;
		}

		/// <summary>
		/// Attempt to connect to a listen server at a given endpoint. 
		/// </summary>
		/// <param name="driver">The driver associated with the connection.</param>
		/// <param name="endpoint">The endpoint to connect to.</param>
		public void Connect(NetworkDriver driver, NetworkEndPoint endpoint)
		{
			networkConnection = driver.Connect(endpoint);
		}

		public override void ProcessIncomingEvents(NetworkDriver driver)
		{
			DataStreamReader stream;
			NetworkEvent.Type netEvent;
			while ((netEvent = networkConnection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
			{
				m_LastReceivedTime = (uint)m_RefTime.ElapsedMilliseconds;

				if (netEvent == NetworkEvent.Type.Connect)
				{
					UtpLog.Info("Client successfully connected to server");
					OnConnected.Invoke();
				}
				else if (netEvent == NetworkEvent.Type.Data)
				{
					NativeArray<byte> nativeMessage = new NativeArray<byte>(stream.Length, Allocator.Temp);
					stream.ReadBytes(nativeMessage);
					OnReceivedData.Invoke(new ArraySegment<byte>(nativeMessage.ToArray()));
				}
				else if (netEvent == NetworkEvent.Type.Disconnect)
				{
					UtpLog.Info("Client disconnected from server");

					networkConnection = default(NetworkConnection);
					OnDisconnected.Invoke();
				}
				else
				{
					UtpLog.Warning("Received unknown event: " + netEvent);
				}
			}
		}
	}
}

