using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;

public class TimePanel : NetworkBehaviour
{
    private Text playerLabel;
    private Text timeLeftText;

    private Transform parent;

    private TurnSystemNetworkManager manager;
    public TurnSystemNetworkManager Manager
    {
        get
        {
            if(manager == null)
            {
                manager = NetworkManager.singleton as TurnSystemNetworkManager;
            }

            return manager;
        }
    }


    [SyncVar]
    private int connectionId;
    [SyncVar(hook = nameof(OnClientTimeLeftChanged))]
    public int timeLeft;

    private void OnClientTimeLeftChanged(int oldTimeLeft, int newTimeLeft)
    {
        timeLeftText.text = string.Format("{0} seconds", newTimeLeft);
    }

    private void Awake()
    {
        parent = GameObject.Find("TimerPanel")?.transform;
        playerLabel = transform.Find("Label")?.GetComponent<Text>();
        timeLeftText = transform.Find("TimeLeft")?.GetComponent<Text>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        connectionId = connectionToClient.connectionId;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(isClientOnly)
        {
            Manager.timePanels[connectionId] = this;
        }

        transform.SetParent(parent);
        playerLabel.text = string.Format("Player {0}", connectionId);
    }

    [Server]
    public void StartCountdown(int clientConnectionId)
    {
        timeLeft = 10;
        StartCoroutine(Countdown(clientConnectionId));
    }

    [Server]
    public void StopCountdown(int clientConnectionId)
    {
        StopAllCoroutines();
    }

    private IEnumerator Countdown(int clientConnectionId)
    {
        while(timeLeft > 0)
        {
            yield return new WaitForSeconds(1f);
            timeLeft -= 1;
        }

        // Could stop prematurely...

        Debug.Log("Countdown finished");

        // gross that TimePanel depends on TurnPlayerController and TurnPlayerController depends on TimePanel
        Manager.playerControllers[clientConnectionId].TargetDisableControls(NetworkServer.connections[clientConnectionId]);
        Manager.GoToNextTurn();
        int currentPlayerConnectionId = Manager.turnOrder[Manager.CurrentTurn];
        Manager.playerControllers[currentPlayerConnectionId].TargetEnableControls(NetworkServer.connections[currentPlayerConnectionId]);
        Manager.timePanels[currentPlayerConnectionId].StartCountdown(currentPlayerConnectionId);

        yield return null;
    }
}
