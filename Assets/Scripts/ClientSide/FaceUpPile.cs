using UnityEngine;
using UnityEngine.EventSystems;

public class FaceUpPile : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null)
        {
            var cardController = eventData.pointerDrag.GetComponent<CardController>();
            if(cardController != null)
            {
                cardController.DropCardOnPile();
            }
        }
    }
}
