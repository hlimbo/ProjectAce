using ProjectAce;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private CardController controller;
    private Transform canvas;

    public bool isDragging = false;

    private GameObject cardPlaceholder;

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
        cardPlaceholder = new GameObject("cardPlaceholder");
        LayoutElement layoutElement = cardPlaceholder.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = GetComponent<LayoutElement>().preferredWidth;
        layoutElement.preferredHeight = GetComponent<LayoutElement>().preferredHeight;
        cardPlaceholder.transform.SetParent(controller.OriginalParent);
        cardPlaceholder.transform.SetSiblingIndex(controller.OriginalSiblingIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        isDragging = true;
        transform.position = eventData.position;

        // Rearrange card logic
        if(transform.position.y - controller.OriginalParent.transform.position.y <= 2f)
        {
            int newSiblingIndex = controller.OriginalParent.childCount;
            for (int i = 0; i < controller.OriginalParent.childCount; ++i)
            {
                if (transform.position.x < controller.OriginalParent.GetChild(i).position.x)
                {
                    newSiblingIndex = i;
                    if (cardPlaceholder.transform.GetSiblingIndex() < newSiblingIndex)
                    {
                        newSiblingIndex--;
                    }

                    break;
                }
            }

            cardPlaceholder.transform.SetSiblingIndex(newSiblingIndex);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        if(!controller.isPlacedOnTable)
        {
            Debug.Log("Moving Card Back to Original Location");
            //controller.MoveBackToOriginalLocalPosition();

            // Side effect.. destroys cardPlaceholder
            controller.MoveBackToHand(cardPlaceholder.transform);
        }
        else
        {
            Destroy(cardPlaceholder);
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

    public void DestroyPlaceholder()
    {
        if(cardPlaceholder != null)
        {
            Destroy(cardPlaceholder);
        }
    }
}
