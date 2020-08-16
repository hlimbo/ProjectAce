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

    // the purpose of this flag is to reset the player's name if they attempt to duplicate any other player's name
    [SyncVar(hook = nameof(OnNameChangeFlagTriggered))]
    private bool nameChangeFlag;
    [SyncVar(hook = nameof(OnClientNameChanged))]
    private NameTag playerName;

    private void OnNameChangeFlagTriggered(bool _, bool newFlag)
    {
        if (playerText.gameObject.activeInHierarchy)
        {
            playerText.text = playerName.name;
        }

        if (playerNameInput.gameObject.activeInHierarchy)
        {
            if (playerName.isUserDefined)
            {
                playerNameInput.text = playerName.name;
            }
            else
            {
                playerNameInput.text = "";
                playerNameInput.transform.Find("Placeholder").GetComponent<Text>().text = playerName.name;
            }
        }
    }

    private void OnClientNameChanged(NameTag _, NameTag newTag)
    {
        if (playerText.gameObject.activeInHierarchy)
        {
            playerText.text = newTag.name;
        }

        if (playerNameInput.gameObject.activeInHierarchy)
        {
            if (playerName.isUserDefined)
            {
                playerNameInput.text = newTag.name;
            }
            else
            {
                playerNameInput.text = "";
                playerNameInput.transform.Find("Placeholder").GetComponent<Text>().text = newTag.name;
            }
        }
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
        if(Manager.playerNames.ContainsKey(connectionId))
        {
            Debug.Log("REadyPanel on start server with name! " + Manager.playerNames[connectionId].name);
            playerName = Manager.playerNames[connectionId];
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        playerText.gameObject.SetActive(false);

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
        transform.localScale = new Vector3(0.8f, 0.8f, 1f);

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
        CmdChangeName(playerNameInput.text);
    }

    private void SetPlayerName(string name)
    {
        CmdChangeName(name);
    }

    [Command]
    private void CmdChangeName(string name)
    {
        // If person attempts to assign themselves a name that is already set to another player, prevent it by setting their name to a default name
        bool isNameTaken = false;
        foreach(var kvp in Manager.playerNames)
        {
            int connId = kvp.Key;
            if (connId == connectionId)
            {
                // check only every player name excluding the current name the requesting player connectionId has
                continue;
            }

            string usedName = kvp.Value.name;
            if(name.Equals(usedName))
            {
                Debug.Log("name already taken: " + name);
                isNameTaken = true;
                break;
            }
        }

        if (isNameTaken || name.Length == 0)
        {
            // Set default name
            Manager.playerNames[connectionId] = new NameTag
            {
                name = connectionId == 0 ? "Leader" : string.Format("Player{0}", connectionId),
                isUserDefined = false
            };

            playerName = Manager.playerNames[connectionId];
        }
        else if(name.Length > 0)
        {
            Manager.playerNames[connectionId] = new NameTag
            {
                name = name,
                isUserDefined = true
            };

            playerName = Manager.playerNames[connectionId];
        }

        // used to trigger on the client side when a name gets updated
        nameChangeFlag = !nameChangeFlag;
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
