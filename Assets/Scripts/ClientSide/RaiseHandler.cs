using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        RaiseCard(controller.OriginalYPosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isRaised = false;
        isRaising = false;

        if(!controller.IsDragging)
        {
            controller.MoveBackToOriginalLocalPosition();
        }
    }

    private void Awake()
    {
        controller = GetComponent<CardController>();
        raisedVector = new Vector3(0f, raisedHeight);
    }

    // Needed to enable/disable this script
    void Update()
    {
        
    }

    private void RaiseCard(float originalYPosition)
    {
        if (!isRaised && !isRaising)
        {
            transform.DOMove(transform.position + raisedVector, tweenTransitionTime, true);
            isRaising = true;
            StartCoroutine(WaitUntilRaised(originalYPosition));
        }
    }

    private IEnumerator WaitUntilRaised(float originalYPosition)
    {
        // To handle floating point inprecisions
        float tolerance = 0.001f;
        while (transform.position.y < originalYPosition + raisedVector.y - tolerance)
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

        isRaising = false;
    }
}
