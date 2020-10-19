using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class FaceUpPile : MonoBehaviour, IDropHandler
{
    private AudioManager audioManager;
    // Used to check if single player mode is enabled
    private ClientSideController clientSideController;

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
        clientSideController = FindObjectOfType<ClientSideController>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null)
        {
            var cardController = eventData.pointerDrag.GetComponent<CardController>();
            if(cardController != null)
            {
                if(NetworkClient.active && NetworkClient.isConnected)
                {
                    if(cardController.PlayerPanel.IsMyTurn)
                    {
                        audioManager.PlayClip("cardPlacedOnTable");
                        cardController.DropCardOnPile();
                        cardController.MoveToTargetPosition(transform, 0f);
                    }
                }
                else if(clientSideController != null)
                {
                    // Single Player Mode
                    audioManager.PlayClip("cardPlacedOnTable");
                    cardController.DropCardOnPile();
                    cardController.MoveToTargetPosition(transform, 0f);
                }
            }
        }
    }
}
