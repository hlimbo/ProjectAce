using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform parentToReturnTo;
    public int originalSiblingIndex;
    public InteractableStateController isc;

    private void Awake()
    {
        originalSiblingIndex = transform.GetSiblingIndex();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //parentToReturnTo = transform.parent;
        transform.SetParent(transform.parent.parent); // canvas

        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("OnEndDrag");
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        var isc = eventData.pointerDrag.GetComponent<InteractableStateController>();
        if(isc != null)
        {
            if(!isc.isPlacedOnTable)
            {
                isc.MoveCardBackToOriginalPosition();
            }
        }

        // Draggable can do a raycast to check to see what everything is under
        // good use case would be if you wanted to cast a spell on a minion
        // it can target the card in question
        // EventSystem.current.RaycastAll(eventData,);
        this.enabled = false;
    }

    private void Update()
    {
        // Need to have Update so the script can be toggled on/off in the editor
    }
}
