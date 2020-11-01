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
    private CardController controller;

    private void Awake()
    {
        controller = GetComponent<CardController>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check if a card is already being dragged
        if(eventData.dragging)
        {
            // Don't play hover animation if some other game object is already being dragged
            return;
        }

        controller.InitPlaceholder();
        controller.ScaleCard();
        RaiseCard();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.UnScaleCard();
        isRaised = isRaising = false;

        if (!controller.IsDragging)
        {
            transform.DOLocalMoveY(controller.OriginalLocalPosition.y, 0.5f, true)
                .OnStart(() => {
                    controller.ResetPosition();
                    controller.DestroyPlaceholder();
                });
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

    private void RaiseCard()
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
