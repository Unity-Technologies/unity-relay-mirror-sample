using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vivox;

public class Player : NetworkBehaviour
{

    [SyncVar]
    public string sessionId = "";

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

        if (m_VivoxManager != null)
        {
            if (m_VivoxManager.GetAudioState() == VivoxUnity.ConnectionState.Connected && transform.hasChanged)
            {
                m_VivoxManager.Update3DPosition(transform);
                transform.hasChanged = false;
            }
        }
    }

    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
    }
}
