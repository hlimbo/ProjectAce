using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ProjectAce;
using Mirror;
using DG.Tweening;

public class NetworkPlayerController : NetworkBehaviour
{
    // Client side objects/prefabs
    private GameObject cardPrefab;
    private GameObject opponentCardPrefab;
    private Transform parent;

    private GameObject cardHandGroup;
    private GameObject comboButton;
    private GameObject endTurnButton;

    // Available client-side only
    [SerializeField]
    private List<CardController> hand;
    private Transform faceUpHolder;
    private Queue<GameObject> cardsToDraw = new Queue<GameObject>();
    private Queue<Card> cardValuesToDraw = new Queue<Card>();
    private bool isCoroutineRunning = false;
    private Queue<GameObject> placeholders = new Queue<GameObject>();
    private Transform drawPileGraphic;

    [SerializeField]
    private AudioManager audioManager;

    [System.Serializable]
    public class SyncListCards : SyncList<Card> { }
    public readonly SyncListCards myCards = new SyncListCards();

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
    private OpponentCardMatManager opponentCardMatManager;

    [SyncVar]
    private int connectionId;
    public int ConnectionId => connectionId;
    [SyncVar]
    public bool hasPlayedCardOrComboThisTurn;

    [SerializeField]
    private GameObject verticalFaceDownCardPrefab;
    [SerializeField]
    private GameObject horizontalFaceDownCardPrefab;

    // Can belong to either the opponent or current player
    private GameObject cardMat;
    public GameObject CardMat => cardMat;

    // if game is being self-hosted, then this button only belongs to the hosting game client. During headless mode, all clients will have the ability to reset the game on end
    // as long as they are the game leader
    public Button playAgainButton;

    // Is set by the server only when it is this player's turn
    private bool canEnableComboButton = false;

