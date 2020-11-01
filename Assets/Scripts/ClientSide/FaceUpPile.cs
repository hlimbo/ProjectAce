using Mirror;
using Mirror.Websocket;
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
                audioManager.PlayClip("cardPlacedOnTable");

                if (NetworkClient.active && NetworkClient.isConnected)
                {
                    if(cardController.PlayerPanel.IsMyTurn)
                    {
                        cardController.DropCardOnPile();
                        cardController.MoveToTargetPosition(transform, 0f);
                    }
                }
                else if(clientSideController != null)
                {
                    // Single Player Mode
                    cardController.DropCardOnPile();
                }
            }
        }
    }
}
