using System;
using System.Net;

namespace Unity.Helpers.ServerQuery.Protocols
{
    public interface IProtocol
    {
        byte[] ReceiveData(byte[] data, IPEndPoint remoteClient, uint serverID);

        int GetDefaultPort();
    }
}