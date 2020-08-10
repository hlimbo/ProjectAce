using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private bool isRaised;
    public bool IsRaised => isRaised;

    [SerializeField]
    private bool isRaising;

    [SerializeField]
    private float raisedHeight;

    [SerializeField]
    private float tweenTransitionTime = 0.5f;

    private Vector3 raisedVector;
    private CardController controller;

    private void Awake()
    {
        controller = GetComponent<CardController>();
        raisedVector = new Vector3(0f, raisedHeight);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                RaiseCard(controller.OriginalYPosition);
                break;
            case PointerEventData.InputButton.Right:
                Debug.Log("Right mouse click");
                //controller.MoveBackToOriginalPosition();
                controller.MoveBackToOriginalLocalPosition();
                break;
            default:
                Debug.Log("Unsupported InputButton type");
                break;
        }
    }

    private void RaiseCard(float originalYPosition)
    {
        if(!isRaised && !isRaising)
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

    public void ResetState()
    {
        isRaising = false;
        isRaised = false;
        StopAllCoroutines();
    }

    private void Update()
    {
        // Need to have Update so the script can be toggled on/off in the editor
    }
}
