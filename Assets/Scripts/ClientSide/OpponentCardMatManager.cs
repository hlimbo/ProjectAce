using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectAce;
using System;

public class OpponentCardMatManager : MonoBehaviour
{
    private GameObject[] cardMats;
    // key = connectionId
    private Dictionary<int, GameObject> cardMatTable = new Dictionary<int, GameObject>();

    [SerializeField]
    private GameObject horizontalFaceDownCardPrefab;
    [SerializeField]
    private GameObject verticalFaceDownCardPrefab;

    private void Awake()
    {
        cardMats = GameObject.FindGameObjectsWithTag("OpponentCardMat");

        foreach(var mat in cardMats)
        {
            mat.SetActive(false);
        }
    }

    public GameObject GetCardMat(int connectionId)
    {
        if(cardMatTable.ContainsKey(connectionId))
        {
            return cardMatTable[connectionId];
        }

        return null;
    }

    public bool UnregisterMat(int connectionId)
    {
        if (cardMatTable.ContainsKey(connectionId))
        {
            cardMatTable[connectionId].SetActive(false);
            return cardMatTable.Remove(connectionId);
        }

        return false;
    }

    public void RegisterMats()
    {
        PlayerPanel[] playerPanels = FindObjectsOfType<PlayerPanel>();
        PlayerPanel ownedPlayerPanel = playerPanels.Where(p => p.hasAuthority).FirstOrDefault();

        if (playerPanels.Length > 2)
        {
            if(ownedPlayerPanel != null)
            {
                bool isOwnerEven = ownedPlayerPanel.playerNumber % 2 == 0;
                PlayerPanel oppositeSidePanel = playerPanels
                    .Where(p => !p.hasAuthority && isOwnerEven == (p.playerNumber % 2 == 0))
                    .FirstOrDefault();

                if(oppositeSidePanel != null)
                {
                    AssignOppositeSideMat(oppositeSidePanel);
                }
                
                if(isOwnerEven && playerPanels.Length == 3 && oppositeSidePanel == null)
                {
                    PlayerPanel[] sidePanels = playerPanels
                        .Where(p => !p.hasAuthority)
                        .OrderBy(p => p.playerNumber)
                        .ToArray();

                    AssignSidePanelMats(sidePanels);
                }
                else
                {
                    PlayerPanel[] sidePanels = playerPanels
                        .Where(p => !p.hasAuthority && oppositeSidePanel.playerNumber != p.playerNumber)
                        .ToArray();

                    // swap order player panels get assigned to for a particular anchor
                    if(ownedPlayerPanel.playerNumber > oppositeSidePanel.playerNumber)
                    {
                        sidePanels = sidePanels.OrderByDescending(p => p.playerNumber).ToArray();
                    }
                    else
                    {
                        sidePanels = sidePanels.OrderBy(p => p.playerNumber).ToArray();
                    }

                    AssignSidePanelMats(sidePanels);

                }
            }
        }
        else if(playerPanels.Length == 2)
        {
            PlayerPanel unownedPanel = playerPanels.Where(p => !p.hasAuthority).FirstOrDefault();
            AssignOppositeSideMat(unownedPanel);
        }
    }

    private void AssignOppositeSideMat(PlayerPanel unownedPanel)
    {
        if (unownedPanel != null)
        {
            var mat = cardMats.Where(go => go.name.Equals("OpponentCardMat1")).FirstOrDefault();
            if (mat != null)
            {
                mat.SetActive(true);
                cardMatTable[unownedPanel.ConnectionId] = mat;
                var opponentCardMat = mat.GetComponent<OpponentCardMat>();
                opponentCardMat.SetFaceDownCardPrefab(verticalFaceDownCardPrefab);

                unownedPanel.anchorPreset = AnchorPresets.TOP_RIGHT;
                var rectTransform = unownedPanel.GetComponent<RectTransform>();
                AnchorPresetsUtils.AssignAnchor(unownedPanel.anchorPreset, ref rectTransform);
                // offset
                rectTransform.anchoredPosition = new Vector2(-50f, 0f);
            }
        }
        else
        {
            Debug.LogError("unownedpanel cannot be assigned as it is null");
        }
    }

    private void AssignSidePanelMats(PlayerPanel[] sidePanels)
    {
        if (sidePanels.Length == 0)
        {
            Debug.LogError("OpponentCardMatManager AssignSidePanel Mats error... SidePanels is empty");
        }

        var sideCardMats = cardMats.Where(c => c.name.Equals("OpponentCardMat2") || c.name.Equals("OpponentCardMat3")).ToArray();
      
        for(int i = 0;i < Math.Min(sideCardMats.Length, sidePanels.Length); ++i)
        {
            var mat = sideCardMats[i];
            var sidePanel = sidePanels[i];
            mat.SetActive(true);
            cardMatTable[sidePanel.ConnectionId] = mat;

            var opponentCardMat = mat.GetComponent<OpponentCardMat>();
            opponentCardMat.SetFaceDownCardPrefab(horizontalFaceDownCardPrefab);

            switch (opponentCardMat.anchorPreset)
            {
                case AnchorPresets.MIDDLE_LEFT:
                    sidePanel.anchorPreset = AnchorPresets.TOP_LEFT;
                    break;
                case AnchorPresets.MIDDLE_RIGHT:
                    sidePanel.anchorPreset = AnchorPresets.BOTTOM_RIGHT;
                    break;
                default:
                    Debug.LogError("OpponentCardMatManager RegisterMats: could not assign a valid player panel anchor");
                    break;
            }

            var rectTransform = sidePanel.GetComponent<RectTransform>();
            AnchorPresetsUtils.AssignAnchor(sidePanel.anchorPreset, ref rectTransform);
        }
    }
}
