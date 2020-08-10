using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FaceUpPile : MonoBehaviour, IDropHandler
{
    // Perhaps implement IPointerEnter interface to check if card can be dropped on this gameobject
    // ~adds a glow effect 

    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag != null)
        {
            var cardController = eventData.pointerDrag.GetComponent<CardController>();
            if(cardController != null)
            {
                cardController.SendCardToServer();
            }
        }
    }
}
