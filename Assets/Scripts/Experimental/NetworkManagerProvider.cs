using UnityEngine;
using Mirror;

public class NetworkManagerProvider : MonoBehaviour
{
    [SerializeField]
    private NetworkManager networkManagerPrefab;

    private void Awake()
    {
        var singleton = FindObjectOfType<NetworkManager>();
        if(singleton == null)
        {
            NetworkManager instance = Instantiate(networkManagerPrefab);
            if(instance == null)
            {
                Debug.LogWarning("NetworkManagerProvider could not spawn NetworkManager prefab. Ensure networkManagerPrefab reference is not null");
            }
        }
        else
        {
            Debug.Log("NetworkManagerProvider.... NetworkManagerProvider already exists in the scene. No need to create another instance");
        }
    }
}
