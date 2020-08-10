using UnityEngine;
using UnityEngine.EventSystems;
using ProjectAce;
using System.Collections;
using DG.Tweening;

public class CardSelectorProto : MonoBehaviour, IPointerDownHandler
{
    private Animator animController;
    private RectTransform rectTransform;

    [SerializeField]
    private bool isRaised;

    // Will be set by the ProjectAceNetworkManager -> NetworkPlayerController -> cardSelector
    [SerializeField]
    private int animIndex;

    [SerializeField]
    private Transform faceUpPile;
    [SerializeField]
    private GameObject cardProtoPrefab;

    private Vector2 screenPosition;

    public RuntimeAnimatorController ac1;

    private void Awake()
    {
        animController = GetComponent<Animator>();
        rectTransform = GetComponent<RectTransform>();

        var canvas = GameObject.Find("Canvas").GetComponent<RectTransform>();

        screenPosition = Camera.main.WorldToScreenPoint(canvas.position);


        Vector3 worldPoint;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.GetComponent<RectTransform>(), GetComponent<RectTransform>().rect.position, null, out worldPoint);
    }


    // IPointerDownHandler function
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log(eventData.selectedObject);

        bool isLeftMouseButtonPressed = Input.GetMouseButton(0);
        bool isRightMouseButtonPressed = Input.GetMouseButton(1);

        Debug.Log("Rect Position: " + GetComponent<RectTransform>().anchoredPosition); // position relative to parent UI gameObject
        Debug.Log("LocalPosition: " + transform.localPosition);
        Debug.Log("Global position: " + transform.TransformPoint(Vector2.zero));


        
        // Q: How do I obtain smooth animation where the card that once belonged to the player's hand
        // gets reparented to the faceUpPile while maintaining its old position relative to canvas?
        if (isLeftMouseButtonPressed)
        {
            if(isRaised)
            {
                // Rewrite to use AnimationEvent instead to swap out the runtimeAnimatorController and to set the position 
                // when changing a rect transform's parent, it will modify its position to remain relative to its anchor
                animController.enabled = false;
                transform.SetParent(faceUpPile, true);
                var rectTransform = GetComponent<RectTransform>();
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                rectTransform.DOAnchorMax(new Vector2(0.5f, 0.5f), 0.5f);
                rectTransform.DOAnchorMin(new Vector2(0.5f, 0.5f), 0.5f);
                rectTransform.DOAnchorPos(Vector2.zero, 0.5f);
                rectTransform.DORotate(new Vector3(0f, 0f, 30f), 0.5f);

                //StartCoroutine(DelayBoy());

                //animController.enabled = true;
                //GetComponent<RectTransform>().anchoredPosition = new Vector2(screenPosition.x, screenPosition.y);
                //AnchorPresetsUtils.AssignAnchor(AnchorPresets.MIDDLE_CENTER, ref rectTransform);
                //animController.SetBool("isGivenToDealer", true);
                //animController.SetInteger("animIndex", animIndex);

                // Instantiate a different card in place whose position is relative to the faceUpPile
                //var cardProto = Instantiate(cardProtoPrefab, faceUpPile, true);
                //// ^ on Awake of CardProto Component... have card immediately move up
                //Destroy(this.gameObject);
            }
            else
            {
                if(animController.enabled)
                {
                    isRaised = true;
                    animController?.SetBool("isRaised", isRaised);
                }
            }
        }
        else if (isRightMouseButtonPressed)
        {
            if(animController.enabled)
            {
                isRaised = false;
                animController?.SetBool("isRaised", isRaised);
            }
            else
            {
                animController.enabled = true;
            }
        }
    }

    // May need to use dotween here instead to tween from old position to new anchored position
    private IEnumerator DelayBoy()
    {
        //animController.runtimeAnimatorController = ac1;
        //animController.enabled = true;

        var rectTransform = GetComponent<RectTransform>();
        //Vector2 oldPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y);
        //Vector2 oldLocalPosition = new Vector2(transform.localPosition.x, transform.localPosition.y);
        yield return new WaitForSeconds(0.25f);

        //AnchorPresetsUtils.AssignAnchor(AnchorPresets.MIDDLE_CENTER, ref rectTransform);
        //Vector2 newLocalPosition = new Vector2(transform.localPosition.x, transform.localPosition.y);
        //Vector2 localPositionDiff = newLocalPosition - oldLocalPosition;
        //oldPosition.y = oldPosition.y - localPositionDiff.y;
        //rectTransform.anchoredPosition = oldPosition;

        //rectTransform.DOAnchorMax(new Vector2(0.5f, 0.5f), 0.25f);
        //rectTransform.DOAnchorMin(new Vector2(0.5f, 0.5f), 0.25f);
        //rectTransform.DOAnchorPos(Vector2.zero, 0.25f);
        //rectTransform.DORotate(new Vector3(0f, 0f, 30f), 0.25f);
    }

}
