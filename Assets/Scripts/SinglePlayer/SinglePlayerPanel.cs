using DG.Tweening;
using ProjectAce;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SinglePlayerPanel : MonoBehaviour
{
    public string playerName = "Bobby";

    private Image timeLeftCircle;
    private Image avatarImage;
    private Text playerLabel;
    private Text cardsLeftText;
    private RectTransform rectTransform;
    private Transform uiCanvas;

    private Color originalLabelColor;
    private Image counterFx;
    private Sequence pulseSequence;

    public string avatarName;

    private float timeLeft;
    private float initialTimeLeft;


    private GameManager manager;
    public GameManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<GameManager>();
            }

            return manager;
        }
    }

    private void Awake()
    {
        playerLabel = transform.Find("PlayerName")?.GetComponent<Text>();
        timeLeftCircle = transform.Find("Avatar/Counter")?.GetComponent<Image>();
        counterFx = transform.Find("CounterFX")?.GetComponent<Image>();
        avatarImage = transform.Find("Avatar/PlayerImage")?.GetComponent<Image>();
        cardsLeftText = transform.Find("CardsLeft/Text")?.GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();

        uiCanvas = GameObject.Find("Canvas")?.transform;

        originalLabelColor = playerLabel.color;
        pulseSequence = DOTween.Sequence()
            .Append(counterFx.transform.DOScaleX(1.25f, 1f))
            .Join(counterFx.transform.DOScaleY(1.25f, 1f))
            .Join(counterFx.DOFade(0f, 1.5f))
            .SetLoops(-1, LoopType.Restart)
            .Pause();
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        playerLabel.text = playerName;
        initialTimeLeft = Manager.InitialTimeLeftPerPlayer;
        transform.SetParent(uiCanvas);
        rectTransform.anchoredPosition = new Vector2(0f, 0f);
        rectTransform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        // Hack - hides the player panel from being visible from the lobby by rendering it behind the lobby game object
        transform.SetAsFirstSibling();
    }

    public void SetToBottomLeftAnchor()
    {
        AnchorPresetsUtils.AssignAnchor(AnchorPresets.BOTTOM_LEFT, ref rectTransform);
    }

    public void AnimateFillAmount(float timeLeft, float initialTimeLeft, float delta)
    {
        timeLeftCircle.DOFillAmount(timeLeft / initialTimeLeft, delta);
    }

    public void ToggleTimerUI(bool active)
    {
        timeLeftCircle.fillAmount = 1f;
        timeLeftCircle.gameObject.SetActive(active);

        if(active)
        {
            pulseSequence.Play();
        }
        else
        {
            pulseSequence.Rewind();
        }
    }

    public void ToggleHighlightPlayerLabel(bool toggle)
    {
        if (toggle)
        {
            playerLabel.color = Color.yellow;
            playerLabel.fontStyle = FontStyle.Bold;
        }
        else
        {
            playerLabel.color = originalLabelColor;
            playerLabel.fontStyle = FontStyle.Normal;
        }
    }

    public void SetAvatarImage(Sprite image)
    {
        avatarImage.sprite = image;
    }

    public void SetCardsLeft(int cardsLeft)
    {
        cardsLeftText.text = cardsLeft.ToString();
    }

    public void StartCountdown()
    {
        StopAllCoroutines();
        ToggleTimerUI(true);
        ToggleHighlightPlayerLabel(true);
        StartCoroutine(CountdownRoutine(Time.deltaTime));
    }

    public void StopCountdown()
    {
        ToggleTimerUI(false);
        ToggleHighlightPlayerLabel(false);
        Manager.MoveCardsDown();
        StopAllCoroutines();
    }

    private IEnumerator CountdownRoutine(float delta)
    {
        timeLeft = initialTimeLeft;
        while (timeLeft > 0)
        {
            yield return new WaitForSeconds(delta);
            timeLeft -= delta;
            timeLeftCircle.fillAmount = timeLeft / initialTimeLeft;
        }

        timeLeft = 0f;
        Manager.ResetTurn();
    }

}
