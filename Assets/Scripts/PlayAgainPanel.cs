using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using ProjectAce;

public class PlayAgainPanel : MonoBehaviour
{
    private Text winnerText;
    private Button exitButton;
    // client side only
    private Button playAgainButton;
    private Button cancelButton;
    private ProjectAceNetworkManager manager;
    private Image winningAvatar;

    private GameObject optionsPanel;
    private GameObject waitingOnHostPanel;

    private void Awake()
    {
        winnerText = transform.Find("WinnerText")?.GetComponent<Text>();
        playAgainButton = transform.Find("OptionsPanel/PlayAgainButton")?.GetComponent<Button>();
        exitButton = transform.Find("OptionsPanel/ExitButton")?.GetComponent<Button>();
        cancelButton = transform.Find("WaitingOnHostPanel/CancelButton")?.GetComponent<Button>();
        winningAvatar = transform.Find("WinningAvatar/PlayerImage")?.GetComponent<Image>();

        waitingOnHostPanel = transform.Find("WaitingOnHostPanel")?.gameObject;
        optionsPanel = transform.Find("OptionsPanel")?.gameObject;

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
        waitingOnHostPanel?.SetActive(true);
        optionsPanel?.SetActive(false);
    }

    private void ClientOnlyCancelClicked()
    {
        waitingOnHostPanel?.SetActive(false);
        optionsPanel?.SetActive(true);
    }

    private void Disconnect()
    {
        manager.Disconnect();
    }

    public void SetWinnerText(string winnerName)
    {
        winnerText.text = string.Format("{0} Wins!", winnerName);
    }

    public void SetWinningAvatarMulti(int connectionId)
    {
        var winningPanel = FindObjectsOfType<PlayerPanel>()
            .Where(panel => panel.ConnectionId == connectionId).FirstOrDefault();

        if(winningPanel != null && Utils.avatarAssets.ContainsKey(winningPanel.avatarName))
        {
            winningAvatar.sprite = Utils.avatarAssets[winningPanel.avatarName];
        }
    }

    public void SetWinningAvatarSingle(SinglePlayerPanel playerPanel)
    {
        if (playerPanel != null && Utils.avatarAssets.ContainsKey(playerPanel.avatarName))
        {
            winningAvatar.sprite = Utils.avatarAssets[playerPanel.avatarName];
        }
    }
}
