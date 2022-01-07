using System;

using Unity.Collections;
using Unity.Networking.Transport;

namespace Utp
{
    /// <summary>
    /// A UTP connection that holds client-specific implementations. Used to manage a client's connection to a server.
    /// </summary>
    public struct UtpClientConnection
    {
        /// <summary>
        /// The wrapped UTP connection.
        /// </summary>
        public NetworkConnection networkConnection;

        /// <summary>
        /// Attempt to connect to a listen server at a given endpoint. 
        /// </summary>
        /// <param name="driver">The driver associated with the connection.</param>
        /// <param name="endpoint">The endpoint to connect to.</param>
        public void Connect(NetworkDriver driver, NetworkEndPoint endpoint)
        {
            networkConnection = driver.Connect(endpoint);
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

