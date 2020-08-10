using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;


public class PlayAgainPanel : MonoBehaviour
{
    private Text winnerText;
    private Button exitButton;
    // client side only
    private Button playAgainButton;
    private Button cancelButton;
    private ProjectAceNetworkManager manager;

    private void Awake()
    {
        winnerText = transform.Find("WinnerText")?.GetComponent<Text>();
        playAgainButton = transform.Find("OptionsPanel/PlayAgainButton")?.GetComponent<Button>();
        exitButton = transform.Find("OptionsPanel/ExitButton")?.GetComponent<Button>();
        cancelButton = transform.Find("WaitingOnHostPanel/CancelButton")?.GetComponent<Button>();

        exitButton?.onClick.AddListener(Disconnect);        
        manager = FindObjectOfType<ProjectAceNetworkManager>();
    }

    public void RegisterClientSideListeners()
    {
        playAgainButton?.onClick.AddListener(ClientOnlyPlayAgainClicked);
        cancelButton?.onClick.AddListener(ClientOnlyCancelClicked);
    }

    private void OnDestroy()
    {
        playAgainButton?.onClick.RemoveListener(ClientOnlyPlayAgainClicked);
        exitButton?.onClick.RemoveListener(Disconnect);
        cancelButton?.onClick.RemoveListener(ClientOnlyCancelClicked);
    }

    private void ClientOnlyPlayAgainClicked()
    {
        manager.WaitingOnHostPanel?.SetActive(true);
        manager.OptionsPanel?.SetActive(false);
    }

    private void ClientOnlyCancelClicked()
    {
        manager.WaitingOnHostPanel?.SetActive(false);
        manager.OptionsPanel?.SetActive(true);
    }

    private void Disconnect()
    {
        manager.Disconnect();
    }

    public void SetWinnerText(int clientConnectionId)
    {
        if(clientConnectionId == 0)
        {
            winnerText.text = "Leader Wins!";
        }
        else
        {
            winnerText.text = string.Format("Player {0} wins!", clientConnectionId);
        }
    }

    public void SetWinnerText(string winnerName)
    {
        winnerText.text = string.Format("{0} Wins!", winnerName);
    }
}
