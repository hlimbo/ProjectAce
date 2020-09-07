using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectAce;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public static event Action<List<Card>> OnCardGivenToDealer = delegate { };

    private List<Card> hand;
    [SerializeField]
    private List<Image> cardImages;
    private List<bool> isCoroutineRunning;

    public int CardCount => hand.Count;

    [SerializeField]
    private Dealer dealer;

    [SerializeField]
    private GameObject cardImagePrefab;
    public Image[] CardImages => cardImages.ToArray();

    [SerializeField]
    private int removeCount = 0;
    [SerializeField]
    private Transform horizLayout;

    void Awake()
    {
        hand = new List<Card>();
        isCoroutineRunning = new List<bool>();
        foreach (var _ in cardImages)
        {
            isCoroutineRunning.Add(false);
        }
    }

    public void ReceiveCards(Card[] cards)
    {
        hand.AddRange(cards);
        for(int i = 0;i < cardImages.Count; ++i)
        {
            cardImages[i].sprite = Resources.Load<Sprite>("CardAssets/" + hand[i]);
        }
    }

    public bool GiveCardToDealer(int index)
    {
        Card card = hand[index];
        Image cardImage = cardImages[index];
        bool canCardBeAdded = dealer.AddCardToFaceUpPile(card);
        if(canCardBeAdded)
        {
            Debug.Log("Giving card: " + card);
            hand.RemoveAt(index);
            cardImages.RemoveAt(index);
            isCoroutineRunning.RemoveAt(index);
            Destroy(cardImage.gameObject);
            removeCount++;
            OnCardGivenToDealer(hand);
        }
        else
        {
            Debug.Log("Card not given: " + card);
            if (!isCoroutineRunning[index])
            {
                isCoroutineRunning[index] = true;
                StartCoroutine(FadeColorInOut(index, Color.red, 2f));
            }
        }

        return canCardBeAdded;
    }

    private IEnumerator FadeColorInOut(int index, Color targetColor, float fadeDuration)
    {
        Image cardImage = cardImages[index];
        Color oldColor = cardImage.color;
        cardImage.CrossFadeColor(targetColor, fadeDuration / 2f, false, true);
        yield return new WaitForSeconds(1f);
        cardImage.CrossFadeColor(oldColor, fadeDuration / 2f, false, true);
        isCoroutineRunning[index] = false;
    }

    public bool CanPlaceCardFromHand()
    {
        foreach(var card in hand)
        {
            if(GameRules.ValidateCard(dealer.TopCard, card))
            {
                return true;
            }
        }

        return false;
    }

    public void GetCardFromDealer()
    {
        Card? card = dealer.GiveCard();
        if (card != null)
        {
            Card c = (Card)card;
            Debug.Log("Attempt to add a card called: " + c);
            GameObject cardImageGO = Instantiate(cardImagePrefab, horizLayout);
            //cardImageGO.GetComponent<CardSelector>().SetPlayerRef(this);
            Image cardImage = cardImageGO.GetComponent<Image>();
            cardImage.sprite = Resources.Load<Sprite>("CardAssets/" + c);
            hand.Add(c);
            cardImages.Add(cardImage);
            isCoroutineRunning.Add(false);
        }
        else
        {
            Debug.Log("No more cards for the dealer to hand out.. shuffling all cards but one from face up pile into the draw pile");
            dealer.TransferCardsFromFaceUpPileToDrawPile();
        }
    }

    // Used in ComboButton onClick event handler in Unity Scene named SampleScene
    public void ComboButtonClicked()
    {
        // Find out which cards that were raised
        var cardImageIndices = cardImages
            .Where(image => image.GetComponent<CardSelector>() != null && image.GetComponent<CardSelector>().IsCardRaised)
            .Select(image => Array.IndexOf(CardImages, image))
            .Where(index => index != -1)
            .ToArray();

        var selectedCardImages = cardImages
            .Where(image => image.GetComponent<CardSelector>() != null && image.GetComponent<CardSelector>().IsCardRaised)
            .ToArray();

        var cardSelectors = cardImageIndices
             .Select(index => cardImages[index].GetComponent<CardSelector>())
             .Where(cardSelector => cardSelector != null)
             .ToArray();

        var cardsSelected = cardImageIndices
            .Select(index => hand[index])
            .ToArray();

        if (!dealer.TryAddCardsToFaceUpPile(cardsSelected))
        {
            // Move all cards raised down
            foreach (var cardSelector in cardSelectors)
            {
                cardSelector.MoveCardDown();
            }
        }
        else
        {
            // Remove selected cards from hand
            foreach(var cardSelected in cardsSelected)
            {
                hand.Remove(cardSelected);
            }

            for(int i = 0;i < selectedCardImages.Length; ++i)
            {
                var cardImage = selectedCardImages[i];
                cardImages.Remove(cardImage);
                Destroy(cardImage.gameObject);
                removeCount++;
            }


            int offset = 0;
            int prevIndex = -1;
            foreach(var index in cardImageIndices)
            {
                if(prevIndex == -1)
                {
                    isCoroutineRunning.RemoveAt(index);
                }
                else
                {
                    if(prevIndex > index)
                    {
                        isCoroutineRunning.RemoveAt(index);
                    }
                    else
                    {
                        if(index - offset < 0)
                        {
                            isCoroutineRunning.RemoveAt(0);
                        }
                        else
                        {
                            isCoroutineRunning.RemoveAt(index - offset);
                        }
                    }
                }

                prevIndex = index;
                ++offset;
            }
        }
    }
}
