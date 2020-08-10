using UnityEngine;
using Mirror;

public class QuitGame : MonoBehaviour
{
    // Reference to a NetworkManager singleton that persists between scenes
    private NetworkManager manager;
    private void Start()
    {
        manager = FindObjectOfType<NetworkManager>();
        if(manager == null)
        {
            Debug.LogWarning("NetworkManager is null... ensure Main Menu Scene has NetworkManager component attached to a non-network gameobject");
        }
    }

    // Used on QuitGameButton
    public void Disconnect()
    {
        if(manager == null)
        {
            Debug.LogWarning("NetworkManager is null... ensure Main Menu Scene has NetworkManager component attached to a non-network gameobject");
            return;
        }

        // Assuming this is client only
        if (NetworkServer.active)
        {
            Debug.Log("Host is ending the server");
            manager.StopHost();
        }
        else
        {
            Debug.Log("Client is disconnecting from the server");
            manager.StopClient();
        }
    }


}
