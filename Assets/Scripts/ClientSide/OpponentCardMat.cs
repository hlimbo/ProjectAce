using ProjectAce;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentCardMat : MonoBehaviour
{
    private GameObject facedownCardPrefab;

    private Transform hand;
    private List<GameObject> cards = new List<GameObject>();
    private Stack<GameObject> cardsMarkedForDestruction = new Stack<GameObject>();

    private bool isCardDestructionInProgress = false;

    private void Awake()
    {
        hand = transform.Find("CardContainer/Viewport/OpponentCards");
    }

    public void SpawnCard()
    {
        var card = Instantiate(facedownCardPrefab, hand);
        cards.Add(card);
        card.name = string.Format("Opponent Card {0}", hand.childCount);

        // Assign anchors so cards render properly on screen
        var rectTransform = card.GetComponent<RectTransform>();
        // left hand side mat
        if(gameObject.name.Equals("OpponentCardMat2"))
        {
            AnchorPresetsUtils.AssignAnchor(AnchorPresets.MIDDLE_LEFT, ref rectTransform);
        }
        // right hand side mat
        else if(gameObject.name.Equals("OpponentCardMat3"))
        {
            AnchorPresetsUtils.AssignAnchor(AnchorPresets.MIDDLE_RIGHT, ref rectTransform);
        }
    }

    public void DestroyCard(int index)
    {
        if(index >= 0 && index < cards.Count)
        {
            // Unity completes game object destruction after current update loop finishes
            // This code marks this game object for destruction
            Destroy(cards[index]);
            cardsMarkedForDestruction.Push(cards[index]);
            StartCardDestruction();
        }
    }

    private void StartCardDestruction()
    {
        if(!isCardDestructionInProgress)
        {
            isCardDestructionInProgress = true;
            CardDestructionRoutine();
        }
    }

    private void CardDestructionRoutine()
    {
        while(cardsMarkedForDestruction.Count > 0)
        {
            var card = cardsMarkedForDestruction.Pop();
            cards.Remove(card);
        }

        isCardDestructionInProgress = false;
    }

    public void SetFaceDownCardPrefab(GameObject prefab)
    {
        facedownCardPrefab = prefab;
    }
}
