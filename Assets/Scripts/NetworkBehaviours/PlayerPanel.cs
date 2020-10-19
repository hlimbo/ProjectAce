using Mirror;
using UnityEngine;
using UnityEngine.UI;
using ProjectAce;
using System.Collections;
using System.Linq;
using DG.Tweening;

public class PlayerPanel : NetworkBehaviour
{
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
    private Image avatarImage;
    private Text playerLabel;
    private Text cardsLeftText;

    private Color originalLabelColor;

    private Image counterFx;
    private Sequence pulseSequence;

    public AnchorPresets anchorPreset;

    [SyncVar]
    public int playerNumber;

    [SyncVar(hook=nameof(OnClientReceiveAvatarName))]
    public string avatarName;

    private void OnClientReceiveAvatarName(string _, string newName)
    {
        if (Utils.avatarAssets.ContainsKey(newName))
        {
            avatarImage.sprite = Utils.avatarAssets[newName];
        }
    }

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

    private void Awake()
    {
        playerLabel = transform.Find("PlayerName")?.GetComponent<Text>();
        timeLeftCircle = transform.Find("Avatar/Counter")?.GetComponent<Image>();
        counterFx = transform.Find("CounterFX")?.GetComponent<Image>();
        avatarImage = transform.Find("Avatar/PlayerImage")?.GetComponent<Image>();
        cardsLeftText = transform.Find("CardsLeft/Text")?.GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();
        uiCanvas = GameObject.Find("Canvas")?.transform;

        pulseSequence = DOTween.Sequence()
            .Append(counterFx.transform.DOScaleX(1.25f, 1f))
            .Join(counterFx.transform.DOScaleY(1.25f, 1f))
            .Join(counterFx.DOFade(0f, 1.5f))
            .SetLoops(-1, LoopType.Restart)
            .Pause();

        originalLabelColor = playerLabel.color;

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
        transform.GetComponent<RectTransform>().localScale = new Vector3(0.8f,0.8f,0.8f);
        // Hack - hides the player panel from being visible from the lobby by rendering it behind the lobby game object
        transform.SetAsFirstSibling();

        if (!isMyTurn)
        {
            timeLeftCircle.gameObject.SetActive(false);
        }
    }


    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        anchorPreset = AnchorPresets.BOTTOM_LEFT;
        AnchorPresetsUtils.AssignAnchor(AnchorPresets.BOTTOM_LEFT, ref rectTransform);
    }

    [Server]
    public void StartCountdown()
    {
        StopAllCoroutines();
        RpcToggleTimerUI(true);
        RpcToggleHighlightPlayerLabel(true);
        StartCoroutine(CountdownRoutine(syncInterval));
    }

    [Server]
    public void StopCountdown(int connectionId)
    {
        isMyTurn = false;
        RpcToggleTimerUI(false);
        RpcToggleHighlightPlayerLabel(false);

        // When only 1 client is actively connected to the server
        if (NetworkServer.connections.Count == 1)
        {
            StopAllCoroutines();
        }
        else
        {
            StopCoroutine(CountdownRoutine(syncInterval));
        }
    }

    // Server Side only
    private IEnumerator CountdownRoutine(float delta)
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
            yield return new WaitForSeconds(delta - .01f);
            timeLeft -= delta;
        }

        timeLeft = 0f;
        
        // Need this bit of logic so that the turn system can automatically go to the next
        // player's turn in the event the current player lets all X seconds elapse
        // otherwise game will not enable controls for the next player's turn
        if (isMyTurn)
        {
            isMyTurn = false;
            Manager.CheckPendingPile(connectionId);
        }
    }

    [ClientRpc]
    private void RpcToggleTimerUI(bool active)
    {
        timeLeftCircle.fillAmount = 1f;
        timeLeftCircle.gameObject.SetActive(active);

        if (active)
        {
            pulseSequence.Play();    
        }
        else
        {
            pulseSequence.Rewind();
        }

    }

    [ClientRpc]
    private void RpcToggleHighlightPlayerLabel(bool toggle)
    {
        if(toggle)
        {
            playerLabel.color = Color.yellow;
            playerLabel.fontStyle = FontStyle.Bold;
        }
        else
        {
            playerLabel.color = originalLabelColor;
            playerLabel.fontStyle = FontStyle.Normal;
        }
    }
}
