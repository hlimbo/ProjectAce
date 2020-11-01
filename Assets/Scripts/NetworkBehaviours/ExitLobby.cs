using UnityEngine;
using UnityEngine.UI;

public class ExitLobby : MonoBehaviour
{
    [SerializeField]
    private Button exitButton;
    private ProjectAceNetworkManager manager;

    private void Awake()
    {
        manager = FindObjectOfType<ProjectAceNetworkManager>();
        exitButton?.onClick.AddListener(Disconnect);
    }

    private void Disconnect()
    {
        manager.Disconnect();
    }
}
