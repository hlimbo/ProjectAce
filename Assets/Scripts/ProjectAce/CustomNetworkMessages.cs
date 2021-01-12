using Mirror;

namespace ProjectAce.CustomNetworkMessages
{
    // See: https://github.com/vis2k/Mirror/pull/2317
    public struct DrawPileMessage : NetworkMessage
    {
        public int cardsLeft;
    }

}
