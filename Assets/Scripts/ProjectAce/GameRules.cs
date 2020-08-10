using System.Linq;

namespace ProjectAce
{
    public static class GameRules2
    {
        public static bool ValidateCard(Card? topCard, Card currentCard)
        {
            // if no cards are in the face up pile
            if (topCard == null)
            {
                return true;
            }

            Card top = (Card)topCard;
            return IsSameSuit(top, currentCard) || IsSameNumber(top, currentCard);
        }

        private static bool IsSameSuit(Card topCard, Card currentCard)
        {
            return topCard.suit == currentCard.suit;
        }

        private static bool IsSameNumber(Card topCard, Card currentCard)
        {
            return topCard.value == currentCard.value;
        }

        public static bool DoCardsAddUpToTopCardValue(Card topCard, Card[] cards)
        {
            int topValue = topCard.Value;
            int sum = cards.Sum(card => card.Value);
            bool areAllSameSuit = cards.Length > 1 && cards.All(card => IsSameSuit(cards[0], card));
            return areAllSameSuit && sum == topValue;
        }
    }
}
