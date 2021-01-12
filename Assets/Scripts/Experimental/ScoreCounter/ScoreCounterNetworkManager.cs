using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;

// check maxNumber of connections here
public class ScoreCounterNetworkManager : NetworkManager
{
    public struct ScorePoolMessage : NetworkMessage
    {
        public int scorePool;
    }

    public struct FullServerMessage : NetworkMessage
    {
        public int maxConnectionCount;
    }

    // ServerSide properties
    public const int MAX_SCORE_POOL_SIZE = 52;
    public int scorePool;

    // Local game objects that aren't instantiated over the network
    [SerializeField]
    private Button increaseScoreButton;
    [SerializeField]
    private Button decreaseScoreButton;
    public Button IncreaseScoreButton => increaseScoreButton;
    public Button DecreaseScoreButton => decreaseScoreButton;

    [SerializeField]
    private Text scorePoolText;
    public Text ScorePoolText => scorePoolText;

    [SerializeField]
    private ScorePanel scorePanelPrefab;

    public readonly Dictionary<int, ScorePanel> scorePanels = new Dictionary<int, ScorePanel>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        scorePool = MAX_SCORE_POOL_SIZE;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("OnStartClient!!");
        NetworkClient.RegisterHandler<ScorePoolMessage>(OnClientReceivedScorePoolMessage, false);
        NetworkClient.RegisterHandler<FullServerMessage>(OnClientReceivedFullServerMessage, false);
    }

    private void OnClientReceivedScorePoolMessage(NetworkConnection conn, ScorePoolMessage msg)
    {
        // client-side
        Debug.Log("OnClientReceivedScorePoolMessage connectionId: " + conn.connectionId);

        // sync score pool from server on the client
        scorePool = msg.scorePool;

        // update the score pool for all connected clients to display retrieved from the server
        scorePoolText.text = string.Format("Score Pool: {0}", msg.scorePool);
    }

    private void OnClientReceivedFullServerMessage(NetworkConnection conn, FullServerMessage msg)
    {
        Debug.Log("OnClientReceivedFullServerMessage connectionId: " + conn.connectionId);
        Debug.Log("This client can't connect because the server is full with maxPlayerCount = " + msg.maxConnectionCount);
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);

        var message = new ScoreCounterNetworkManager.ScorePoolMessage();
        message.scorePool = scorePool;
        NetworkServer.SendToAll(message);
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        var scorePanel = Instantiate(scorePanelPrefab);
        NetworkServer.Spawn(scorePanel.gameObject, conn);
        scorePanels[conn.connectionId] = scorePanel;
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);

        if(scorePanels.ContainsKey(conn.connectionId))
        {
            var scorePanel = scorePanels[conn.connectionId];

            Debug.Log("Placing points back into scorePool: " + scorePanel.Score);
            // Refund scores back to scorePool
            scorePool += scorePanel.Score;
            var message = new ScorePoolMessage();
            message.scorePool = scorePool;
            NetworkServer.SendToAll(message);
            
            scorePanels.Remove(conn.connectionId);
            NetworkServer.Destroy(scorePanel.gameObject);
            scorePanel = null;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("OnStopClient!!");

        // Need a way to tell the client why it can't connect....

        NetworkClient.UnregisterHandler<ScorePoolMessage>();
        //NetworkClient.UnregisterHandler<FullServerMessage>();
        scorePanels.Clear();
    }

    //public override void OnClientDisconnect(NetworkConnection conn)
    //{
    //    base.OnClientDisconnect(conn);
    //    Debug.Log("OnClientDisconnect!!: ");
    //    NetworkClient.UnregisterHandler<ScorePoolMessage>();
    //    scorePanels.Clear();
    //}
}
