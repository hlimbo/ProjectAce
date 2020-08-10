using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ProjectAce;
using System.Linq;

public class CardSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject cardPrefab;

    [SerializeField]
    private Transform container;

    [SerializeField]
    private Transform drawPile;

    private void Awake()
    {
        Utils.LoadCardAssets();   
    }

    private Sprite RandomCard()
    {
        int spriteNameIndex = Random.Range(0, Utils.cardAssets.Count);
        string spriteName = Utils.cardAssets.Keys.ToArray()[spriteNameIndex];
        return Utils.cardAssets[spriteName];
    }

    public void OnSpawnButtonClicked()
    {
        Sprite randomCardSprite = RandomCard();

        var card = Instantiate(cardPrefab);
        var controller = card.GetComponent<CardController>();

        card.GetComponent<Image>().sprite = randomCardSprite;
        var myColor = card.GetComponent<Image>().color;
        card.GetComponent<Image>().color = new Color(myColor.r, myColor.g, myColor.b, 0f);
        card.transform.SetParent(container);

        // a good possible optimization here is to "pool" these placeholder objects for reuse
        GameObject placeholder = new GameObject("placeholder");
        var r = placeholder.AddComponent<RectTransform>();
        var l = placeholder.AddComponent<LayoutElement>();
        var i = placeholder.AddComponent<Image>();
        l.preferredWidth = card.GetComponent<LayoutElement>().preferredWidth;
        l.preferredHeight = card.GetComponent<LayoutElement>().preferredHeight;
        l.ignoreLayout = true;
        r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, l.preferredWidth);
        r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, l.preferredHeight);
        i.raycastTarget = false;
        i.sprite = randomCardSprite;

        // Option A: recalculate positions of children by invoking 4 functions below
        //var hand = container.GetComponent<Hand>();
        //hand.CalculateLayoutInputHorizontal();
        //hand.CalculateLayoutInputVertical();
        //hand.SetLayoutHorizontal();
        //hand.SetLayoutVertical();

        // ~ Option B: is to write a coroutine that waits until end of frame and then retrieves the calculated position of the newly placed child object

        /*
         * SetLayoutVertical gets called 3 times
         * 1. called when placeholder is placed in the container
         * 2. called when ForceRebuildLayoutImmediate() is called
         * 3. placeholder gets destroyed from the system.
         */

        // Note: ForceRebuildLayoutImmediate is supposedly costs performance as per unity docs
        // https://docs.unity3d.com/2018.1/Documentation/ScriptReference/UI.LayoutRebuilder.ForceRebuildLayoutImmediate.html
        Debug.Log("Placeholder position: " + placeholder.transform.localPosition);
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
        Debug.Log("Placeholder position after: " + placeholder.transform.localPosition);

        placeholder.transform.SetParent(drawPile);
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0f, 0f);
        placeholder.transform.SetParent(container);

        Vector3 localPos = new Vector3(card.transform.localPosition.x, card.transform.localPosition.y, card.transform.localPosition.z);
        placeholder.transform.DOLocalMove(localPos, 1.25f, true).OnComplete(() =>
        {
            card.GetComponent<Image>().color = new Color(myColor.r, myColor.g, myColor.b, 1f);
            Destroy(placeholder);
        });
    }

    private IEnumerator hack(GameObject card)
    {
        card.transform.SetParent(drawPile);
        card.transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(2.0f);
        card.transform.SetParent(container);


    }
}
