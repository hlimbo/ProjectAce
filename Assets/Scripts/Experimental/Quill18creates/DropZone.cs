using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // This gets triggered before the OnEndDrag
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log(eventData.pointerDrag.name + " was dropped on " + gameObject.name);

        //eventData.pointerDrag.transform.SetParent(transform);

        Draggable d = eventData.pointerDrag.GetComponent<Draggable>();
        if(d != null)
        {
            d.parentToReturnTo = transform;
            //d.enabled = false;
        }

        InteractableStateController isc = eventData.pointerDrag.GetComponent<InteractableStateController>();
        if(isc != null)
        {
            isc.isPlacedOnTable = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
       // Debug.Log("OnPointerEnter");
       // Can have the dragged item e.g. card glow if its a valid card to place 
       // Insert network call here to check to see if card is valid move
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      //  Debug.Log("OnPointerExit");
    }
}
