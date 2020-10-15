using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hand : HorizontalLayoutGroup
{
    [SerializeField]
    private CardController[] cards;
    private RectTransform root;

    public override void SetLayoutHorizontal()
    {
        // This needs to be done prior to calling the base method
        // because the child elements depend on the parent's calculations
        adjustScrollableContentWidth();
        base.SetLayoutHorizontal();
    }

    private void adjustScrollableContentWidth()
    {
        root = GetComponent<RectTransform>();

        float totalWidth = 0f;
        float lastWidth = 0f;
        foreach (Transform child in transform)
        {
            var rectTransform = child.GetComponent<RectTransform>();
            totalWidth += rectTransform.rect.width;
            lastWidth = rectTransform.rect.width;
        }

        float parentWidth = totalWidth;
        if (transform.childCount > 1)
        {
            parentWidth = (totalWidth + spacing * transform.childCount) + (lastWidth);
        }

        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth);
        root.ForceUpdateRectTransforms();
    }

    // This class's parent should calculate the positions of all children
    // everytime a child gets added or removed from this gameobject
    public override void SetLayoutVertical()
    {
        // An alternative here would be to wait in NetworkPlayerController until end of frame when
        // this layout group calculates the local position of the chld game-object
        base.SetLayoutVertical();
        cards = transform.GetComponentsInChildren<CardController>();
        foreach(var card in cards)
        {
            card.SetLocalTransformPropertiesFromLayoutGroup();
            //if (card.IsRaised)
            //{
            //    card.ResetCard();
            //}
        }
    }
}