    private void Awake()
    {
        opponentCardMatManager = FindObjectOfType<OpponentCardMatManager>();
        cardHandGroup = GameObject.Find("CardHandGroup");
        drawPileGraphic = GameObject.Find("DrawPileGraphic")?.transform;
        // can be replaced with cardMat and grab cardhandgroup via cardMat instead
        parent = cardHandGroup?.transform;

        // Since this is a NetworkBehaviour it will load these resources per instance which is bad
        // I only want to load this resource once!!!!
        // move this to a monobehaviour script that makes this call on SampleScene only
        cardPrefab = Resources.Load<GameObject>("Prefabs/CardV3");
        opponentCardPrefab = Resources.Load<GameObject>("Prefabs/OpponentFaceUpCard");
        hand = new List<CardController>();
        hand.Clear();
        faceUpHolder = GameObject.Find("FaceUpHolder")?.transform;

        audioManager = GameObject.Find("AudioManager")?.GetComponent<AudioManager>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        connectionId = connectionToClient.connectionId;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (NetworkClient.connection != null)
        {
            Debug.Log("My connectionId: " + connectionId);
        }

        if (isClientOnly)
        {
            Manager.networkPlayerControllers[connectionId] = this;
        }

        if (!hasAuthority)
        {
            myCards.Callback += OnClientOpponentCardsUpdated;
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        cardMat = GameObject.Find("PlayerCardMat");
        comboButton = GameObject.Find("ComboButton");
        endTurnButton = GameObject.Find("EndTurnButton");

        comboButton?.SetActive(false);
        endTurnButton?.SetActive(false);

        comboButton?.GetComponent<Button>().onClick.AddListener(OnComboButtonSelected);
        endTurnButton?.GetComponent<Button>().onClick.AddListener(OnEndTurnButtonSelected);

        myCards.Callback += OnClientMyCardsUpdated;
    }

    public void MoveCardsDown()
    {
        Debug.Log("MOVING CARDS DOWN!!!!!!! ON NPC EVENT END TURN");
        foreach (var card in hand)
        {
            if (card.IsRaised)
            {
                card.MoveBackToOriginalLocalPosition();
            }
            card.DisableInteraction();
        }
    }

    public void DisableCards()
    {
        foreach (var card in hand)
        {
            card.DisableInteraction();
        }
    }

    [TargetRpc]
    public void TargetMoveRaisedCardsDown(NetworkConnection clientConnection)
    {
        List<CardController> raisedCards = hand.Where(card => card.IsRaised).ToList();
        StartCoroutine(CheckIfAllCardsPutBackInHand(raisedCards));
    }

    private IEnumerator CheckIfAllCardsPutBackInHand(List<CardController> raisedCards)
    {
        foreach (var card in raisedCards)
        {
            card.MoveBackToOriginalLocalPosition();
        }

        bool areAllCardsMovedBack = false;
        while (!areAllCardsMovedBack)
        {
            areAllCardsMovedBack = true;
            foreach (var card in raisedCards)
            {
                areAllCardsMovedBack = areAllCardsMovedBack && card.isDoneMovingBack;
            }

            yield return null;
        }
    }

    [Server]
    public void CheckIfPlayerDrawsACard()
    {
        if (hasPlayedCardOrComboThisTurn)
        {
            hasPlayedCardOrComboThisTurn = false;
        }
        else
        {
            Manager.DealerGiveCardToPlayer(connectionId);
        }
    }

    private void OnClientOpponentCardsUpdated(SyncListCards.Operation op, int index, Card oldCard, Card newCard)
    {
        // cardMat gameObject reference won't be available until game completes starting up
        if (cardMat == null)
        {
            return;
        }

        if (op == SyncListCards.Operation.OP_ADD)
        {
            //Debug.Log("OnClientOpponentCardsUpdated adding Card: " + newCard);
            cardMat.GetComponent<OpponentCardMat>()?.SpawnCard();
        }
        else if (op == SyncListCards.Operation.OP_REMOVEAT)
        {
            cardMat.GetComponent<OpponentCardMat>()?.DestroyCard(index);
        }
    }

    private void DrawCardsAnimation()
    {
        if(!isCoroutineRunning)
        {
            StartCoroutine(DrawCardsRoutine());
            isCoroutineRunning = true;
        }
    }

    private IEnumerator DrawCardsRoutine()
    {
        // Recalculate all child elements within the parent container and wait until end of frame 
        // for layout to rebuild all child element positions
        LayoutRebuilder.MarkLayoutForRebuild(parent.GetComponent<RectTransform>());
        yield return new WaitForEndOfFrame();

        int totalCardCount = cardsToDraw.Count;
        while (cardsToDraw.Count > 0)
        {
            var placeholder = placeholders.Dequeue();
            var myNewCard = cardsToDraw.Dequeue();
            var newCard = cardValuesToDraw.Dequeue();
            var cardController = myNewCard.GetComponent<CardController>();

            placeholder.GetComponent<LayoutElement>().ignoreLayout = true;
            placeholder.transform.SetParent(drawPileGraphic);
            placeholder.transform.localScale = new Vector3(1f, 1f, 1f);
            var cardRt = placeholder.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, 0f);
            placeholder.transform.SetParent(parent);

            placeholder.transform.DOLocalMove(myNewCard.transform.localPosition, 1.25f, true).OnComplete(() =>
            {
                var myColor = myNewCard.GetComponent<Image>().color;
                myNewCard.GetComponent<Image>().color = new Color(myColor.r, myColor.g, myColor.b, 1f);
                cardController.Initialize(this, newCard);
                Destroy(placeholder);
            });

            yield return new WaitForSeconds(0.5f);
        }

        isCoroutineRunning = false;
        
        // Check if player is receiving their hand when the game first starts
        // This is done to prevent calling TargetEnableControls multiple times
        if(isReceivingInitialHand)
        {
            CmdCheckForTurn();
            isReceivingInitialHand = false;
        }
    }

    // Goal: separate client-side only related variables from host/network dependent variables
    private void OnClientMyCardsUpdated(SyncListCards.Operation op, int index, Card oldCard, Card newCard)
    {
        CmdUpdateNumberOfCardsLeft(connectionId, myCards.Count);
        if (op == SyncListCards.Operation.OP_ADD)
        {
            Debug.Log("Addding card: " + newCard);
            GameObject myNewCard = Instantiate(cardPrefab);
            var newCardController = myNewCard.GetComponent<CardController>();
            if(newCardController != null)
            {
                hand.Add(newCardController);
            }

            // Draw Card Animation
            GameObject placeholder = new GameObject("placeholder");
            var r = placeholder.AddComponent<RectTransform>();
            var l = placeholder.AddComponent<LayoutElement>();
            var i = placeholder.AddComponent<Image>();
            l.preferredWidth = myNewCard.GetComponent<LayoutElement>().preferredWidth;
            l.preferredHeight = myNewCard.GetComponent<LayoutElement>().preferredHeight;
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, l.preferredWidth);
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, l.preferredHeight);
            i.sprite = Utils.cardAssets[newCard.ToString()];
            i.raycastTarget = false;

