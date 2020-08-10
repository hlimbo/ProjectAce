using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractableStateController : MonoBehaviour
{
    [SerializeField]
    private Clickable clickable;
    [SerializeField]
    private Draggable draggable;

    public Transform originalParent;
    public Vector3 originalPosition;
    public int originalSiblingIndex = -1;

    public bool IsRaised => clickable != null && clickable.isRaised;
    public bool isPlacedOnTable = false;

    private void Awake()
    {
        clickable = GetComponent<Clickable>();
        draggable = GetComponent<Draggable>();
    }

    private void Update()
    {
        if(IsRaised)
        {
            if(!draggable.enabled)
            {
                draggable.enabled = true;
            }
        }
        else
        {
            if(draggable.enabled)
            {
                draggable.enabled = false;
            }
        }
    }

    // Used to set original position of game-object after horizontal layout group calculates its position
    public void Initialize()
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    public void MoveCardBackToOriginalPosition()
    {
        clickable.ResetState();
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.DOMove(originalPosition, 0.5f, true);
    }

    public void MoveCardToTargetPosition(Transform parent, float targetRotation)
    {
        transform.SetParent(parent);
        transform.DOLocalMove(Vector2.zero, 0.5f, true);
        //Quaternion rotation = Quaternion.Euler(new Vector3(0f, 0f, targetRotation));
        //transform.DORotateQuaternion(rotation, 0.5f);
        Debug.Log(targetRotation);
        transform.DORotate(new Vector3(0f, 0f, targetRotation), 0.5f);
        transform.SetAsLastSibling();
        clickable.enabled = false;
    }

}
