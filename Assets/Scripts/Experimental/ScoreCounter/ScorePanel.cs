using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class ScorePanel : NetworkBehaviour
{
    private Text scoreText;
    private Text playerText;
    private Transform playerScoresPanel;

    [SyncVar(hook = nameof(HandleScoreChanged))]
    private int score;
    public int Score => score;

    [SyncVar]
    private int connectionId;

    private ScoreCounterNetworkManager manager;
    public ScoreCounterNetworkManager Manager
    {
        get
        {
            if(manager == null)
            {
                manager = NetworkManager.singleton as ScoreCounterNetworkManager;
            }

            return manager;
        }
    }

    private void HandleScoreChanged(int oldScore, int newScore)
    {
        scoreText.text = newScore.ToString();
    }

    private void Awake()
    {
        playerScoresPanel = GameObject.Find("PlayerScoresPanel").transform;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        score = 0;
        connectionId = connectionToClient.connectionId;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    
        scoreText = transform.Find("ScoreText")?.GetComponent<Text>();
        playerText = transform.Find("PlayerText")?.GetComponent<Text>();
        scoreText.text = score.ToString();
        playerText.text = connectionId == 0 ? "Host" : string.Format("Player {0}", connectionId);
        transform.SetParent(playerScoresPanel);
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Manager.IncreaseScoreButton.onClick.AddListener(CmdIncreaseScore);
        Manager.DecreaseScoreButton.onClick.AddListener(CmdDecreaseScore);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (isClientOnly)
        {
            Manager.scorePanels.Remove(NetworkClient.connection.connectionId);
        }

        if (hasAuthority)
        {
            Debug.Log("Removing Listeners");
            Manager.IncreaseScoreButton.onClick.RemoveListener(CmdIncreaseScore);
            Manager.DecreaseScoreButton.onClick.RemoveListener(CmdDecreaseScore);
        }
    }

    [Command]
    private void CmdIncreaseScore()
    {
        if(Manager.scorePool > 0 && score < ScoreCounterNetworkManager.MAX_SCORE_POOL_SIZE)
        {
            score += 1;
            Manager.scorePool -= 1;

            // Trigger OnMessageBase event on all clients

            var message = new ScoreCounterNetworkManager.ScorePoolMessage()
            {
                scorePool = Manager.scorePool
            };

            NetworkServer.SendToAll(message);
        }
    }

    [Command]
    private void CmdDecreaseScore()
    {
        if (Manager.scorePool < ScoreCounterNetworkManager.MAX_SCORE_POOL_SIZE && score > 0)
        {
            score -= 1;
            Manager.scorePool += 1;
            // Trigger OnMessageBase event on all clients

            var message = new ScoreCounterNetworkManager.ScorePoolMessage()
            {
                scorePool = Manager.scorePool
            };

            NetworkServer.SendToAll(message);
        }
    }
}
