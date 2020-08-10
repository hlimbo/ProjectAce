using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAce;

public class CheckWinner : MonoBehaviour
{
    [SerializeField]
    private GameObject playAgainPanel;

    private void Awake()
    {
        PlayerController.OnCardGivenToDealer += PlayerController_OnCardGivenToDealer;
    }

    private void PlayerController_OnCardGivenToDealer(List<Card> hand)
    {
        if(hand.Count == 0)
        {
            playAgainPanel.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        PlayerController.OnCardGivenToDealer -= PlayerController_OnCardGivenToDealer;
    }

    // Attached to Play Again Button OnClick event
    public void PlayAgain()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
}
