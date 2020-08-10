using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnPlayerController : NetworkBehaviour
{
    #region Client Side Controls
    [SerializeField]
    private Button randomNumberButton;

    [SerializeField]
    private Button endTurnButton;

    [SerializeField]
    private Text playerLabel;

    [SerializeField]
    private Text randomNumberLabel;

    #endregion

    private TurnSystemNetworkManager manager;
    public TurnSystemNetworkManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = NetworkManager.singleton as TurnSystemNetworkManager;
            }

            return manager;
        }
    }


    [SyncVar]
    private int connectionId;
    [SyncVar(hook = nameof(OnClientReceivedRandomNumber))]
    private int randomNumber;
    [SyncVar(hook = nameof(OnClientReceivedTurnIndex))]
    public int turnIndex;

    private void OnClientReceivedRandomNumber(int oldRandom, int newRandom)
    {
        randomNumberLabel.text = newRandom.ToString();
    }

    private void OnClientReceivedTurnIndex(int oldIndex, int newIndex)
    {
        Debug.LogFormat("Player {0} Turn Index is {1}", connectionId, newIndex);
    }

    private void Awake()
    {
        randomNumberButton = GameObject.Find("PickRandomNumberButton")?.GetComponent<Button>();
        endTurnButton = GameObject.Find("EndTurnButton")?.GetComponent<Button>();
        playerLabel = GameObject.Find("PlayerText")?.GetComponent<Text>();
        randomNumberLabel = GameObject.Find("RandomNumberText")?.GetComponent<Text>();
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
            Manager.playerControllers[connectionId] = this;
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        playerLabel.text = string.Format("Player {0} Controls", connectionId);

        randomNumberButton.onClick.AddListener(CmdOnRandomNumberGenerated);
        endTurnButton.onClick.AddListener(CmdOnEndTurnSelected);
    }

    [Command]
    private void CmdOnRandomNumberGenerated()
    {
        randomNumber = Manager.GenerateRandomNumber();
    }

    [Command]
    private void CmdOnEndTurnSelected()
    {
        // connection to player who just ended their turn
        int lastPlayerConnectionId = Manager.turnOrder[Manager.CurrentTurn];
        Manager.timePanels[lastPlayerConnectionId].StopCountdown(lastPlayerConnectionId);
        TargetDisableControls(NetworkServer.connections[lastPlayerConnectionId]);
        Manager.GoToNextTurn();
        int currentPlayerConnectionId = Manager.turnOrder[Manager.CurrentTurn];
        TargetEnableControls(NetworkServer.connections[currentPlayerConnectionId]);
        Manager.timePanels[currentPlayerConnectionId].StartCountdown(currentPlayerConnectionId);
    }

    [TargetRpc]
    public void TargetDisableControls(NetworkConnection clientConnection)
    {
        Debug.Log("Disabling controls for client connection: " + clientConnection.connectionId);
        randomNumberButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
    }

    [TargetRpc]
    public void TargetEnableControls(NetworkConnection clientConnection)
    {
        Debug.Log("Enabling controls for client connection: " + clientConnection.connectionId);
        randomNumberButton.gameObject.SetActive(true);
        endTurnButton.gameObject.SetActive(true);
    }
}
