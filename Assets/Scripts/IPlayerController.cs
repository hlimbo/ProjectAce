namespace ProjectAce
{
    public interface IPlayerController
    {
        void SendCardToDealer(Card card);
        void SendCardsToDealer(Card[] cards); // prob not needed
    }
}