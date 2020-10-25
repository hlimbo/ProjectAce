using ProjectAce;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private CardController controller;
    private Transform canvas;

    public bool isDragging = false;
    private RaiseHandler raiseHandler;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        controller = GetComponent<CardController>();
        canvas = GameObject.Find("Canvas")?.transform;
        raiseHandler = GetComponent<RaiseHandler>();
        layoutElement = GetComponent<LayoutElement>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetParent(canvas);
        // Allows rayCast for the mouse to hit other gameobjects
        canvasGroup.blocksRaycasts = false;
        // Stops card from flickering while dragging
        layoutElement.ignoreLayout = true;

        if(controller.CardPlaceholder == null)
        {
            controller.InitPlaceholder();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        isDragging = true;
        transform.position = eventData.position;
        controller.ReorderCard();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        raiseHandler.enabled = true;

        if(!controller.isPlacedOnTable)
        {
            // Side effect.. destroys cardPlaceholder
            controller.MoveBackToHand();
        }
    }

    private void Update()
    {
        // Need to have Update so the script can be toggled on/off in the editor
    }

    public void SetBlockRaycasts(bool toggle)
    {
        canvasGroup.blocksRaycasts = toggle;
    }
}