            // set to empty and fill in after animation is done
            myNewCard.GetComponent<Image>().sprite = null;
            var myColor = myNewCard.GetComponent<Image>().color;
            myNewCard.GetComponent<Image>().color = new Color(myColor.r, myColor.g, myColor.b, 0f);
            myNewCard.transform.SetParent(parent);
            myNewCard.transform.localScale = new Vector3(1f, 1f, 1f);

            cardsToDraw.Enqueue(myNewCard);
            cardValuesToDraw.Enqueue(newCard);
            placeholders.Enqueue(placeholder);
        }
    }

    [Command]
    private void CmdCheckGameStatus()
    {
        Manager.CheckGameStatus(connectionId);
    }

    [Command]
    private void CmdUpdateNumberOfCardsLeft(int connectionId, int cardCount)
    {
        if(Manager.playerPanels.ContainsKey(connectionId))
        {
            Manager.playerPanels[connectionId].SetCardsLeft(cardCount);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if(!hasAuthority && cardMat != null)
        {
            opponentCardMatManager.UnregisterMat(connectionId);
            myCards.Callback -= OnClientOpponentCardsUpdated;
        }

        if(hasAuthority)
        {
            myCards.Callback -= OnClientMyCardsUpdated;
        }

        if (isClientOnly)
        {
            if(Manager.networkPlayerControllers.ContainsKey(connectionId))
            {
                Manager.networkPlayerControllers.Remove(connectionId);
            }
        }
    }

    private float[] rotations = new float[] { 0f, 15f, 30f, -15f, -30f };

    [TargetRpc]
    public void TargetRemoveMyCardFromHand(NetworkConnection clientConnection, Card card, int animIndex)
    {
        if(hand.Count == 0)
        {
            Debug.LogWarningFormat("Hand Count for this connection id {0} is empty", connectionId);
            return;
        }

        CardController cardToRemove = hand.Where(h => h.card.Equals(card)).FirstOrDefault();
        if(cardToRemove == null)
        {
            Debug.LogWarningFormat("TargetRemoveMyCardFromHand: could not find card {0} in hand", card);
            return;
        }

        cardToRemove.isPlacedOnTable = true;
        cardToRemove.MoveToTargetPosition(faceUpHolder, rotations[animIndex]);

        audioManager.PlayClip("cardPlacedOnTable");

        hand.Remove(cardToRemove);
        CmdRemoveCard(card);
    }

    [ClientRpc(excludeOwner = true)]
    public void RpcRemoveOpponentCardFromHand(Card card, int animIndex)
    {
        audioManager.PlayClip("cardPlacedOnTable");

        // Opponent Card Animation //
        Debug.LogFormat("RpcRemoveOpponentCardFromHand: removing card {0}", card);
        var cardGO = Instantiate(opponentCardPrefab, faceUpHolder, false);
        var rectTransform = cardGO.GetComponent<RectTransform>();
        AnchorPresetsUtils.AssignAnchor(AnchorPresets.MIDDLE_CENTER, ref rectTransform);
        cardGO.GetComponent<Image>().sprite = Utils.cardAssets[card.ToString()];
        var opponentCard = cardGO.GetComponent<OpponentFaceUpCard>();
        opponentCard.PlayAnimation(animIndex);
    }

    private Queue<Card> pendingCardsToRemove = new Queue<Card>();
    private Queue<int> pendingAnimIndices = new Queue<int>();
    private bool isRoutineRunning = false;
    private IEnumerator RemoveCardsRoutine()
    {
        while(pendingCardsToRemove.Count > 0)
        {
            var card = pendingCardsToRemove.Dequeue();
            int animIndex = pendingAnimIndices.Dequeue();

            var c = Instantiate(opponentCardPrefab, faceUpHolder, false);
            c.GetComponent<Image>().sprite = Utils.cardAssets[card.ToString()];
            c.transform.SetParent(faceUpHolder);
            var rectTransform = c.GetComponent<RectTransform>();
            AnchorPresetsUtils.AssignAnchor(AnchorPresets.MIDDLE_CENTER, ref rectTransform);

            if(c.GetComponent<OpponentFaceUpCard>() != null)
            {
                c.GetComponent<OpponentFaceUpCard>().PlayAnimation(animIndex);
            }

            audioManager.PlayClip("cardPlacedOnTable");

            yield return new WaitForSeconds(0.75f);
        }

        isRoutineRunning = false;
    }

    private void RunCardsAnimsRoutine()
    {
        if(!isRoutineRunning)
        {
            isRoutineRunning = true;
            StartCoroutine(RemoveCardsRoutine());
        }
    }

    [TargetRpc]
    public void TargetRemoveCardsFromMyHand(NetworkConnection clientConnection, Card[] cards, int[] animIndices)
    {
        for (int i = 0; i < cards.Length; ++i)
        {
            pendingCardsToRemove.Enqueue(cards[i]);
            pendingAnimIndices.Enqueue(animIndices[i]);
        }

        var cardSelectorsToRemove = hand.Where(h => cards.Contains(h.card)).ToList();
        Stack<GameObject> removals = new Stack<GameObject>();
        foreach (var c in cardSelectorsToRemove)
        {
            c.ToggleClickHandlerBehaviour(false);
            c.ToggleDragHandlerBehaviour(false);
            hand.Remove(c);
            removals.Push(c.gameObject);
        }

        // Possible Optimization here could be to pool the cards used for the cards the player owns instead of destroying it all the time
        while (removals.Count > 0)
        {
            GameObject g = removals.Pop();
            Destroy(g);
        }

        RunCardsAnimsRoutine();
    }

    [ClientRpc(excludeOwner = true)]
    public void RpcRemoveOpponentCardsFromHand(Card[] cards, int[] animIndices)
    {
        for (int i = 0; i < cards.Length; ++i)
        {
            pendingCardsToRemove.Enqueue(cards[i]);
            pendingAnimIndices.Enqueue(animIndices[i]);
        }

        RunCardsAnimsRoutine();
    }


    [ClientRpc]
    public void RpcEnablePlayAgainPanel(int clientConnectionId, string winnerName)
    {
        if(Manager.PlayAgainPanel != null)
        {
            Manager.PlayAgainPanel.gameObject.SetActive(true);
            Manager.PlayAgainPanel.SetWinnerText(winnerName);
            Manager.PlayAgainPanel.SetWinningAvatar(connectionId);
        }
    }

    [TargetRpc]
    public void TargetOnClientPlayGameOverSound(NetworkConnection clientConnection, string clipName)
    {
        audioManager.PlayClip(clipName);
    }

    [Command]
    private void CmdRemoveCard(Card card)
    {
        myCards.Remove(card);
    }

    private bool isReceivingInitialHand;
    [TargetRpc]
    public void TargetPlayerReceivesInitialHand(NetworkConnection clientConnection)
    {
        isReceivingInitialHand = true;
    }

    [TargetRpc]
    public void TargetEnableControls(NetworkConnection clientConnection)
    {
        Debug.Log("TargetEnableControls called");
        canEnableComboButton = true;
        endTurnButton.SetActive(true);

        foreach(var card in hand)
        {
            card.ToggleClickHandlerBehaviour(true);
        }

        audioManager.PlayClip("turnNotification");
    }

    [TargetRpc]
    public void TargetDisableControls(NetworkConnection clientConnection)
    {
        canEnableComboButton = false;
        comboButton.SetActive(false);
        endTurnButton.SetActive(false);
        DisableCards();
    }

    [TargetRpc]
    public void TargetCardPlacementFailed(NetworkConnection clientConnection, Card card)
    {
        CardController cardController = hand.Where(h => h.card.Equals(card)).FirstOrDefault();
        if(cardController != null)
        {
            audioManager.PlayClip("cardShove");
            cardController.MoveBackToOriginalLocalPosition();
        }
    }

    [Client]
    private void OnComboButtonSelected()
    {
        if(hasAuthority)
        {
            Card[] selectedCards =
                hand.Where(cardSelector => cardSelector.IsRaised)
                    .Select(cardSelector => cardSelector.card).ToArray();

            CmdSelectedCardsToCombo(connectionId, selectedCards);
        }
    }

    [Command]
    private void CmdSelectedCardsToCombo(int clientConnectionId, Card[] cards)
    {
        Manager.DealerEvaluateCardsToCombo(clientConnectionId, cards);
    }

    [TargetRpc]
    public void TargetOnComboInvalid(NetworkConnection connection, Card[] cards)
    {
        // Safeguard to prevent other network player controllers from moving other player cards down
        if (!hasAuthority) return;

        // Move cards down
        foreach(var cardSelector in hand)
        {
            foreach(var card in cards)
            {
                if(cardSelector.card.Equals(card))
                {
                    cardSelector.MoveBackToOriginalLocalPosition();
                }
            }
        }
    }

    [Command]
    private void CmdOnEndTurnButtonSelected(int connectionId)
    {
        Manager.GoToNextTurn();
    }

    private void OnEndTurnButtonSelected()
    {
        CmdOnEndTurnButtonSelected(connectionId);
    }

    private void OnHostPlayAgainPressed()
    {
        Manager.PlayAgainPanel.gameObject.SetActive(false);
        CmdOnPlayAgainPressed();
    }

    [Command]
    private void CmdOnPlayAgainPressed()
    {
        manager.PlayGameAgain();
    }

    [TargetRpc]
    public void TargetHostEnablePlayAgainButton(NetworkConnection clientConnection)
    {
        if (Manager.PlayAgainPanel != null)
        {
            Manager.PlayAgainPanel.gameObject.SetActive(true);
            if(playAgainButton == null)
            {
                playAgainButton = Manager.OptionsPanel.transform.Find("PlayAgainButton")?.GetComponent<Button>();
            }

            if(playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnHostPlayAgainPressed);
            }
        }
    }

    [Command]
    public void CmdSendCardToDealer(Card card)
    {
        Manager.TryAddCardToFaceUpPile(connectionId, card);
    }

    [ClientCallback]
    private void Update()
    {
        if(hasAuthority && canEnableComboButton)
        {
            Card[] selectedCards =
                hand.Where(cardSelector => cardSelector.IsRaised)
                    .Select(cardSelector => cardSelector.card).ToArray();
            
            if(selectedCards.Length >= 2)
            {
                comboButton.SetActive(true);
            }
            else
            {
                comboButton.SetActive(false);
            }
        }

        if(cardsToDraw.Count > 0)
        {
            DrawCardsAnimation();
        }
    }

    [Command]
    private void CmdCheckForTurn()
    {
        // will start timer if it is currently the player's turn
        Manager.CheckForTurn(connectionId);
    }

    [ClientRpc]
    public void RpcOnClientStartGame()
    {
        if (!hasAuthority)
        {
            opponentCardMatManager.RegisterMats();
            cardMat = opponentCardMatManager.GetCardMat(connectionId);
            if (cardMat == null)
            {
                Debug.LogError("NetworkPlayerController could not register an opponent card mat. All opponent card mats are taken");
            }
            else
            {
                // Spawn cards as soon as server triggers this rpc to assign a valid cardMat gameObject reference
                for(int i = 0;i < myCards.Count; ++i)
                {
                    cardMat.GetComponent<OpponentCardMat>()?.SpawnCard();
                }
            }
        }
    }

    [TargetRpc]
    public void TargetOnClientPlayDealCardSounds(NetworkConnection clientConnection)
    {
        StartCoroutine(DealCardSounds());
    }

    private IEnumerator DealCardSounds()
    {
        for (int i = 0; i < Manager.StartingCardCountPerPlayer; ++i)
        {
            audioManager.PlayClip("drawCard");
            yield return new WaitForSeconds(audioManager.GetCurrentClipDuration());
        }
    }

    [TargetRpc]
    public void TargetOnClientPlayDrawCardSound(NetworkConnection clientConnection)
    {
        audioManager.PlayClip("drawCard");
    }
}
