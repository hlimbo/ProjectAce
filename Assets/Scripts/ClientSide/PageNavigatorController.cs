using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PageNavigatorController : MonoBehaviour
{
    [SerializeField]
    private int currentPageIndex;
    private Dictionary<string, GameObject> pages = new Dictionary<string, GameObject>();
    private Text pageNumberText;

    private Button nextPageBtn;
    private Button prevPageBtn;

    private void Awake()
    {
        pageNumberText = transform.Find("PageNumber")?.GetComponent<Text>();
        GameObject[] pagesArray = GameObject.FindGameObjectsWithTag("HowToPlayPage");
        currentPageIndex = 0;
        for(int i = 0;i < pagesArray.Length; ++i)
        {
            pages[pagesArray[i].name] = pagesArray[i];
            if(pagesArray[i].name.Equals("Page1"))
            {
                continue;
            }

            pagesArray[i].SetActive(false);
        }

        nextPageBtn = transform.Find("NextPageButton")?.GetComponent<Button>();
        prevPageBtn = transform.Find("BackPageButton")?.GetComponent<Button>();

        nextPageBtn?.onClick.AddListener(GotoNextPage);
        prevPageBtn?.onClick.AddListener(GotoPrevPage);

        pageNumberText.text = string.Format("1/{0}", pages.Count);
    }

    private void OnDestroy()
    {
        nextPageBtn?.onClick.RemoveListener(GotoNextPage);
        prevPageBtn?.onClick.RemoveListener(GotoPrevPage);
    }

    private void GotoNextPage()
    {
        pages[string.Format("Page{0}",currentPageIndex + 1)].SetActive(false);
        currentPageIndex = (currentPageIndex + 1) % pages.Count;
        pages[string.Format("Page{0}", currentPageIndex + 1)].SetActive(true);
        pageNumberText.text = string.Format("{0}/{1}", currentPageIndex + 1, pages.Count);
    }
    private void GotoPrevPage()
    {
        pages[string.Format("Page{0}", currentPageIndex + 1)].SetActive(false);
        currentPageIndex = (currentPageIndex - 1 < 0) ? pages.Count - 1 : currentPageIndex - 1;
        pages[string.Format("Page{0}", currentPageIndex + 1)].SetActive(true);
        pageNumberText.text = string.Format("{0}/{1}", currentPageIndex + 1, pages.Count);
    }
}
