﻿using System.Collections;
using System.Collections.Generic;
using ProjectAce;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using Mirror;

public class CardController : MonoBehaviour
{
    // TODO: remove in favor of using pure drag and drop controls
    // adding the extra control to click to raise cards up isn't easy enough for player to have
    //[SerializeField]
    //private ClickHandler clickHandler;
    [SerializeField]
    private DragHandler dragHandler;
    private IPlayerController owner;

    public Card card;
    //public bool IsRaised => clickHandler != null && clickHandler.IsRaised;
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
        //clickHandler = GetComponent<ClickHandler>();
        dragHandler = GetComponent<DragHandler>();
        cardImage = GetComponent<Image>();
        rotationAngles = new Vector3(0f, 0f, 0f);
    }

    private void Update()
    { 
        // TODO: remake controls in progress
        //if (IsRaised)
        //{
        //    if(!dragHandler.enabled)
        //    {
        //        dragHandler.enabled = true;
        //    }
        //}
        //else
        //{
        //    if(dragHandler.enabled)
        //    {
        //        dragHandler.enabled = false;
        //    }
        //}
    }

    public void Initialize(IPlayerController player, Card card)
    {
        owner = player;
        this.card = card;
        cardImage.sprite = Utils.cardAssets[card.ToString()];
    }

    public void DropCardOnPile()
    {
        // clickHandler.enabled = false;

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

    public void ToggleClickHandlerBehaviour(bool isEnabled)
    {
        // clickHandler.enabled = isEnabled;
    }

    public void ToggleDragHandlerBehaviour(bool isEnabled)
    {
        dragHandler.enabled = isEnabled;
    }

    public void MoveBackToOriginalPosition()
    {
        //clickHandler.enabled = true;
        //dragHandler.enabled = false;
        //clickHandler.ResetState();
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.DOMove(originalPosition, 0.5f, true);
    }

    public bool isDoneMovingBack;
    public void MoveBackToOriginalLocalPosition()
    {
        isDoneMovingBack = false;
        dragHandler.SetBlockRaycasts(true);
        //clickHandler.ResetState();

        Debug.Log("card dragging? " + dragHandler.isDragging);

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

        //clickHandler.enabled = true;
        //dragHandler.enabled = false;
        //dragHandler.isDragging = false;
        //dragHandler.enabled = true;
        dragHandler.isDragging = false;
    }

    public void ResetCard()
    {
        isDoneMovingBack = true;
        dragHandler.SetBlockRaycasts(true);
        //clickHandler.ResetState();
        //clickHandler.enabled = true;
        //dragHandler.enabled = false;
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

        //clickHandler.enabled = false;
        dragHandler.enabled = false;
        dragHandler.isDragging = false;
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
        //clickHandler.ResetState();
        dragHandler.SetBlockRaycasts(true);
        //clickHandler.enabled = false;
        //dragHandler.enabled = false;
    }
}
