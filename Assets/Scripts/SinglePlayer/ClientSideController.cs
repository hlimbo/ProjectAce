using ProjectAce;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class ClientSideController : MonoBehaviour, IPlayerController
{
    [SerializeField]
    private GameObject cardPrefab;
    [SerializeField]
    private GameObject comboCardPrefab;

    [SerializeField]
    private List<CardController> hand;
    public List<Card> myCards = new List<Card>();
    public bool IsHandEmpty => hand.Count == 0;

    [SerializeField]
    private AudioManager audioManager;

    private Button confirmButton;
    private Button endTurnButton;

    private Transform faceUpHolder;
    private Transform cardHandGroup;
    private Transform drawPileGraphic;

    // Draw Card Animation Variables
    private bool isCoroutineRunning;
    private Queue<GameObject> placeholders = new Queue<GameObject>();
    private Queue<GameObject> cardsToDraw = new Queue<GameObject>();
    private Queue<Card> cardValuesToDraw = new Queue<Card>();

    public bool hasPlayedCardOrComboThisTurn;
    private bool isInitialTurn = true;

    private GameManager manager;
    public GameManager Manager
    {
        get
        {
            if(manager == null)
            {
                manager = FindObjectOfType<GameManager>();
            }

            return manager;
        }
    }

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();

        cardHandGroup = GameObject.Find("CardHandGroup")?.transform;
        drawPileGraphic = GameObject.Find("DrawPileGraphic")?.transform;
        faceUpHolder = GameObject.Find("FaceUpHolder")?.transform;

        endTurnButton = GameObject.Find("EndTurnButton")?.GetComponent<Button>();
        confirmButton = GameObject.Find("ConfirmButton")?.GetComponent<Button>();
    }

    private void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurnButtonSelected);
        confirmButton.onClick.AddListener(OnConfirmSelectionButtonSelected);
    }

    private void OnDestroy()
    {
        endTurnButton.onClick.RemoveListener(OnEndTurnButtonSelected);
        confirmButton.onClick.RemoveListener(OnConfirmSelectionButtonSelected);
    }

    public void RemoveCard(Card card, float targetRotation)
    {
        if (hand.Count == 0)
        {
            Debug.LogWarning("Hand Count is empty");
            return;
        }

        CardController cardToRemove = hand.Where(h => h.card.Equals(card)).FirstOrDefault();
        if (cardToRemove == null)
        {
            Debug.LogWarningFormat("TargetRemoveMyCardFromHand: could not find card {0} in hand", card);
            return;
        }

        cardToRemove.MoveToTargetPosition(faceUpHolder, targetRotation);
        cardToRemove.DestroyInteractiveComponents();

        audioManager.PlayClip("cardPlacedOnTable");
        hand.Remove(cardToRemove);
        myCards.Remove(card);
    }

    private bool isRoutineRunning = false;
    private Queue<Card> pendingCardsToRemove = new Queue<Card>();
    private Queue<int> pendingAnimIndices = new Queue<int>();
    public void RemoveCards(Card[] cards, int[] animIndices)
    {
        for (int i = 0; i < cards.Length; ++i)
        {
            pendingCardsToRemove.Enqueue(cards[i]);
            pendingAnimIndices.Enqueue(animIndices[i]);
        }

        foreach(var card in cards)
        {
            myCards.Remove(card);            
        }

        var cardControllersToRemove = hand.Where(c => cards.Contains(c.card)).ToList();
        Stack<GameObject> removals = new Stack<GameObject>();
        foreach (var c in cardControllersToRemove)
        {
            c.DestroyInteractiveComponents();
            hand.Remove(c);
            removals.Push(c.gameObject);
        }

        while(removals.Count > 0)
        {
            GameObject g = removals.Pop();
            Destroy(g);
        }

        RunCardsAnimsRoutine();
    }

    private void RunCardsAnimsRoutine()
    {
        if(!isRoutineRunning)
        {
            isRoutineRunning = true;
            StartCoroutine(RemoveCardsRoutine());
        }
    }

    private IEnumerator RemoveCardsRoutine()
    {
        while (pendingCardsToRemove.Count > 0)
        {
            var card = pendingCardsToRemove.Dequeue();
            int animIndex = pendingAnimIndices.Dequeue();

            var c = Instantiate(comboCardPrefab, faceUpHolder, false);
            c.GetComponent<Image>().sprite = Utils.cardAssets[card.ToString()];
            c.transform.SetParent(faceUpHolder);
            var rectTransform = c.GetComponent<RectTransform>();
            AnchorPresetsUtils.AssignAnchor(AnchorPresets.MIDDLE_CENTER, ref rectTransform);

            if (c.GetComponent<OpponentFaceUpCard>() != null)
            {
                c.GetComponent<OpponentFaceUpCard>().PlayAnimation(animIndex);
            }

            audioManager.PlayClip("cardPlacedOnTable");

            yield return new WaitForSeconds(audioManager.GetCurrentClipDuration());
        }

        isRoutineRunning = false;
    }

    public void EnableControls()
    {
        audioManager.PlayClip("turnNotification");

        endTurnButton.gameObject.SetActive(true);

        foreach(var card in hand)
        {
            card.ToggleDragHandlerBehaviour(true);
        }
    }

    public void DisableControls()
    {
        confirmButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
    }

    public void CardPlacementFailed(Card card)
    {
        Debug.Log("Failed to add: " + card);
        CardController cardController = hand.Where(c => c.card.Equals(card)).FirstOrDefault();

        if(cardController != null)
        {
            Debug.Log("card controller: " + cardController.card);
            audioManager.PlayClip("cardShove");
            cardController.MoveBackToOriginalLocalPosition();
        }

        confirmButton.gameObject.SetActive(false);
    }

    private void OnConfirmSelectionButtonSelected()
    {
        Manager.VerifyConfirmedSelection();
    }

    private void OnEndTurnButtonSelected()
    {
        Manager.CheckPendingPile();
    }

    public void OnComboInvalid(Card[] cards)
    {
        hand.Where(c => cards.Contains(c.card))
            .ToList()
            .ForEach(c => c.MoveBackToOriginalLocalPosition());

        confirmButton.gameObject.SetActive(false);
    }

    public IEnumerator PlayDealCardSounds()
    {
        for(int i = 0;i < Manager.StartingCardCountPerPlayer; ++i)
        {
            audioManager.PlayClip("drawCard");
            yield return new WaitForSeconds(audioManager.GetCurrentClipDuration());
        }
    }

    public void PlayDrawCardSound()
    {
        audioManager.PlayClip("drawCard");
    }

    public void PlayGameOverSound(string clipName)
    {
        audioManager.PlayClip(clipName);
    }

    public GameObject AddCard(Card card)
    {
        GameObject newCard = Instantiate(cardPrefab);
        var cardController = newCard.GetComponent<CardController>();
        if(cardController != null)
        {
            cardController.card = card;
            hand.Add(cardController);
        }

        return newCard;
    }

    public void PrepareDrawCardAnimation(GameObject newCard)
    {
        // Draw Card Animation
        GameObject placeholder = new GameObject("placeholder");
        var r = placeholder.AddComponent<RectTransform>();
        var l = placeholder.AddComponent<LayoutElement>();
        var i = placeholder.AddComponent<Image>();
        l.preferredWidth = newCard.GetComponent<LayoutElement>().preferredWidth;
        l.preferredHeight = newCard.GetComponent<LayoutElement>().preferredHeight;
        r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, l.preferredWidth);
        r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, l.preferredHeight);
        i.sprite = Utils.cardAssets[newCard.GetComponent<CardController>().card.ToString()];
        i.raycastTarget = false;

        // set to empty and fill in after animation is done
        newCard.GetComponent<Image>().sprite = null;
        newCard.GetComponent<CardController>().ToggleBlockRaycasts(true);
        var myColor = newCard.GetComponent<Image>().color;
        newCard.GetComponent<Image>().color = new Color(myColor.r, myColor.g, myColor.b, 0f);
        newCard.transform.SetParent(cardHandGroup);
        newCard.transform.localScale = new Vector3(1f, 1f, 1f);

        cardsToDraw.Enqueue(newCard);
        cardValuesToDraw.Enqueue(newCard.GetComponent<CardController>().card);
        placeholders.Enqueue(placeholder);
    }

    private void DrawCardsAnimation()
    {
        if(!isCoroutineRunning)
        {
            isCoroutineRunning = true;
            StartCoroutine(DrawCardsRoutine());
        }
    }

    private IEnumerator DrawCardsRoutine()
    {
        // Recalculate all child elements within the parent container and wait until end of frame 
        // for layout to rebuild all child element positions
        LayoutRebuilder.MarkLayoutForRebuild(cardHandGroup.GetComponent<RectTransform>());
        yield return new WaitForEndOfFrame();

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

            // Need to wait 1 frame here to ensure the card's new position can be calculated by Hand.cs successfully
            myNewCard.transform.SetParent(cardHandGroup);
            yield return new WaitForEndOfFrame();

            placeholder.transform.SetParent(cardHandGroup);
            placeholder.transform.DOLocalMove(myNewCard.transform.localPosition, 1.25f, true).OnComplete(() =>
            {
                var myColor = myNewCard.GetComponent<Image>().color;
                myNewCard.GetComponent<Image>().color = new Color(myColor.r, myColor.g, myColor.b, 1f);
                cardController.Initialize(this, newCard);
                Destroy(placeholder);
            });

            audioManager.PlayClip("drawCard");
            yield return new WaitForSeconds(audioManager.GetCurrentClipDuration());
        }

        isCoroutineRunning = false;

        // Check is needed here so that the audio clip to notify when its your turn doesn't get played twice
        if(isInitialTurn)
        {
            isInitialTurn = false;
            Manager.StartTurn();
        }

        foreach(var c in hand)
        {
            c.ToggleInteraction(true);
        }
    }

    public void OnCardsAdded(Card[] cards)
    {
        foreach(var card in cards)
        {
            var cardGO = AddCard(card);
            PrepareDrawCardAnimation(cardGO);
        }

        // Disable interaction when a card is being drawn 
        foreach (var c in hand)
        {
            c.ToggleInteraction(false);
        }

        DrawCardsAnimation();
    }

    public void OnCardAdded(Card card)
    {
        var cardGO = AddCard(card);

        // Disable interaction when a card is being drawn 
        foreach(var c in hand)
        {
            c.ToggleInteraction(false);
        }

        PrepareDrawCardAnimation(cardGO);
        DrawCardsAnimation();
    }

    public void CheckIfPlayerDrawsACard()
    {
        if (hasPlayedCardOrComboThisTurn)
        {
            hasPlayedCardOrComboThisTurn = false;
        }
        else
        {
            Manager.DealerGiveCardToPlayer();
        }
    }

    void IPlayerController.SendCardToDealer(Card card)
    {
        confirmButton.gameObject.SetActive(true);
        Manager.AddCardToPendingPile(card);
    }

    public void MoveToFaceUpPilePending(Card card, Transform faceUpPile)
    {
        CardController cardController = hand.Where(c => c.card.Equals(card)).FirstOrDefault();
        if (cardController != null)
        {
            cardController.MoveToTargetPosition(faceUpPile, 0f);
        }
    }

    public void ToggleConfirmButton(bool toggle)
    {
        confirmButton.gameObject.SetActive(toggle);
    }
}
