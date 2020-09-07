using ProjectAce;
using System;
using UnityEngine;
using UnityEngine.UI;

// Available Server-Side only for Online Play.
public class Dealer : MonoBehaviour
{
    public static event Action<Dealer> OnDrawPileCountChanged = delegate { };

    public const int PLAYER_COUNT = 2;
    public const int STARTING_CARDS_PER_PLAYER = 8;

    // Use the Unity Editor to drag and drop player refs into this array
    [SerializeField]
    private PlayerController[] players;

    private Deck drawPile;
    private Deck faceUpPile;

    public Card? TopCard => faceUpPile.TopCard;
    public int DrawPileCount => drawPile.Count;

    [SerializeField]
    private Image faceUpCard;

    private void Awake()
    {
        drawPile = new Deck();
        faceUpPile = new Deck();
    }

    public void EmptyFaceUpPile()
    {
        faceUpPile.Clear();
    }

    public void PrepareDeck()
    {
        drawPile.Clear();
        drawPile.FillCards();
        drawPile.Shuffle();
    }

    public Card[] GetCards(int cardCount)
    {
        Card[] cards = drawPile.Count >= cardCount ?
            new Card[cardCount] : new Card[drawPile.Count];
        for (int i = 0; i < cards.Length; ++i)
        {
            Card? temp = drawPile.Remove();
            if (temp != null)
            {
                cards[i] = (Card)temp;
            }
        }

        return cards;
    }

    public void DealCards()
    {
        if(players.Length <= 0)
        {
            return;
        }

        // assuming even number of players only 2 or 4 players
        int cardCount = 8; // drawPile.Count / players.Length;
        foreach(var player in players)
        {
            Card[] cards = GetCards(cardCount);
            player.ReceiveCards(cards);
        }

        OnDrawPileCountChanged(this);
    }

    // Network Version
    public Card? GiveCard2()
    {
        return drawPile.Remove();
    }

    public Card? GiveCard()
    {
        var card = drawPile.Remove();
        if(card != null)
        {
            OnDrawPileCountChanged(this);
        }
        return card;
    }

    public bool AddCardToFaceUpPile(Card card)
    {
        bool canCardBeAdded = GameRules.ValidateCard(faceUpPile.TopCard, card);
        // Need to determine when face up pile needs to transfer all its cards to the draw pile
        // ^ when draw pile is empty move all cards from face up pile to draw pile and shuffle
        if (canCardBeAdded)
        {
            bool isAdded = faceUpPile.Add(card);
            if (isAdded)
            {
                faceUpCard.sprite = Resources.Load<Sprite>("CardAssets/" + card);
            }
            Debug.Log("Card to add: " + card);
            Debug.Log("Card added to face up pile? " + isAdded);

            return canCardBeAdded && isAdded;
        }

        return false;
    }

    // Network Version
    public bool AddCardToFaceUpPile2(Card card)
    {
        return faceUpPile.Add(card);
    }

    public bool TryAddCardsToFaceUpPile(Card[] cards)
    {
        // Can only add cards to face up pile when face up pile has card count > 0
        if(faceUpPile.TopCard == null)
        {
            return false;
        }

        bool canCardsBeAdded = GameRules.DoCardsAddUpToTopCardValue((Card)faceUpPile.TopCard, cards);
        if(canCardsBeAdded)
        {
            bool areCardsAdded = faceUpPile.Add(cards);
            if(areCardsAdded)
            {
                // Single-Player Reference without Network code involved
                faceUpCard.sprite = Resources.Load<Sprite>("CardAssets/" + cards[cards.Length - 1]);
            }

            return canCardsBeAdded && areCardsAdded;
        }
        
        return false;
    }

    public void TransferCardsFromFaceUpPileToDrawPile()
    {
        faceUpPile.TransferCards(ref drawPile);
        drawPile.Shuffle();
        OnDrawPileCountChanged(this);
    }

    public void AddCardsBackToDrawPile(Card[] cards)
    {
        foreach(var card in cards)
        {
            drawPile.Add(card);
        }

        if(cards.Length > 0)
        {
            drawPile.Shuffle();
        }

        OnDrawPileCountChanged(this);
    }
}
