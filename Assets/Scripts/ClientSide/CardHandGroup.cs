using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CardHandGroup : UIBehaviour, ILayoutGroup
{
    public enum CardHandGroupOrientation
    {
        HORIZONTAL,
        VERTICAL
    };

    [SerializeField]
    private CardHandGroupOrientation orientation;

    [SerializeField]
    [Tooltip("Sets the anchors of all child UI elements contained within this game-object")]
    private TextAnchor childAlignment;

    [SerializeField]
    [Tooltip("Only affects the layout if orientation is set to VERTICAL. Used to center align the cards based on its child's anchor")]
    [Range(1, 12)]
    private int verticalAlignmentFactor = 6;

    private RectTransform root;

    [SerializeField]
    [Tooltip("The higher the number, the closer the cards group together, the lower the number the further apart the cards appear from each other")]
    [Range(1, 20)]
    private int groupFactor;

    [SerializeField]
    private int childCount;

    protected override void Awake()
    {
        base.Awake();
        root = GetComponent<RectTransform>();
    }

    // Called in the Unity Editor only
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!CanvasUpdateRegistry.IsRebuildingLayout())
        {
            LayoutRebuilder.MarkLayoutForRebuild(root);
        }
        else
        {
            StartCoroutine(DelayMarkLayoutForRebuild());
        }
    }
#endif

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();

        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!CanvasUpdateRegistry.IsRebuildingLayout())
        {
            LayoutRebuilder.MarkLayoutForRebuild(root);
        }
        else
        {
            StartCoroutine(DelayMarkLayoutForRebuild());
        }
    }

    private IEnumerator DelayMarkLayoutForRebuild()
    {
        // delay for 1 frame
        yield return null;
        LayoutRebuilder.MarkLayoutForRebuild(root);
    }

    // ILayoutGroup Method
    public void SetLayoutHorizontal()
    {
        if(orientation != CardHandGroupOrientation.HORIZONTAL)
        {
            return;
        }

        float totalWidth = 0;
        float lastWidth = 0;
        foreach (Transform child in transform)
        {
            var rectTransform = child.GetComponent<RectTransform>();
            //SetChildPivot(childAlignment, ref rectTransform);
            float width = rectTransform.rect.width;
            totalWidth += width;
            lastWidth = width;
        }

        float parentWidth = totalWidth;
        if(transform.childCount > 1)
        {
            parentWidth = (totalWidth / groupFactor) + (lastWidth / groupFactor) * 2;
        }

        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth);
        root.ForceUpdateRectTransforms();

        // adding a new child item to this game object
        for (int i = 0; i < transform.childCount; ++i)
        {
            Transform child = transform.GetChild(i);
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2((rectTransform.rect.width / groupFactor) * i, rectTransform.anchoredPosition.y);
        }

        childCount = transform.childCount;
    }

    // ILayoutGroup Method
    public void SetLayoutVertical()
    {
        if(orientation != CardHandGroupOrientation.VERTICAL)
        {
            return;
        }

        float totalHeight = 0;
        float lastHeight = 0;
        foreach (Transform child in transform)
        {
            float height = child.GetComponent<RectTransform>().rect.height;
            totalHeight += height;
            lastHeight = height;
        }

        float parentHeight = totalHeight;
        if(transform.childCount > 1)
        {
            parentHeight = (totalHeight / groupFactor) + (lastHeight / groupFactor) * 2;
        }

        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentHeight);
        root.ForceUpdateRectTransforms();

        // code assumes that each child rect transform has the same height
        for (int i = 0; i < transform.childCount; ++i)
        {
            Transform child = transform.GetChild(i);
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            // keeps the cards centered and aligned
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, (rectTransform.rect.height / groupFactor) * i - ((lastHeight / verticalAlignmentFactor) * (transform.childCount - 1)));
        }
    }
}
