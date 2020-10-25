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

    private GameObject cardPlaceholder;
    [SerializeField]
    private GameObject cardPlaceholderTemplate;

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

        cardPlaceholder = Instantiate(cardPlaceholderTemplate);
        cardPlaceholder.transform.SetParent(controller.OriginalParent);
        cardPlaceholder.transform.SetSiblingIndex(controller.OriginalSiblingIndex);
        cardPlaceholder.transform.localScale = Vector3.one;
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

        raiseHandler.enabled = true;

        if(!controller.isPlacedOnTable)
        {
            // Side effect.. destroys cardPlaceholder
            controller.MoveBackToHand(cardPlaceholder.transform);
        }
        else
        {
            Destroy(cardPlaceholder);
            cardPlaceholder = null;
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
