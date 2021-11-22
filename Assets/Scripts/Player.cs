using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vivox;

public class Player : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHolaCountChanged))]
    int holaCount = 0;

    [SyncVar]
    public string m_SessionId = "";

    private string m_Username = System.Guid.NewGuid().ToString();
    private VivoxManager m_VivoxManager;

    void HandleMovement()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(moveHorizontal * 0.1f, moveVertical * 0.1f, 0);
            transform.position = transform.position + movement;
        }
    }

    private void Awake()
    {
        
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            m_VivoxManager = FindObjectOfType<VivoxManager>(); 
        }
    }

    void Update()
    {
        HandleMovement();

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.N))
        {
            m_VivoxManager.ChangeChannel(KeyCode.N);
        }

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.M))
        {
            m_VivoxManager.ChangeChannel(KeyCode.M);
        }

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Sending Hola to Server!");
            Hola();
        }

        if (m_VivoxManager != null)
        {
            if (m_VivoxManager.GetAudioState() == VivoxUnity.ConnectionState.Connected && transform.hasChanged)
            {
                m_VivoxManager.Update3DPosition(transform);
                transform.hasChanged = false;
            }
        }
        //n == area 
        //m == team 
        //f == area PTT
        //v == team PTT
    }

    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
    }

    [Command]
    void Hola()
    {
        Debug.Log("Received Hola from Client!");
        holaCount += 1;
        ReplyHola();
    }

    [Command]
    public void CreateSessionId()
    {
        Debug.Log("Creating sessionId");
        m_SessionId = m_SessionId = System.Guid.NewGuid().ToString();
    }

    [TargetRpc]
    void ReplyHola()
    {
        Debug.Log("Received Hola from Server!");
    }

    [ClientRpc]
    void TooHigh()
    {
        Debug.Log("Too high!");
    }

    void OnHolaCountChanged(int oldCount, int newCount)
    {
        Debug.Log($"We had {oldCount} holas, but now we have {newCount} holas!");
    }
}
