using DG.Tweening;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnIndicator : MonoBehaviour
{
    public float transitionTime = 1f;
    public float delayInSeconds = 1f;
    private Image bgImage;
    private Text text;
    private Sequence fadeSequence;

    private void Awake()
    {
        bgImage = GetComponent<Image>();
        text = GetComponentInChildren<Text>();

        fadeSequence = DOTween.Sequence()
            .Append(bgImage.DOFade(0.9f, transitionTime))
            .Join(text.DOFade(1f, transitionTime))
            .AppendInterval(delayInSeconds)
            .Append(bgImage.DOFade(0f, transitionTime))
            .Join(text.DOFade(0f, transitionTime))
            .SetAutoKill(false)
            .Pause();
    }

    public void PlayFadeSequence()
    {
        fadeSequence.Play();
    }

    public void RestartSequence()
    {
        fadeSequence.Restart();
    }

    private void OnDestroy()
    {
        fadeSequence.Kill();
    }
}
