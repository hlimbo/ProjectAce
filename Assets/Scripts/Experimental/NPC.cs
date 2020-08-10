using UnityEngine;
using Mirror;

public class NPC : NetworkBehaviour
{
    [SyncVar]
    private int connectionId;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (connectionToClient == null) return;
        connectionId = connectionToClient.connectionId;
        Debug.LogFormat("Number of owned objects by the client {0} OnStartServer: {1}", connectionId, connectionToClient.clientOwnedObjects.Count);
        //foreach(var obj in connectionToClient.clientOwnedObjects)
        //{
        //    Debug.Log(obj.gameObject.name + " with net id: " + obj.netId);
        //}
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // connectionToClient is null when game client is connecting to host server
        // connectionToClient is not null when game client is the host
        //Debug.Log(connectionToClient);
        //Debug.Log(connectionToServer);
        // Must replicate color here not on the derived network manager script
        if(connectionId > 0)
        {
            GetComponent<SpriteRenderer>().color = Color.red;
        }

    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Debug.Log("Gaining client authority over game object spawned on server");
    }
}
