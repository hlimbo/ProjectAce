using UnityEngine;
using Mirror;

public class PlayerCount : NetworkBehaviour
{
    [SyncVar]
    public int count = 0;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("PlayerCount Server Started");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("OnStartClient player count: " + count);
    }
}
