using Mirror;

namespace ProjectAce.CustomNetworkMessages
{
    public class AllInGamePanelsReceivedMessage : MessageBase
    {
        // Empty but used to notify client when all game panels are received
    }

    public class DrawPileMessage : MessageBase
    {
        public int cardsLeft;
    }

}
