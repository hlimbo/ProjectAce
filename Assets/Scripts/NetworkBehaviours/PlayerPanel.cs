using Mirror;
using UnityEngine;
using UnityEngine.UI;
using ProjectAce;
using System.Collections;
using System.Linq;
using DG.Tweening;

public class PlayerPanel : NetworkBehaviour
{
    public delegate void EndTurnDelegate();

    [SyncEvent]
    public event EndTurnDelegate EventEndTurn;

    [SyncVar]
    private int connectionId;
    public int ConnectionId => connectionId;

    private ProjectAceNetworkManager manager;
    public ProjectAceNetworkManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = NetworkManager.singleton as ProjectAceNetworkManager;
            }
            return manager;
        }
    }

    private Image timeLeftCircle;
    private Text playerLabel;
    private Text cardsLeftText;

    public AnchorPresets anchorPreset;

    [SyncVar]
    public int playerNumber;

    [SyncVar(hook = nameof(OnClientTimeLeftChanged))]
    private float timeLeft;
    [SyncVar]
    private float initialTimeLeft;

    private void OnClientTimeLeftChanged(float _, float newTimeLeft)
    {
        timeLeftCircle.DOFillAmount(newTimeLeft / initialTimeLeft, syncInterval);
    }

    [SyncVar(hook = nameof(OnClientHandleCardsLeftCountChanged))]
    private int cardsLeft = 0;

    [Server]
    public void SetCardsLeft(int cardsLeft)
    {
        this.cardsLeft = cardsLeft;
    }

    private void OnClientHandleCardsLeftCountChanged(int oldCardsLeftCount, int newCardsLeftCount)
    {
        cardsLeftText.text = newCardsLeftCount.ToString();
    }

    [SyncVar]
    private bool isMyTurn;
    public bool IsMyTurn => isMyTurn;

    private Transform uiCanvas;

    private RectTransform rectTransform;

    [SyncVar(hook = nameof(OnClientNameChanged))]
    public string playerName;

    private void OnClientNameChanged(string _, string newName)
    {
        playerLabel.text = newName;
    }

    [SyncVar(hook = nameof(OnClientReceiveNetworkPlayerControllerNetId))]
    public uint networkPlayerControllerNetId;
    private NetworkPlayerController networkPlayerController;

    private void OnClientReceiveNetworkPlayerControllerNetId(uint _, uint newId)
    {
        networkPlayerController = FindObjectsOfType<NetworkPlayerController>()
            .Where(npc => npc.netId == newId).FirstOrDefault();
    }

    private void Awake()
    {
        playerLabel = transform.Find("PlayerName")?.GetComponent<Text>();
        timeLeftCircle = transform.Find("Avatar/Counter")?.GetComponent<Image>();
        cardsLeftText = transform.Find("CardsLeft/Text")?.GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();
        uiCanvas = GameObject.Find("Canvas")?.transform;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        connectionId = connectionToClient.connectionId;
        initialTimeLeft = Manager.InitialTimeLeftPerPlayer;
        isMyTurn = false;
        timeLeft = 0f;

        if(Manager.playerNames.ContainsKey(connectionId))
        {
            playerName = Manager.playerNames[connectionId].name;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        transform.SetParent(uiCanvas);
        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
        transform.GetComponent<RectTransform>().localScale = new Vector3(0.75f, 0.75f, 0.75f);
        // Hack - hides the player panel from being visible from the lobby by rendering it behind the lobby game object
        transform.SetAsFirstSibling();

        // TODO: to support headless mode, remove the need to have client-side copy references of player panels
        if (isClientOnly)
        {
            Manager.playerPanels[connectionId] = this;
        }

        if(!isMyTurn)
        {
            timeLeftCircle.gameObject.SetActive(false);
        }
    }


    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        anchorPreset = AnchorPresets.BOTTOM_LEFT;
        AnchorPresetsUtils.AssignAnchor(AnchorPresets.BOTTOM_LEFT, ref rectTransform);
        rectTransform.anchoredPosition = new Vector2(0f, 30f);

        if(networkPlayerController != null)
        {
            Debug.Log("Registering event delegate");
            EventEndTurn += networkPlayerController.MoveCardsDown;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if(isClientOnly)
        {
            if(Manager.playerPanels.ContainsKey(connectionId))
            {
                Manager.playerPanels.Remove(connectionId);
            }
        }

        if (hasAuthority)
        {
            if (networkPlayerController != null)
            {
                EventEndTurn -= networkPlayerController.MoveCardsDown;
            }
        }
    }

    [Server]
    public void StartCountdown(int connectionId)
    {
        StopAllCoroutines();
        RpcToggleTimerUI(true);
        StartCoroutine(CountdownRoutine(connectionId));
    }

    [Server]
    public void StopCountdown(int connectionId)
    {
        isMyTurn = false;
        RpcToggleTimerUI(false);
        EventEndTurn();

        // Single Player Mode only
        if (NetworkServer.connections.Count == 1)
        {
            StopAllCoroutines();
        }
        else
        {
            StopCoroutine(CountdownRoutine(connectionId));
        }
    }

    // Server Side only
    private IEnumerator CountdownRoutine(int connectionId)
    {
        isMyTurn = true;
        timeLeft = initialTimeLeft;

        while (timeLeft > 0)
        {
            // if somewhere else in the code sets isMyTurn to false... break out of this loop
            if (!isMyTurn)
            {
                yield break;
            }

            // subtraction here accounts for delays that the client will receive the time to update the timer panel
            yield return new WaitForSeconds(syncInterval - .01f);
            timeLeft -= syncInterval;
        }

        timeLeft = 0f;
        
        // Need this bit of logic so that the turn system can automatically go to the next
        // player's turn in the event the current player lets all X seconds elapse
        // otherwise game will not enable controls for the next player's turn
        if (isMyTurn)
        {
            isMyTurn = false;
            Manager.GoToNextTurn();
        }

    }

    [Server]
    public void SetNetworkPlayerControllerNetId(uint netId)
    {
        networkPlayerControllerNetId = netId;
    }

    [ClientRpc]
    private void RpcToggleTimerUI(bool active)
    {
        timeLeftCircle.fillAmount = 1f;
        timeLeftCircle.gameObject.SetActive(active);
    }
}
