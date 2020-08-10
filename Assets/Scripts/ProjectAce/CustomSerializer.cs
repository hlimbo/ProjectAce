using Mirror;

namespace ProjectAce
{
    public static class CustomSerializer
    {
        // These functions should be detected by Weaver Magic automatically
        // and add these to its Serializers to use (e.g. it'll know to when to call NetworkWriter.WriteCard() and NetworkReader.ReadCard()
        public static void WriteCard(this NetworkWriter writer, Card card)
        {
            writer.WriteInt32((int)card.suit);
            writer.WriteInt32((int)card.value);
        }

        public static Card ReadCard(this NetworkReader reader)
        {
            return new Card
            {
                suit = (SuitType)reader.ReadInt32(),
                value = (ValueType)reader.ReadInt32()
            };

        }
    }
}
