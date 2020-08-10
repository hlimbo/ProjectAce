//using UnityEngine;
//using UnityEngine.UI;
//using Mirror;

//public class ReadyButton : NetworkBehaviour
//{
//    private Button btn;
//    private RectTransform rectTransform;
    
//    [SyncVar]
//    private uint playerPanelNetId;

//    [SyncVar]
//    private int connectionId;

//    private void Awake()
//    {
//        btn = GetComponent<Button>();
//        rectTransform = GetComponent<RectTransform>();
//    }

//    public override void OnStartServer()
//    {
//        base.OnStartServer();
//        connectionId = connectionToClient.connectionId;
//        foreach(var obj in connectionToClient.clientOwnedObjects)
//        {
//            Debug.Log(obj.gameObject.name + "   netId: " + obj.netId);
//            if(obj.GetComponent<PlayerPanel>() != null)
//            {
//                playerPanelNetId = obj.netId;
//            }
//        }
//    }

//    public override void OnStartClient()
//    {
//        base.OnStartClient();
//        Debug.Log("Network Id: " + GetComponent<NetworkIdentity>().netId);
//        Debug.Log("ConnectionId: " + connectionId);
//        // This button only needs to be instantiated once client side
//        transform.SetParent(GameObject.Find("Canvas").transform);
//        // Middle Right
//        rectTransform.anchorMin.Set(1f, 0.5f);
//        rectTransform.anchorMax.Set(1f, 0.5f);
//        rectTransform.pivot.Set(1f, 0.5f);
//        rectTransform.anchoredPosition = new Vector2(0f, 0f);

//        // Set the onclick listener on the playerpanel we have client authority over
//        // Does not work the way I expect it to as I don't know which game client this instance belongs to
//        Debug.Log("Looking for Player panel object");
//        foreach(var playerPanel in FindObjectsOfType<PlayerPanel>())
//        {
//            Debug.Log("playerPanel conn id: " + playerPanel.connectionId);
//            Debug.Log("my connection id: " + connectionId);
//            Debug.Log("My PLayer Panel Net Id: " + playerPanelNetId);
//            Debug.Log("A player panel net id: " + playerPanel.GetComponent<NetworkIdentity>().netId);
//            if(playerPanel.GetComponent<NetworkIdentity>().netId == playerPanelNetId)
//            {
//                Debug.Log("Registering button callback!");
//                btn.onClick.AddListener(playerPanel.ToggleReady);
//                break;
//            }
//        }
        
//    }
//}
