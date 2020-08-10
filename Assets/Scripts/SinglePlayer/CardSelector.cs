using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class CardSelector : MonoBehaviour, IPointerDownHandler
{
    private PlayerController player;
    private Image cardImage;
    private Animator animController;

    [SerializeField]
    private bool isCardRaised = false;
    public bool IsCardRaised => isCardRaised;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
        player = FindObjectOfType<PlayerController>();
        animController = GetComponent<Animator>();
    }

    public void SetPlayerRef(PlayerController player)
    {
        this.player = player;
    }

    // implements OnPointerDown
    public void OnPointerDown(PointerEventData eventData)
    {
        bool isLeftMouseButtonPressed = Input.GetMouseButton(0);
        bool isRightMouseButtonPressed = Input.GetMouseButton(1);

        if(isLeftMouseButtonPressed)
        {
            if(isCardRaised)
            {
                // Do nothing as this script is using in the How to play section
            }
            else
            {
                MoveCardUp();
            }

            //if(isCardRaised && player != null)
            //{
            //    // Instead of doing an indexOf call here we could probably 
            //    // check to see if the gameObject clicked is the same instance as the gameObject that has the Image component attached
            //    int cardIndex = Array.IndexOf(player.CardImages, cardImage);
            //    if (cardImage != null && cardIndex != -1)
            //    {
            //        //bool isCardGiven = player.GiveCardToDealer(cardIndex);
            //        //if (!isCardGiven)
            //        //{
            //        //    MoveCardDown();

            //        //    // Get a random card back from the dealer if the current player cannot 
            //        //    // place any cards from their hand into the face up pile
            //        //    if (!player.CanPlaceCardFromHand())
            //        //    {
            //        //        player.GetCardFromDealer();
            //        //    }
            //        //}

            //    }
            //}
            //else
            //{
            //    MoveCardUp();
            //}
        }
        else if(isRightMouseButtonPressed)
        {
            if(isCardRaised)
            {
                MoveCardDown();
            }
        }
    }

    public void MoveCardDown()
    {
        isCardRaised = false;
        animController.SetBool("isCardSelected", isCardRaised);
    }

    public void MoveCardUp()
    {
        isCardRaised = true;
        animController.SetBool("isCardSelected", isCardRaised);
    }
}
