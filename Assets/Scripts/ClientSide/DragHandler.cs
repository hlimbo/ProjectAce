using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private CardController controller;
    private Transform canvas;

    public bool isDragging = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        controller = GetComponent<CardController>();
        canvas = GameObject.Find("Canvas")?.transform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetParent(canvas);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        isDragging = true;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        if(!controller.isPlacedOnTable)
        {
            controller.MoveBackToOriginalLocalPosition();
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
