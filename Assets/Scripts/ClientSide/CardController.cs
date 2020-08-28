using System.Collections;
using System.Collections.Generic;
using ProjectAce;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class CardController : MonoBehaviour
{
    [SerializeField]
    private ClickHandler clickHandler;
    [SerializeField]
    private DragHandler dragHandler;
    private NetworkPlayerController owner;

    public Card card;
    public bool IsRaised => clickHandler != null && clickHandler.IsRaised;
    private Image cardImage;

    // Variables used to maintain positions within a LayoutGroup
    // as LayoutGroups automatically calculate each child's position
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector3 originalPosition;
    private Vector3 originalLocalPosition;

    private Vector3 rotationAngles;

    public bool isPlacedOnTable = false;
    public float OriginalYPosition => originalPosition.y;

    private void Awake()
    {
        clickHandler = GetComponent<ClickHandler>();
        dragHandler = GetComponent<DragHandler>();
        cardImage = GetComponent<Image>();
        rotationAngles = new Vector3(0f, 0f, 0f);
    }

    private void Update()
    { 
        if (IsRaised)
        {
            if(!dragHandler.enabled)
            {
                dragHandler.enabled = true;
            }
        }
        else
        {
            if(dragHandler.enabled)
            {
                dragHandler.enabled = false;
            }
        }
    }

    public void Initialize(NetworkPlayerController player, Card card)
    {
        owner = player;
        this.card = card;
        cardImage.sprite = Utils.cardAssets[card.ToString()];
    }

    public void SendCardToServer()
    {
        clickHandler.enabled = false;

        // check if it is still the player's turn here
        var panel = FindObjectsOfType<PlayerPanel>().Where(p => p.ConnectionId == owner.ConnectionId).FirstOrDefault();
        if(panel == null)
        {
            return;
        }

        if(panel.IsMyTurn)
        {
            owner.CmdSendCardToDealer(card);
        }
    }

    public void ToggleClickHandlerBehaviour(bool isEnabled)
    {
        clickHandler.enabled = isEnabled;
    }

    public void ToggleDragHandlerBehaviour(bool isEnabled)
    {
        dragHandler.enabled = isEnabled;
    }

    public void MoveBackToOriginalPosition()
    {
        clickHandler.enabled = true;
        dragHandler.enabled = false;
        clickHandler.ResetState();
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.DOMove(originalPosition, 0.5f, true);
    }

    public bool isDoneMovingBack;
    public void MoveBackToOriginalLocalPosition()
    {
        isDoneMovingBack = false;
        dragHandler.SetBlockRaycasts(true);
        clickHandler.ResetState();
        
        if(dragHandler.isDragging)
        {
            transform.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        var tweenMover = transform.DOLocalMove(originalLocalPosition, 0.5f, true);

        if(dragHandler.isDragging)
        {
            tweenMover.OnComplete(() => 
            { 
                transform.GetComponent<LayoutElement>().ignoreLayout = false;
                isDoneMovingBack = true;
            });
        }
        else
        {
            tweenMover.OnComplete(() => isDoneMovingBack = true);
        }

        clickHandler.enabled = true;
        dragHandler.enabled = false;
        dragHandler.isDragging = false;
    }

    public void ResetCard()
    {
        isDoneMovingBack = true;
        dragHandler.SetBlockRaycasts(true);
        clickHandler.ResetState();
        clickHandler.enabled = true;
        dragHandler.enabled = false;
    }

    public void MoveToTargetPosition(Transform parent, float targetRotation)
    {
        rotationAngles.z = targetRotation;
        Quaternion rotation = Quaternion.Euler(rotationAngles);

        transform.SetParent(parent);
        transform.SetAsLastSibling();
        transform.DOLocalMove(Vector2.zero, 0.5f, true);
        transform.DORotateQuaternion(rotation, 0.5f);

        clickHandler.enabled = false;
        dragHandler.enabled = false;
    }

    public void SetTransformPropertiesFromLayoutGroup()
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    public void SetLocalTransformPropertiesFromLayoutGroup()
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalLocalPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
    }

    public void DisableInteraction()
    {
        clickHandler.ResetState();
        dragHandler.SetBlockRaycasts(true);
        clickHandler.enabled = false;
        dragHandler.enabled = false;
    }
}
