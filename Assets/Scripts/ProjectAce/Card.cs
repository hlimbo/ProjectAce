using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectAce
{
    public enum SuitType { HEART, DIAMOND, SPADE, CLUB };
    public enum ValueType 
    { 
        ACE, 
        TWO, 
        THREE, 
        FOUR, 
        FIVE, 
        SIX, 
        SEVEN, 
        EIGHT, 
        NINE, 
        TEN, 
        JACK, 
        QUEEN, 
        KING 
    };

    [System.Serializable]
    public struct Card
    {
        public SuitType suit;
        public ValueType value;

        public string SuitName => suit.ToString();
        public string ValueName => value.ToString();

        public int Value => (int)value + 1;

        public override string ToString()
        {
            string valName;
            if(value > (int)ValueType.ACE && (int)value < (int)ValueType.JACK)
            {
                valName = Value.ToString();
            }
            else
            {
                valName = ValueName.ToLower();
            }

            return string.Format("{0}_of_{1}s", valName, SuitName.ToLower());
        }

        // value type equality check
        public override bool Equals(object obj)
        {
            if(!(obj is Card))
            {
                return false;
            }

            Card otherCard = (Card)obj;
            return suit.Equals(otherCard.suit) && value.Equals(otherCard.value);
        }

        public override int GetHashCode()
        {
            // See: https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + suit.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }
        }
    }

    public class Deck
    {
        public static int SUIT_COUNT = Enum.GetNames(typeof(SuitType)).Length;
        public static int VALUE_COUNT = Enum.GetNames(typeof(ValueType)).Length;
        public static readonly SuitType[] SUITS = Enum.GetValues(typeof(SuitType)).OfType<SuitType>().ToArray();
        public static readonly ValueType[] VALUES = Enum.GetValues(typeof(ValueType)).OfType<ValueType>().ToArray();

        public const int MAX_COUNT = 52;
        private List<Card> cards;
        public int Count => cards.Count;
        public bool IsEmpty => cards.Count == 0;

        public Card? TopCard => IsEmpty ? null : (Card?)cards[Count - 1];

        public Deck()
        {
            cards = new List<Card>();
        }

        public void FillCards()
        {
            for (int i = 0; i < SUIT_COUNT; ++i)
            {
                for (int j = 0; j < VALUE_COUNT; ++j)
                {
                    cards.Add(new Card()
                    {
                        suit = SUITS[i],
                        value = VALUES[j]
                    }); ;
                }
            }
        }

        public void Clear()
        {
            cards.Clear();
        }

        public void PrintDeck()
        {
            for(int i = 0;i < cards.Count; ++i)
            {
                Debug.Log(cards[i]);
            }
        }

        // Using Fisher-Yates shuffle modern method: https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        public void Shuffle()
        {
            int k = cards.Count;
            while(k > 1)
            {
                --k;
                int i = UnityEngine.Random.Range(0, k);
                Card temp = cards[i];
                cards[i] = cards[k];
                cards[k] = temp;
            }
        }

        public Card? Remove()
        {
            if(cards.Count == 0)
            {
                return null;
            }

            Card removedCard = cards[cards.Count - 1];
            cards.RemoveAt(cards.Count - 1);
            return removedCard;
        }

        public bool Add(Card card)
        {
            if(cards.Count + 1 > MAX_COUNT)
            {
                return false;
            }

            cards.Add(card);
            return true;
        }

        public bool Add(Card[] cardsToAdd)
        {
            if(cards.Count + cardsToAdd.Length > MAX_COUNT)
            {
                return false;
            }

            cards.AddRange(cardsToAdd);
            return true;
        }

        public bool TransferCards(ref Deck dest)
        {
            if (Count - 1 <= 0) return false;
            List<Card> subCards = cards.GetRange(0, Count - 1);
            dest.cards.AddRange(subCards);
            cards.RemoveRange(0, Count - 1);
            return true;
        }
    }

}
