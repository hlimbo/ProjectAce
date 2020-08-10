using UnityEngine;
using UnityEngine.UI;
using Mirror;


// Will not spawn on all game clients
// because it is not setup to spawn in NetworkManager script
public class ReadyScript2 : NetworkBehaviour
{
    private Text readyText;

    [SyncVar(hook = nameof(OnBoolChanged))]
    public bool playerData;

    private void OnBoolChanged(bool oldData, bool newData)
    {
        Debug.Log("OldData: " + oldData);
        Debug.Log("newData: " + newData);
        readyText.text = playerData.ToString();
    }

    // Start is called before the first frame update
    void Start()
    {
        readyText = GetComponent<Text>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InvokeRepeating(nameof(ToggleReady), 1f, 2f);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("OnStartClient Invoked");
        if (GameObject.Find("TestText") == null)
        {
            readyText.color = Color.red;
        }
    }

    [ServerCallback]
    private void ToggleReady()
    {
        playerData = !playerData;
    }
}
