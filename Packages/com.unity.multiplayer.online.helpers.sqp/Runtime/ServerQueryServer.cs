using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Helpers.ServerQuery.Data;
using Unity.Helpers.ServerQuery.Protocols;
using Unity.Helpers.ServerQuery.Protocols.SQP;
using Unity.Helpers.ServerQuery.Protocols.A2S;
using Unity.Helpers.ServerQuery.Protocols.TF2E;

namespace Unity.Helpers.ServerQuery
{
    public class ServerQueryServer : IDisposable
    {
        private bool m_disposed = false;
        private UdpClient m_socket;
        private IProtocol m_backendProtocol;
        private uint m_serverID;
        public enum Protocol
        {
            A2S,
            SQP,
            TF2E
        }
        
        public ServerQueryServer(Protocol protocol, QueryData data, string iface = "0.0.0.0", int port = 0)
        {
            m_serverID = QueryDataProvider.RegisterServer(data);
            switch (protocol)
            {
                case Protocol.A2S:
                    m_backendProtocol = new A2SProtocol();
                    Debug.Log("A2S protocol found");
                    break;
                case Protocol.SQP:
                    m_backendProtocol = new SQPProtocol();
                    Debug.Log("SQP protocol found");
                    break;
                case Protocol.TF2E:
                    m_backendProtocol = new TF2EProtocol();
                    Debug.Log("TF2E protocol found");
                    break;
                default:
                    throw new ArgumentException("Invalid protocol", nameof(protocol));
            }

            if (port == 0) port = m_backendProtocol.GetDefaultPort();

            m_socket = new UdpClient(new IPEndPoint(IPAddress.Parse(iface), port));
            StartReceiving();
        }

        private void StartReceiving()
        {
            m_socket.BeginReceive(ReceiveCallback, this);
        }
        
        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            if (m_disposed) return;
            if (!asyncResult.IsCompleted) return;
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            var data = m_socket.EndReceive(asyncResult, ref sender);
            var response = m_backendProtocol.ReceiveData(data, sender, m_serverID);
            if (response == null)
            {
                StartReceiving();
                return;
            }
            
            m_socket.BeginSend(response, response.Length, sender, SendCallback, response);
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            if (m_disposed) return;
            if (!asyncResult.IsCompleted) return;
            var sentLength = m_socket.EndSend(asyncResult);
            var shouldSent = ((byte[])asyncResult.AsyncState).Length;
            if (sentLength != shouldSent)
            {
                throw new Exception($"Unable to send data : sent {sentLength}, should have sent : {shouldSent}");
            }
            StartReceiving();
        }
        
        public void Dispose()
        {
            m_disposed = true;
            m_socket?.Dispose();
        }

        public void UpdateData(QueryData data)
        {
            QueryDataProvider.UpdateData(m_serverID, data);
        }

        public QueryData GetQueryData()
        {
            return QueryDataProvider.GetServerData(m_serverID);
        }
    }
}