using System;

using Unity.Collections;
using Unity.Networking.Transport;

namespace Utp
{
    /// <summary>
    /// A UTP connection that holds server-specific implementations. Used to manage client connections on a server.
    /// </summary>
    public struct UtpServerConnection
    {
        /// <summary>
        /// The wrapped UTP connection.
        /// </summary>
        public NetworkConnection networkConnection;

        public UtpServerConnection(NetworkConnection networkConnection)
        {
            this.networkConnection = networkConnection;
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
    }
}
