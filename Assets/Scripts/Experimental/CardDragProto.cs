using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

// Prototype complete --> integrate this code into the actual codebase :)
// This script will potentially conflict with the CardHandGroup script
// as cardhandgroup calculates new positions for every card in the player's hand when a new card is added
public class CardDragProto : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    private Vector2 raisedVector = new Vector2(0f, 50f);

    private BoxCollider2D boxCollider;

    [SerializeField]
    private bool isRaised;
    private bool isRaising;
    private bool isCoroutineRunning;
    private bool isPlacedOnFaceUpPile;

    [SerializeField]
    private Transform canvas;

    [SerializeField]
    private RectTransform viewport;

    [SerializeField]
    private Transform faceUpPile;

    private RectTransform rectTransform;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalPosition;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;

        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.enabled = false;
    }

    private void WaitUntilCardIsRaisedRoutine()
    {
        if(!isCoroutineRunning)
        {
            isCoroutineRunning = true;
            StartCoroutine(WaitUntilCardIsRaised());
        }
    }

    private IEnumerator WaitUntilCardIsRaised()
    {
        // To handle floating point inprecisions
        float tolerance = 0.001f;
        while(rectTransform.anchoredPosition.y < originalPosition.y + raisedVector.y - tolerance)
        {
            if(!isRaising)
            {
                // stop checking if card isn't in the middle of being raised up
                yield break;
            }

            yield return null;
        }

        if(isRaising)
        {
            isRaised = true;
            boxCollider.enabled = true;
            isRaising = false;
        }

        isCoroutineRunning = false;

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("OnBeginDrag: " + eventData.dragging);
        if (!isRaised) return;
        transform.SetParent(canvas);
    }

    /*
     * Snapping card will need to be a thing please!!! => i don't want the player to accidently play a card due to it not snapping exactly where the mouse is at
     * 
     * Keep the click to raise card (Convert from Unity Animation to use DoTween to animate it instead)
     * As long as the card is raised, the user should be able to freely the card around
     * Need to detect when card overlaps face-up pile => if it does, make a network call to validate if its a valid move
     * If server says its a valid move => play card rotation animation
     * If server says its not a valid move => move card back to the player's hand in the original position in hand
     */

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("OnDrag: " + eventData.dragging);

        if (!isRaised) return;

        if(isPlacedOnFaceUpPile)
        {
            return;
        }

        if(!transform.parent.Equals(canvas))
        {
            transform.SetParent(canvas);
        }
        
        rectTransform.anchoredPosition += eventData.delta / canvas.GetComponent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isRaised) return;

        isRaised = false;

        if (!isPlacedOnFaceUpPile)
        {
            // doing an animation here may be tricky as I will need to calculate the new relative position once the parent is set
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.DOAnchorPos(originalPosition, 0.5f, true);
            //rectTransform.anchoredPosition = new Vector2(originalPosition.x, originalPosition.y);

            boxCollider.enabled = false;
        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        bool isLeftMouseButtonPressed = Input.GetMouseButton(0);
        bool isRightMouseButtonPressed = Input.GetMouseButton(1);

        if(isLeftMouseButtonPressed)
        {
            if(isRaised)
            {
            }
            else
            {
                if(!isRaising)
                {
                    rectTransform.DOAnchorPos(rectTransform.anchoredPosition + raisedVector, 0.5f, true);
                    isRaising = true;
                    WaitUntilCardIsRaisedRoutine();
                }
            }
        }
        else if(isRightMouseButtonPressed)
        {
            isRaising = false;
            isRaised = false;
            isCoroutineRunning = false;
            boxCollider.enabled = false;
            rectTransform.DOAnchorPos(originalPosition, 0.5f, true);
            StopAllCoroutines();
        }

        // use rect-transform utility to check if card is overlapping face-up pile
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.name.Equals("FaceUpPile"))
        {
            Debug.Log("goober");
            isPlacedOnFaceUpPile = true;
            transform.SetParent(faceUpPile);
            rectTransform.DOAnchorMax(new Vector2(0.5f, 0.5f), 0.5f);
            rectTransform.DOAnchorMin(new Vector2(0.5f, 0.5f), 0.5f);
            rectTransform.DOAnchorPos(Vector2.zero, 0.5f);
            rectTransform.DORotate(new Vector3(0f, 0f, 30f), 0.5f);
        }
    }
}
