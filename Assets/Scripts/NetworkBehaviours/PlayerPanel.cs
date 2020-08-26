using Mirror;
using UnityEngine;
using UnityEngine.UI;
using ProjectAce;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    private GameObject timeLeftLabel;
    private GameObject timerPanel;
    private Text timeLeftText;
    private Image timeLeftCircle;
    private Text playerLabel;
    private Text cardsLeftText;

    public AnchorPresets anchorPreset;

    [SyncVar]
    public int playerNumber;

    [SyncVar(hook = nameof(OnClientTimeLeftChanged))]
    private int timeLeft;
    [SyncVar]
    private int initialTimeLeft;

    private void OnClientTimeLeftChanged(int oldTimeLeft, int newTimeLeft)
    {
        timeLeftText.text = newTimeLeft.ToString();
        timeLeftCircle.fillAmount = (float)newTimeLeft / initialTimeLeft;
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
        playerLabel = transform.Find("PlayerLabel")?.GetComponent<Text>();
        timeLeftText = transform.Find("TimerPanel/Timer/TimeLeft")?.GetComponent<Text>();
        timeLeftCircle = transform.Find("TimerPanel/Timer")?.GetComponent<Image>();
        cardsLeftText = transform.Find("CardsLeft")?.GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();

        uiCanvas = GameObject.Find("Canvas")?.transform;
        timeLeftLabel = transform.Find("TimeLeftLabel")?.gameObject;
        timerPanel = transform.Find("TimerPanel")?.gameObject;
        timeLeftLabel.SetActive(false);
        timerPanel.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        connectionId = connectionToClient.connectionId;
        initialTimeLeft = Manager.InitialTimeLeftPerPlayer;
        isMyTurn = false;

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


        //if (!hasAuthority)
        //{

        //    if (activePlayerCount == 2)
        //    {
        //        // opposing player panel will always be hardcoded to the top-right anchor
        //        anchorPreset = AnchorPresets.TOP_RIGHT;
        //        AnchorPresetsUtils.AssignAnchor(anchorPreset, ref rectTransform);
        //        rectTransform.anchoredPosition = new Vector2(-50f, 0f);
        //    }
        //    else if (activePlayerCount > 2 && networkPlayerController != null)
        //    {
        //        var opponentCardMat = networkPlayerController.CardMat?.GetComponent<OpponentCardMat>();
        //        if(opponentCardMat != null)
        //        {
        //            Vector2 offset = new Vector2(0f, 0f);
        //            switch(opponentCardMat.anchorPreset)
        //            {
        //                case AnchorPresets.MIDDLE_LEFT:
        //                    anchorPreset = AnchorPresets.TOP_LEFT;
        //                    break;
        //                case AnchorPresets.MIDDLE_RIGHT:
        //                    anchorPreset = AnchorPresets.BOTTOM_RIGHT;
        //                    break;
        //                case AnchorPresets.TOP_CENTER:
        //                    anchorPreset = AnchorPresets.TOP_RIGHT;
        //                    offset.Set(-50f, 0f);
        //                    break;
        //                default:
        //                    Debug.LogError("PlayerPanel OnStartClient: could not assign a valid player panel anchor");
        //                    break;
        //            }

        //            AnchorPresetsUtils.AssignAnchor(anchorPreset, ref rectTransform);
        //            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x + offset.x, rectTransform.anchoredPosition.y + offset.y);
        //        }
        //    }
        //}
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

    [ClientRpc]
    private void RpcToggleTimerUI(bool active)
    {
        timerPanel?.SetActive(active);
        timeLeftLabel?.SetActive(active);
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

            yield return new WaitForSeconds(1f);
            timeLeft -= 1;
        }

        timeLeft = 0;
        
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

    [ClientRpc(excludeOwner = true)]
    public void RpcOnClientPlayerPanelSpawned(int activePlayerCount)
    {
        if(!hasAuthority)
        {
            if (activePlayerCount == 2)
            {
                // opposing player panel will always be hardcoded to the top-right anchor
                anchorPreset = AnchorPresets.TOP_RIGHT;
                AnchorPresetsUtils.AssignAnchor(anchorPreset, ref rectTransform);
                rectTransform.anchoredPosition = new Vector2(-50f, 0f);
            }
            else if (activePlayerCount > 2 && networkPlayerController != null)
            {
                var opponentCardMat = networkPlayerController.CardMat?.GetComponent<OpponentCardMat>();
                if (opponentCardMat != null)
                {
                    Vector2 offset = new Vector2(0f, 0f);
                    switch (opponentCardMat.anchorPreset)
                    {
                        case AnchorPresets.MIDDLE_LEFT:
                            anchorPreset = AnchorPresets.TOP_LEFT;
                            break;
                        case AnchorPresets.MIDDLE_RIGHT:
                            anchorPreset = AnchorPresets.BOTTOM_RIGHT;
                            break;
                        case AnchorPresets.TOP_CENTER:
                            anchorPreset = AnchorPresets.TOP_RIGHT;
                            offset.Set(-50f, 0f);
                            break;
                        default:
                            Debug.LogError("PlayerPanel OnStartClient: could not assign a valid player panel anchor");
                            break;
                    }

                    AnchorPresetsUtils.AssignAnchor(anchorPreset, ref rectTransform);
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x + offset.x, rectTransform.anchoredPosition.y + offset.y);
                }
            }
        }
    }
}
