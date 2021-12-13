using System;
using Stopwatch = System.Diagnostics.Stopwatch;

using Unity.Collections;
using Unity.Networking.Transport;

namespace UtpTransport
{
	/// <summary>
	/// Wrapper for a UTP connection. Holds state for the connection.
	/// </summary>
	public abstract class UtpConnection
	{
		/// <summary>
		/// The wrapped UTP connection.
		/// </summary>
		public NetworkConnection networkConnection = default(NetworkConnection);

		// If we don't receive anything these many milliseconds
		// then consider us disconnected
		protected const int DEFAULT_TIMEOUT = 10000;
		protected int m_Timeout = DEFAULT_TIMEOUT; // TODO: allow for this to be configurable
		protected uint m_LastReceivedTime;
		protected readonly Stopwatch m_RefTime = new Stopwatch();

		public UtpConnection()
		{
			m_RefTime.Start();
		}

		/// <summary>
		/// Whether or not this connection has timed out.
		/// </summary>
		/// <returns></returns>
		public bool IsTimedOut()
		{
			return (uint)m_RefTime.ElapsedMilliseconds >= m_LastReceivedTime + m_Timeout;
		}

		/// <summary>
		/// Send data over the connection.
		/// </summary>
		/// <param name="driver">The network driver the connection is associated with.</param>
		/// <param name="pipeline">The pipeline data should be sent through.</param>
		/// <param name="stageType">The pipeline stage type to send data through.</param>
		/// <param name="segment">The data to send.</param>
		public void Send(NetworkDriver driver, NetworkPipeline pipeline, System.Type stageType, ArraySegment<byte> segment)
		{
			NetworkPipelineStageId stageId = NetworkPipelineStageCollection.GetStageId(stageType);
			driver.GetPipelineBuffers(pipeline, stageId, networkConnection, out var tmpReceiveBuffer, out var tmpSendBuffer, out var reliableBuffer);

			DataStreamWriter writer;
			int writeStatus = driver.BeginSend(pipeline, networkConnection, out writer);
			if (writeStatus == 0)
			{
				// segment.Array is longer than the number of bytes it holds, grab just what we need
				byte[] segmentArray = new byte[segment.Count];
				Array.Copy(segment.Array, 0, segmentArray, 0, segment.Count);

				NativeArray<byte> nativeMessage = new NativeArray<byte>(segmentArray, Allocator.Temp);
				writer.WriteBytes(nativeMessage);
				driver.EndSend(writer);
			}
			else
			{
				UtpLog.Warning("Write not successful: " + writeStatus);
			}
		}

		/// <summary>
		/// Process all incoming events/messages on this connection.
		/// </summary>
		/// <param name="driver">The network driver associated with the connection.</param>
		public abstract void ProcessIncomingEvents(NetworkDriver driver);
	}
}