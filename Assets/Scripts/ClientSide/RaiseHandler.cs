using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Mirror.Cloud.Examples.Pong;

public class RaiseHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isRaised;
    private bool isRaising;

    [SerializeField]
    private float raisedHeight = 50f;
    [SerializeField]
    private float tweenTransitionTime = 0.5f;

    private Vector3 raisedVector;
    private CardController controller;
    private RectTransform rectTransform;
    private LayoutElement layoutElement;

    private Transform playerCardMat;

    private void Awake()
    {
        controller = GetComponent<CardController>();
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
        raisedVector = new Vector3(0f, raisedHeight);

        playerCardMat = GameObject.Find("PlayerCardMat")?.transform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check if a card is already being dragged
        if(eventData.dragging)
        {
            // Don't play hover animation if some other game object is already being dragged
            return;
        }

        transform.SetParent(playerCardMat);
        rectTransform.localScale = new Vector3(1.5f, 1.5f, 1f);
        RaiseCard(controller.OriginalLocalPosition.y);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.localScale = new Vector3(1f, 1f, 1f);
        transform.SetParent(controller.OriginalParent);
        transform.SetSiblingIndex(controller.OriginalSiblingIndex);
        isRaised = isRaising = false;

        if (!controller.IsDragging)
        {
            transform.DOLocalMoveY(controller.OriginalLocalPosition.y, 0.5f, true);
        }
        else
        {
            enabled = false;
        }
    }

    private void OnDisable()
    {
        isRaised = isRaising = false;
    }

    // Needed to enable/disable this script
    void Update()
    {
        
    }

    private void RaiseCard(float startingYPosition)
    {
        isRaising = true;
        transform.DOLocalMoveY(transform.localPosition.y + raisedHeight, tweenTransitionTime, true)
            .OnComplete(() =>
            {
                isRaising = false;
                isRaised = true;
            });
    }
}
