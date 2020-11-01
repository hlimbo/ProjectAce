using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ProjectAce;
using Mirror;
using DG.Tweening;

public class NetworkPlayerController : NetworkBehaviour, IPlayerController
{
    // Client side objects/prefabs
    private GameObject cardPrefab;
    private GameObject opponentCardPrefab;

    private Transform cardHandGroup;
    private GameObject confirmSelectionButton;
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

    // if game is being self-hosted, then this button only belongs to the hosting game client. During headless mode, all clients will have the ability to reset the game on end
    // as long as they are the game leader
    public Button playAgainButton;

    private void Awake()
    {
        opponentCardMatManager = FindObjectOfType<OpponentCardMatManager>();
        cardHandGroup = GameObject.Find("CardHandGroup")?.transform;
        drawPileGraphic = GameObject.Find("DrawPileGraphic")?.transform;

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

        if (!hasAuthority)
        {
            myCards.Callback += OnClientOpponentCardsUpdated;
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        cardMat = GameObject.Find("PlayerCardMat");
        confirmSelectionButton = GameObject.Find("ConfirmSelectionButton");
        endTurnButton = GameObject.Find("EndTurnButton");

        confirmSelectionButton?.SetActive(false);
        endTurnButton?.SetActive(false);

        confirmSelectionButton?.GetComponent<Button>().onClick.AddListener(OnConfirmSelectionButtonPressed);
        endTurnButton?.GetComponent<Button>().onClick.AddListener(OnEndTurnButtonSelected);

        myCards.Callback += OnClientMyCardsUpdated;
    }

    [TargetRpc]
    public void TargetMovePendingCardsBack(NetworkConnection clientConnection, Card[] cards)
    {
        List<CardController> pendingCards = hand.Where(card => cards.Contains(card.card)).ToList();
        StartCoroutine(CheckIfAllCardsPutBackInHand(pendingCards));
    }

    private IEnumerator CheckIfAllCardsPutBackInHand(List<CardController> raisedCards)
    {
        foreach (var card in raisedCards)
        {
            card.MoveBackToHand();
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

        Debug.Log("Done Moving All Cards back in hand");
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

    private void OnClientMyCardsUpdated(SyncListCards.Operation op, int index, Card oldCard, Card newCard)
    {
        CmdUpdateNumberOfCardsLeft(connectionId, myCards.Count);
        if (op == SyncListCards.Operation.OP_ADD)
        {
            Debug.Log("Addding card: " + newCard);
            GameObject myNewCard = Instantiate(cardPrefab, cardHandGroup, false);
            myNewCard.transform.localScale = new Vector3(1f, 1f, 1f);

            var newCardController = myNewCard.GetComponent<CardController>();
            if (newCardController != null)
            {
                Debug.Log("Disabling card interaction");
                newCardController.ToggleInteraction(false);
                hand.Add(newCardController);
            }

            // edge case: when there is only 1 player connected to the server and is actively playing
            // do not disable the card.. only disable card when multiple players are connected to the game session
            if (isNewCardAdded)
            {
                isNewCardAdded = false;
                newCardController.ToggleBlockRaycasts(true);
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

            cardsToDraw.Enqueue(myNewCard);
            cardValuesToDraw.Enqueue(newCard);
            placeholders.Enqueue(placeholder);
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
        LayoutRebuilder.MarkLayoutForRebuild(cardHandGroup.GetComponent<RectTransform>());
        yield return new WaitForEndOfFrame();

        var recentlyAddedCards = new Stack<CardController>();
        while (cardsToDraw.Count > 0)
        {
            var placeholder = placeholders.Dequeue();
            var myNewCard = cardsToDraw.Dequeue();
            var newCard = cardValuesToDraw.Dequeue();
            var cardController = myNewCard.GetComponent<CardController>();
            recentlyAddedCards.Push(cardController);

            placeholder.GetComponent<LayoutElement>().ignoreLayout = true;
            placeholder.transform.SetParent(drawPileGraphic);
            placeholder.transform.localScale = new Vector3(1f, 1f, 1f);
            var cardRt = placeholder.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, 0f);

            // Need to wait 1 frame here to ensure the card's new position can be calculated by Hand.cs successfully
            myNewCard.transform.SetParent(cardHandGroup);
            yield return new WaitForEndOfFrame();

            placeholder.transform.SetParent(cardHandGroup);
            placeholder.transform.DOLocalMove(myNewCard.transform.localPosition, 1.25f, true).OnComplete(() =>
            {
                var myColor = myNewCard.GetComponent<Image>().color;
                myNewCard.GetComponent<Image>().color = new Color(myColor.r, myColor.g, myColor.b, 1f);
                cardController.Initialize(this, newCard);

                if (!isReceivingInitialHand)
                {
                    cardController.ToggleInteraction(true);
                }

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
            while (recentlyAddedCards.Count > 0)
            {
                Debug.Log("Enabling card");
                var cardController = recentlyAddedCards.Pop();
                cardController.ToggleInteraction(true);
            }
        }
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
            if(confirmSelectionButton != null)
            {
                confirmSelectionButton.GetComponent<Button>()?.onClick.RemoveListener(OnConfirmSelectionButtonPressed);
            }

            if(endTurnButton != null)
            {
                endTurnButton.GetComponent<Button>()?.onClick.RemoveListener(OnEndTurnButtonSelected);
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

        cardToRemove.MoveToTargetPosition(faceUpHolder, rotations[animIndex]);
        cardToRemove.DestroyInteractiveComponents();
        cardToRemove.DestroyPlaceholder();
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

            yield return new WaitForSeconds(audioManager.GetCurrentClipDuration());
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
            c.DestroyInteractiveComponents();
            hand.Remove(c);
            removals.Push(c.gameObject);
        }

        // Possible Optimization here could be to pool the cards used for the cards the player owns instead of destroying it all the time
        while (removals.Count > 0)
        {
            GameObject g = removals.Pop();
            g.GetComponent<CardController>().DestroyPlaceholder();
            Destroy(g);
        }

        // auto plays drop combo animation
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
            Manager.PlayAgainPanel.SetWinningAvatarMulti(connectionId);
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
        endTurnButton.SetActive(true);
        audioManager.PlayClip("turnNotification");
    }

    [TargetRpc]
    public void TargetDisableControls(NetworkConnection clientConnection)
    {
        confirmSelectionButton.SetActive(false);
        endTurnButton.SetActive(false);
    }

    [TargetRpc]
    public void TargetCardPlacementFailed(NetworkConnection clientConnection, Card card)
    {
        CardController cardController = hand.Where(h => h.card.Equals(card)).FirstOrDefault();
        if(cardController != null)
        {
            audioManager.PlayClip("cardShove");
            cardController.MoveBackToHand();
        }
    }

    [Client]
    private void OnConfirmSelectionButtonPressed()
    {
        if(hasAuthority)
        {
            CmdVerifyConfirmedSelection(connectionId);
        }
    }

    [Command]
    private void CmdVerifyConfirmedSelection(int clientConnectionId)
    {
        Manager.VerifyConfirmedSelection(clientConnectionId);
    }

    [TargetRpc]
    public void TargetOnComboInvalid(NetworkConnection connection, Card[] cards)
    {
        // Safeguard to prevent other network player controllers from moving other player cards down
        if (!hasAuthority) return;

        // Move cards down
        var selectedCards = hand.Where(c => cards.Contains(c.card)).ToArray();
        foreach(var card in selectedCards)
        {
            card.MoveBackToHand();
        }
    }

    [Command]
    private void CmdOnEndTurnButtonSelected(int connectionId)
    {
        Manager.CheckPendingPile(connectionId);
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
        Manager.PlayGameAgain();
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
    private void CmdSendCardToDealer(Card card)
    {
        TargetToggleConfirmSelectionButton(connectionToClient, true);
        Manager.AddCardToPendingPile(connectionId, card);
    }

    [TargetRpc]
    public void TargetToggleConfirmSelectionButton(NetworkConnection clientConnection, bool toggle)
    {
        confirmSelectionButton.SetActive(toggle);
    }

    void IPlayerController.SendCardToDealer(Card card)
    {
        CmdSendCardToDealer(card);
    }

    [ClientCallback]
    private void Update()
    {
        // TODO: perhaps have this coroutine only called when specific events get triggered instead of checking
        // every frame if draw card animations is valid
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
    public void TargetOnClientPlayDealCardSounds(NetworkConnection clientConnection, int cardsCount)
    {
        StartCoroutine(DealCardSounds(cardsCount));
    }

    private IEnumerator DealCardSounds(int cardsCount)
    {
        for (int i = 0; i < cardsCount; ++i)
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

    private bool isNewCardAdded = false;
    [TargetRpc]
    public void TargetDidAddNewCard(NetworkConnection clientConnection, int activePlayerCount)
    {
        Debug.Log("TargetDidAddNewCard Active Player Count: " + activePlayerCount);
        isNewCardAdded = activePlayerCount > 1;
    }
}
