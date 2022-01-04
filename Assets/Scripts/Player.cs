using Mirror;
using UnityEngine;
using Vivox;

public class Player : NetworkBehaviour
{
    /// <summary>
    /// The Sessions ID for the current server.
    /// </summary>
    [SyncVar]
    public string sessionId = "";

    /// <summary>
    /// Player name.
    /// </summary>
    public string username;

    /// <summary>
    /// Platform the user is on.
    /// </summary>
    public string platform;

    private VivoxManager m_VivoxManager;

    /// <summary>
    /// Shifts the players position in space based on the inputs received.
    /// </summary>
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
        username = SystemInfo.deviceName;
        platform = Application.platform.ToString();
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

    /// <summary>
    /// Called after player has spawned in the scene.
    /// </summary>
    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
    }
}
