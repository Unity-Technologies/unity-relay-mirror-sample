using System;
using UnityEngine;
using Unity.Helpers.ServerQuery.Data;

namespace Unity.Helpers.ServerQuery.ServerQuery
{
    public class ServerQueryManager : MonoBehaviour
    {
        private SQPServer m_server;

        [SerializeField]
        private string m_interface = "0.0.0.0";
        
        private static ServerQueryManager s_instance;
        
        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(this);
                return;
            }
            
            if (s_instance == null)
            {
                s_instance = this;
            }

            //m_server = new SQPServer(m_protocol, m_interface, m_port);
        }

        public void ServerStart(QueryData data, SQPServer.Protocol protocol, ushort port)
        {
            m_server = new SQPServer(protocol, data, m_interface, port);
        }

        //./go-svrquery -addr localhost:12121 -proto sqp
        
        public void OnDestroy()
        {
            m_server?.Dispose();
        }

        public void UpdateQueryData(QueryData data)
        {
            m_server.UpdateData(data);
        }

        public QueryData GetQueryData()
        {
            return m_server.GetQueryData();
        }
    }
}