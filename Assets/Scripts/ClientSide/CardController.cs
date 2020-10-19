using System.Collections;
using System.Collections.Generic;
using ProjectAce;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using Mirror;

public class CardController : MonoBehaviour
{
    [SerializeField]
    private DragHandler dragHandler;
    private IPlayerController owner;

    [SerializeField]
    private RaiseHandler raiseHandler;

    public bool IsDragging => dragHandler != null && dragHandler.isDragging;

    public Card card;
    private Image cardImage;

    // Variables used to maintain positions within a LayoutGroup
    // as LayoutGroups automatically calculate each child's position
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector3 originalLocalPosition;
    public Vector3 OriginalLocalPosition => originalLocalPosition;

    private Vector3 rotationAngles;

    public bool isPlacedOnTable = false;
    public float OriginalYPosition => originalLocalPosition.y;

    private PlayerPanel playerPanel;
    public PlayerPanel PlayerPanel
    {
        get
        {
            if(playerPanel == null)
            {
                playerPanel = FindObjectsOfType<PlayerPanel>().Where(p => p.hasAuthority).FirstOrDefault();
            }

            return playerPanel;
        }
    }

    private void Awake()
    {
        dragHandler = GetComponent<DragHandler>();
        raiseHandler = GetComponent<RaiseHandler>();
        cardImage = GetComponent<Image>();
        rotationAngles = new Vector3(0f, 0f, 0f);
    }

    public void Initialize(IPlayerController player, Card card)
    {
        owner = player;
        this.card = card;
        cardImage.sprite = Utils.cardAssets[card.ToString()];
    }

    public void DropCardOnPile()
    {
        // Online Multiplayer enabled?
        if(NetworkClient.active || NetworkClient.isLocalClient)
        {
            // Only allow client to send to server when it is the current player's turn
            if (PlayerPanel != null && PlayerPanel.IsMyTurn)
            {
                Debug.Log("Dropping card on pile....");
                owner.SendCardToDealer(card);
            }
        }
        else
        {
            // Single Player Mode
            owner.SendCardToDealer(card);
        }
    }

    public void ToggleDragHandlerBehaviour(bool isEnabled)
    {
        dragHandler.enabled = isEnabled;
    }

    public void ToggleRaiseHandlerBehaviour(bool isEnabled)
    {
        raiseHandler.enabled = isEnabled;
    }

    public bool isDoneMovingBack;
    public void MoveBackToOriginalLocalPosition()
    {
        isDoneMovingBack = false;
        dragHandler.SetBlockRaycasts(true);
        raiseHandler.enabled = false;

        if (dragHandler.isDragging)
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
                raiseHandler.enabled = true;
            });
        }
        else
        {
            tweenMover.OnComplete(() => {
                isDoneMovingBack = true;
                raiseHandler.enabled = true;
                // Used to ensure cards in hand do not get positioned in odd spots 
                // (e.g. cards clump together and not respecting the spacing variable provided in Hand.cs)
                LayoutRebuilder.MarkLayoutForRebuild(originalParent.GetComponent<RectTransform>());
            });
        }

        dragHandler.enabled = true;
        dragHandler.isDragging = false;
        isPlacedOnTable = false;
    }

    public void ResetCard()
    {
        isDoneMovingBack = true;
        dragHandler.SetBlockRaycasts(true);
    }

    public void MoveToTargetPosition(Transform parent, float targetRotation)
    {
        isPlacedOnTable = true;
        rotationAngles.z = targetRotation;
        Quaternion rotation = Quaternion.Euler(rotationAngles);

        transform.SetParent(parent);
        transform.SetAsLastSibling();
        transform.DOLocalMove(Vector2.zero, 0.5f, true);
        transform.DORotateQuaternion(rotation, 0.5f);

        dragHandler.enabled = false;
        dragHandler.isDragging = false;
    }

    public void SetLocalTransformPropertiesFromLayoutGroup()
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalLocalPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
    }

    public void DisableInteraction()
    {
        dragHandler.SetBlockRaycasts(true);
    }
}
