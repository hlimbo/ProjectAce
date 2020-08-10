using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

// When the game has capability to go back to a scene have this object be instantiated
// when the game application is first open so that when the user decides to go back
// to main menu screen... I don't get that warning stating that duplicate managers
// are on the same screen and one of them will get deleted
public class SceneTestNetworkManager : NetworkManager
{
    public override void OnServerConnect(NetworkConnection conn)
    {
        // Called ON Server
        base.OnServerConnect(conn);

        // I don't need to call the below script since the base network manager already calls the function in its base implementation
        //ServerChangeScene(onlineScene);

        // I shouldn't try to spawn objects after server change scene request as the scene can change and objects instantiated will be deleted on scene change unless marked by DontDestroyOnLoad

    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        // Called on the client
        base.OnClientSceneChanged(conn);
        Debug.Log("SceneTestNetworkManager OnClientSceneChanged!!!!!!!!!!!!!");
        Debug.LogFormat("Scene name on client {0}: {1}", conn.connectionId, SceneManager.GetActiveScene().name);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        // Called ON Server
        base.OnServerSceneChanged(sceneName);
        Debug.LogFormat("On Server scene changed to: {0}", sceneName);
        // Supposedly called automatically when offline and online scenes are set from NetworkManager
        // ServerChangeScene("myScene");
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
        Debug.Log("SceneTestNetworkManager OnServerReady");

        if(SceneManager.GetActiveScene().name.Equals("Scene_B"))
        {
            var onlineObject = Instantiate(spawnPrefabs[0]);
            onlineObject.GetComponent<NetworkTransform>().transform.position = new Vector3(conn.connectionId * 2f, 0f, 0f);
            NetworkServer.Spawn(onlineObject, conn);
        }
    }

    public override void OnClientNotReady(NetworkConnection conn)
    {
        base.OnClientNotReady(conn);
        Debug.Log("SceneTestNetworkManager OnClientNotReady with connectionId: " + conn.connectionId);
    }
}
