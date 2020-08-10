using UnityEngine;
using UnityEngine.UI;

public class DrawPileCount : MonoBehaviour
{
    private Text drawPileCountLabel;
    private void Awake()
    {
        drawPileCountLabel = GetComponent<Text>();
        Dealer.OnDrawPileCountChanged += Dealer_OnDrawPileCountChanged;
    }

    private void OnDestroy()
    {
        Dealer.OnDrawPileCountChanged -= Dealer_OnDrawPileCountChanged;
    }

    public void Dealer_OnDrawPileCountChanged(Dealer dealer)
    {
        drawPileCountLabel.text = dealer.DrawPileCount.ToString();
    }
}
