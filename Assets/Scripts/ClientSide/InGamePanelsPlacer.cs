using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ProjectAce;

public class InGamePanelsPlacer : MonoBehaviour
{
    private List<GameObject> cardMats;
    private List<NetworkPlayerController> opponents;

    private void Awake()
    {
        cardMats = GameObject.FindGameObjectsWithTag("OpponentCardMat").ToList();
        ProjectAceNetworkManager.OnReceiveAllPlayerPanels += ToggleCardMatsVisibility;
    }

    private void OnDestroy()
    {
        ProjectAceNetworkManager.OnReceiveAllPlayerPanels -= ToggleCardMatsVisibility;
    }

    private void ToggleCardMatsVisibility()
    {
        opponents = FindObjectsOfType<NetworkPlayerController>().Where(npc => npc.hasAuthority == false).ToList();

        // Turn off cardMats not in use
        if(cardMats.Count > opponents.Count)
        {
            for(int i = opponents.Count;i < cardMats.Count; ++i)
            {
                cardMats[i].SetActive(false);
            }
        }
    }
}
