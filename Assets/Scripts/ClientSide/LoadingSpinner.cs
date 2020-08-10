using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class LoadingSpinner : MonoBehaviour
{
    private Image image;
    private int imageIndex;
    private const int COUNT = 12;
    private Sprite[] spinnerImages = new Sprite[COUNT];
    private WaitForSeconds frameDelay = new WaitForSeconds(0.1f);

    private void Awake()
    {
        image = GetComponent<Image>();
        imageIndex = 0;

        for(int i = 0;i < COUNT; ++i)
        {
            spinnerImages[i] = Resources.Load<Sprite>($"LoadingSpinner/Images/loading-spinner {i}");
        }

        Assert.IsTrue(spinnerImages.Length > 0);
    }

    private void OnEnable()
    {
        image.sprite = null;
        imageIndex = 0;
        StartSpinningRoutine();
    }

    private void OnDisable()
    {
        StopSpinningRoutine();
    }

    public void StartSpinningRoutine()
    {
        StartCoroutine(AnimateSpinner());
    }

    private IEnumerator AnimateSpinner()
    {
        while(true)
        {
            image.sprite = spinnerImages[imageIndex];
            yield return frameDelay;
            imageIndex = (imageIndex + 1) % spinnerImages.Length;
        }
    }

    public void StopSpinningRoutine()
    {
        StopCoroutine(AnimateSpinner());
    }
}
