using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class Clickable : MonoBehaviour, IPointerClickHandler
{
    public bool isRaised;
    public bool isRaising;
    public float raisedHeight;
    public float tweenTransitionTime = 0.5f;

    private Vector3 raisedVector;
    private bool isCoroutineRunning;
    private RectTransform rectTransform;

    public InteractableStateController isc;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        isc = GetComponent<InteractableStateController>();

        raisedVector = new Vector3(0f, raisedHeight);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                if(!isRaised && !isRaising)
                {
                    transform.DOMove(transform.position + raisedVector, tweenTransitionTime, true);
                    isRaising = true;
                    WaitUntilCardIsRaisedRoutine(isc.originalPosition.y);
                }
                break;
            case PointerEventData.InputButton.Right:
                Debug.Log("Right mouse click");
                isc.MoveCardBackToOriginalPosition();
                break;
            default:
                Debug.Log("Unsupported InputButton type");
                break;
        }
    }

    private void Update()
    {
        // Need to have Update so the script can be toggled on/off in the editor
    }

    private void WaitUntilCardIsRaisedRoutine(float originalYPosition)
    {
        if (!isCoroutineRunning)
        {
            isCoroutineRunning = true;
            StartCoroutine(WaitUntilCardIsRaised(originalYPosition));
        }
    }

    private IEnumerator WaitUntilCardIsRaised(float originalYPosition)
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

        isCoroutineRunning = false;
        isRaising = false;
    }

    public void ResetState()
    {
        isRaising = false;
        isRaised = false;
        isCoroutineRunning = false;
        StopAllCoroutines();
    }
}
