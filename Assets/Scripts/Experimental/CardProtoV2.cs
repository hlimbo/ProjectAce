using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardProtoV2 : MonoBehaviour, IPointerDownHandler
{
    public float raisedHeight;
    private Vector2 raisedVector;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;

    [SerializeField]
    private bool isRaised;
    private bool isRaising;
    private bool isCoroutineRunning;

    private CardDragProtoV2 dragProto;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        raisedVector = new Vector2(0f, raisedHeight);
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        dragProto = GetComponent<CardDragProtoV2>();
        dragProto.enabled = false;
    }

    private void Update()
    {
        // Need to have Update so the script can be toggled on/off in the editor
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        bool isLeftMouseButtonPressed = Input.GetMouseButton(0);
        bool isRightMouseButtonPressed = Input.GetMouseButton(1);

        if (isLeftMouseButtonPressed)
        {
            if(isRaised)
            {
                // Enable drag and drop script
                if(!dragProto.enabled)
                {
                    dragProto.enabled = true;
                }
            }
            else if (!isRaising)
            {
                rectTransform.DOAnchorPos(rectTransform.anchoredPosition + raisedVector, 0.5f, true);
                isRaising = true;
                WaitUntilCardIsRaisedRoutine();
            }
        }
        else if (isRightMouseButtonPressed)
        {
            isRaising = false;
            isRaised = false;
            isCoroutineRunning = false;
            dragProto.enabled = false;

            if(!transform.parent.Equals(originalParent))
            {
                transform.SetParent(originalParent);
                transform.SetSiblingIndex(originalSiblingIndex);
                // to prevent weird jumpy animation when card goes back to player's hand
                rectTransform.anchoredPosition = new Vector2(0f, 0f);
            }


            rectTransform.DOAnchorPos(originalPosition, 0.5f, true);
            StopAllCoroutines();
        }
    }

    private void WaitUntilCardIsRaisedRoutine()
    {
        if (!isCoroutineRunning)
        {
            isCoroutineRunning = true;
            StartCoroutine(WaitUntilCardIsRaised());
        }
    }

    private IEnumerator WaitUntilCardIsRaised()
    {
        // To handle floating point inprecisions
        float tolerance = 0.001f;
        while (rectTransform.anchoredPosition.y < originalPosition.y + raisedVector.y - tolerance)
        {
            if (!isRaising)
            {
                // stop checking if card isn't in the middle of being raised up
                yield break;
            }

            yield return null;
        }

        if (isRaising)
        {
            isRaised = true;
        }

        isCoroutineRunning = false;
        isRaising = false;
    }
}
