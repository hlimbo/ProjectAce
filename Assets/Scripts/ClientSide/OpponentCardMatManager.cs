using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentCardMatManager : MonoBehaviour
{
    private GameObject[] cardMats;
    private Dictionary<GameObject, bool> cardMatTable;

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

        cardMatTable = new Dictionary<GameObject, bool>();
    }

    public GameObject RegisterMat()
    {
        foreach(var mat in cardMats)
        {
            if(!cardMatTable.ContainsKey(mat) || cardMatTable[mat] == false)
            {
                mat.SetActive(true);
                cardMatTable[mat] = true;

                var opponentCardMat = mat.GetComponent<OpponentCardMat>();
                if(mat.name.Equals("OpponentCardMat1"))
                {
                    opponentCardMat.SetFaceDownCardPrefab(verticalFaceDownCardPrefab);
                }
                else
                {
                    opponentCardMat.SetFaceDownCardPrefab(horizontalFaceDownCardPrefab);
                }
                
                return mat;
            }
        }

        // Return null if unable to register a card mat for the opponent
        return null;
    }

    public bool UnregisterMat(GameObject mat)
    {
        if(cardMatTable.ContainsKey(mat))
        {
            mat.SetActive(false);
            cardMatTable[mat] = false;
            return true;
        }

        return false;
    }
}
