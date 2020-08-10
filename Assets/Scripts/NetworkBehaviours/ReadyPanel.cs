using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System;
using ProjectAce;

public class ReadyPanel : NetworkBehaviour
{

    [SyncVar]
    public int connectionId;

    [SyncVar(hook = nameof(HandleReadyChanged))]
    private bool isReady;
    public bool IsReady => isReady;

    [SyncVar(hook = nameof(HandleGameStart))]
    private bool hasGameStarted;

    private Text readyText;
    private Text playerText;

    private InputField playerNameInput;
    private Button readyButton;
    // Only available on the host machine
    private Button startGameButton;
    private GameObject lobbyPanel;
    private Transform parentTransform;

    public bool IsHost => isClient && isServer;

    private ProjectAceNetworkManager manager;
    public ProjectAceNetworkManager Manager
    {
        get
        {
            if(manager == null)
            {
                manager = NetworkManager.singleton as ProjectAceNetworkManager;
            }
            return manager;
        }
    }

    private NetworkPlayerController networkPlayerController;

    [SyncVar(hook = nameof(OnClientNameChange))]
    public string playerName;

    private void OnClientNameChange(string oldName, string newName)
    {
        Debug.Log("NEW NAME:" + newName);
        playerText.text = newName;
    }

    private void Awake()
    {
        readyText = transform.Find("ReadyText")?.GetComponent<Text>();
        playerText = transform.Find("PlayerText")?.GetComponent<Text>();
        readyButton = GameObject.Find("ReadyButton")?.GetComponent<Button>();
        startGameButton = GameObject.Find("StartGameButton")?.GetComponent<Button>();
        lobbyPanel = GameObject.Find("LobbyPanel");
        parentTransform = GameObject.Find("PlayerPanels")?.transform;
        playerNameInput = transform.Find("NameInputField")?.GetComponent<InputField>();

        // Only enable start game button on game application where it is the host
        startGameButton?.gameObject.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        connectionId = connectionToClient.connectionId;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        playerText.fontStyle = FontStyle.Bold;

        readyButton.onClick.AddListener(CmdToggleReady);
        startGameButton.onClick.AddListener(CmdStartGame);

        // Name related listeners
        readyButton.onClick.AddListener(SetPlayerName);
        playerNameInput.onEndEdit.AddListener(SetPlayerName);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        transform.SetParent(parentTransform);
        var rectTransform = GetComponent<RectTransform>();
        AnchorPresetsUtils.AssignAnchor(AnchorPresets.TOP_LEFT, ref rectTransform);

        if(!hasAuthority)
        {
            playerNameInput.gameObject.SetActive(false);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if(hasAuthority)
        {
            readyButton.onClick.RemoveListener(CmdToggleReady);
            startGameButton.onClick.RemoveListener(CmdStartGame);
        }
    }

    private void HandleReadyChanged(bool isOldReady, bool isNewReady)
    {
        // Client Side code applied when isReady is modified server-side
        readyText.text = isNewReady ? "Ready" : "Not Ready";
        readyText.color = isNewReady ? Color.green : Color.red;
    }

    private void HandleGameStart(bool oldHasGameStart, bool newHasGameStart)
    {
        lobbyPanel?.SetActive(!newHasGameStart);
    }

    private void SetPlayerName()
    {
        // hack
        if(networkPlayerController == null)
        {
            var npcs = FindObjectsOfType<NetworkPlayerController>();
            foreach (var npc in npcs)
            {
                if (npc.hasAuthority)
                {
                    networkPlayerController = npc;
                    break;
                }
            }
        }

        Debug.Log("SetPlayerName");
        if(networkPlayerController != null)
        {
            Debug.Log("11Change name to " + playerNameInput.text);
            networkPlayerController.CmdChangePlayerName(connectionId, playerNameInput.text);
        }
    }

    private void SetPlayerName(string name)
    {
        // hack
        if (networkPlayerController == null)
        {
            var npcs = FindObjectsOfType<NetworkPlayerController>();
            foreach (var npc in npcs)
            {
                if (npc.hasAuthority)
                {
                    networkPlayerController = npc;
                    break;
                }
            }
        }

        Debug.Log("SetPlayerName2");
        if (networkPlayerController != null)
        {
            Debug.Log("22Change name to " + name);
            networkPlayerController.CmdChangePlayerName(connectionId, name);
        }
    }

    [Command]
    private void CmdToggleReady()
    {
        isReady = !isReady;
        Debug.Log("CmdToggleReady on client: " + connectionId);
        Manager.DetermineIfAllClientsAreReady();
    }

    [Command]
    private void CmdStartGame()
    {
        Debug.Log("Game has started");
        hasGameStarted = true;
        Manager.StartGame();
    }

    [TargetRpc]
    public void TargetToggleStartGameButton(NetworkConnection hostConnection, bool canStartButtonBeEnabled)
    {
        startGameButton.gameObject.SetActive(canStartButtonBeEnabled);
    }
}
