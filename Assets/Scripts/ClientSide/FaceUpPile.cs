using UnityEngine;
using UnityEngine.EventSystems;

public class FaceUpPile : MonoBehaviour, IDropHandler
{
    private AudioManager audioManager;

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null)
        {
            var cardController = eventData.pointerDrag.GetComponent<CardController>();
            if(cardController != null)
            {
                if(cardController.PlayerPanel.IsMyTurn)
                {
                    cardController.DropCardOnPile();
                    audioManager.PlayClip("cardPlacedOnTable");
                    cardController.MoveToTargetPosition(transform, 0f);
                }
            }
        }
    }
}
