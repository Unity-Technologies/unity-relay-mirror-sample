using Mirror;
using UnityEngine;

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

    public string ip;

    /// <summary>
    /// Platform the user is on.
    /// </summary>
    public string platform;

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
        ip = NetworkManager.singleton.networkAddress;
    }

    private void Start()
    {
    }

    void Update()
    {
        HandleMovement();
    }

    /// <summary>
    /// Called after player has spawned in the scene.
    /// </summary>
    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
    }

}
