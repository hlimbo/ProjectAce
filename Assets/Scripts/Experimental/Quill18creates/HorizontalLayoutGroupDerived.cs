using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HorizontalLayoutGroupDerived : HorizontalLayoutGroup
{
    [SerializeField]
    private InteractableStateController[] cards;

    [SerializeField]
    public float defaultSpacing = -50f;
    [SerializeField]
    public float maxSpacing = -100f;
    [SerializeField]
    public float spacingDelta = 20f;

    protected override void Start()
    {
        base.Start();

        cards = transform.GetComponentsInChildren<InteractableStateController>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        cards = transform.GetComponentsInChildren<InteractableStateController>();
    }

    public override void SetLayoutHorizontal()
    {
        base.SetLayoutHorizontal();
        //foreach (var card in cards)
        //{
        //    Debug.Log("SetLayoutHorizontal: " + card.transform.position);
        //}
    }

    public override void SetLayoutVertical()
    {
        base.SetLayoutVertical();
        cards = transform.GetComponentsInChildren<InteractableStateController>();
        foreach (var card in cards)
        {
            // NOTE: I may need to have 2 separate functions to initialize the card's position
            // one for y coordinate and one for x coordinate
            //Debug.Log("SetLayoutVertical: " + card.transform.position);
            card.Initialize();
        }
    }
}
