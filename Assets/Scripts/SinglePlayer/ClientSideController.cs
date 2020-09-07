﻿using ProjectAce;
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

    private bool canEnableComboButton;
    private Button comboButton;
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
        comboButton = GameObject.Find("ComboButton")?.GetComponent<Button>();
    }

    private void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurnButtonSelected);
        comboButton.onClick.AddListener(OnComboButtonSelected);
    }

    private void OnDestroy()
    {
        endTurnButton.onClick.RemoveListener(OnEndTurnButtonSelected);
        comboButton.onClick.RemoveListener(OnComboButtonSelected);
    }

    void Update()
    {
        if(canEnableComboButton)
        {
            Card[] selectedCards = hand
                .Where(c => c.IsRaised)
                .Select(c => c.card).ToArray();

            if (selectedCards.Length >= 2)
            {
                comboButton.gameObject.SetActive(true);
            }
            else
            {
                comboButton.gameObject.SetActive(false);
            }
        }
    }

    public void MoveRaisedCardsDown(Action onCardsRaisedDown)
    {
        List<CardController> raisedCards = hand.Where(card => card.IsRaised).ToList();
        StartCoroutine(CheckIfAllCardsPutBackInHand(raisedCards, onCardsRaisedDown));
    }

    private IEnumerator CheckIfAllCardsPutBackInHand(List<CardController> raisedCards, Action onCardsRaisedDown)
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

        onCardsRaisedDown();
    }

    public void MoveCardsDown()
    {
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
            c.ToggleClickHandlerBehaviour(false);
            c.ToggleDragHandlerBehaviour(false);
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

        canEnableComboButton = true;
        endTurnButton.gameObject.SetActive(true);

        foreach(var card in hand)
        {
            card.ToggleClickHandlerBehaviour(true);
        }
    }

    public void DisableControls()
    {
        canEnableComboButton = false;
        comboButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);

        foreach(var card in hand)
        {
            card.DisableInteraction();
        }
    }

    public void CardPlacementFailed(Card card)
    {
        CardController cardController = hand.Where(c => c.card.Equals(card)).FirstOrDefault();
        if(cardController != null)
        {
            audioManager.PlayClip("cardShove");
            cardController.MoveBackToOriginalLocalPosition();
        }
    }

    private void OnComboButtonSelected()
    {
        Card[] selectedCards = hand
            .Where(cardSelector => cardSelector.IsRaised)
            .Select(cardSelector => cardSelector.card).ToArray();

        Manager.EvaluateCardsToCombo(selectedCards);
    }

    private void OnEndTurnButtonSelected()
    {
        Manager.ResetTurn();
    }

    public void OnComboInvalid(Card[] cards)
    {
        hand.Where(c => cards.Contains(c.card))
            .ToList()
            .ForEach(c => c.MoveBackToOriginalLocalPosition());
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
        newCard.GetComponent<CardController>().DisableInteraction();
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
    }

    public void OnCardsAdded(Card[] cards)
    {
        foreach(var card in cards)
        {
            var cardGO = AddCard(card);
            PrepareDrawCardAnimation(cardGO);
        }

        DrawCardsAnimation();
    }

    public void OnCardAdded(Card card)
    {
        var cardGO = AddCard(card);
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
        Manager.TryAddCardToFaceUpPile(card);
    }
}