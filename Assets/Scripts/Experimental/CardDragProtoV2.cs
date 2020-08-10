using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragProtoV2 : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private Transform canvas;

    private RectTransform rectTransform;
    private BoxCollider2D boxCollider;
    private CardProtoV2 proto;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        boxCollider = GetComponent<BoxCollider2D>();
        proto = GetComponent<CardProtoV2>();
    }

    private void OnEnable()
    {
        boxCollider.enabled = true;
    }

    private void OnDisable()
    {
        boxCollider.enabled = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("OnBeginDrag");
        if(!transform.parent.Equals(canvas))
        {
            transform.SetParent(canvas);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("OnDrag");
        rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("OnEndDrag");
    }

    private void Update()
    {
        // Need to have Update so the script can be toggled on/off in the editor
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.name.Equals("FaceUpPile"))
        {
            // In the actual implementation, a network call to the server
            // should be made to determine if the card can be placed here
            // if it can play the below animations
            // if it cannot, move card back to hand

            Debug.Log("goober");
            proto.enabled = false;
            transform.SetParent(collision.transform);
            rectTransform.DOAnchorMax(new Vector2(0.5f, 0.5f), 0.5f);
            rectTransform.DOAnchorMin(new Vector2(0.5f, 0.5f), 0.5f);
            rectTransform.DOAnchorPos(Vector2.zero, 0.5f);
            rectTransform.DORotate(new Vector3(0f, 0f, 30f), 0.5f);
        }
    }
}
