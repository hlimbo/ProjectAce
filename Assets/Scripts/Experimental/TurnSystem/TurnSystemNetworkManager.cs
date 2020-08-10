using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TurnSystemNetworkManager : NetworkManager
{
    [SerializeField]
    private GameObject timePanelPrefab;

    public readonly Dictionary<int, TimePanel> timePanels = new Dictionary<int, TimePanel>();
    public readonly Dictionary<int, TurnPlayerController> playerControllers = new Dictionary<int, TurnPlayerController>();

    // managed by server only
    // array with index representing turnOrder and value representing connectionId
    public readonly List<int> turnOrder = new List<int>();

    private int currentTurn = 0;
    public int CurrentTurn => currentTurn;

    // Called on Server only
    public void GoToNextTurn()
    {
        currentTurn = (currentTurn + 1) % NetworkServer.connections.Count;
    }
    
    // Called on Server only
    public int GenerateRandomNumber()
    {
        return Random.Range(0, 10);
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("OnServerAddPlayer");
        var player = Instantiate(playerPrefab);
        playerControllers[conn.connectionId] = player.GetComponent<TurnPlayerController>();
        NetworkServer.AddPlayerForConnection(conn, player);
        // Here I can possibly define a callback that allows a client instanced component to receive a reference to a networkBehaviour
        // aand have the client instanced component register to the callback

        var panel = Instantiate(timePanelPrefab);
        NetworkServer.Spawn(panel, conn);
        timePanels[conn.connectionId] = panel.GetComponent<TimePanel>();

        // Randomizing turn order
        if(NetworkServer.connections.Count >= 2)
        {
            foreach (var timePanel in timePanels)
            {
                turnOrder.Add(timePanel.Key);
            }


            // Not so random for 2 players
            int k = NetworkServer.connections.Count;
            while (k > 1)
            {
                --k;
                int i = Random.Range(0, k);
                var temp = turnOrder[i];
                turnOrder[i] = turnOrder[k];
                turnOrder[k] = temp;
            }

            // Setting the turnOrder for all TurnPlayerControllers
            for (int i = 0; i < turnOrder.Count; ++i)
            {
                int clientConnectionId = turnOrder[i];
                if (playerControllers.ContainsKey(clientConnectionId))
                {
                    playerControllers[clientConnectionId].turnIndex = i;
                }
            }

            Debug.Log("Playercontrollers count: " + playerControllers.Count);

            // Find the playerController who can make moves first
            foreach (var playerController in playerControllers)
            {
                Debug.Log("Checking for my turn with connectionId: " + playerController.Key);
                Debug.Log("Turn Index: " + playerController.Value.turnIndex);
                if (playerController.Value.turnIndex == CurrentTurn)
                {
                    playerController.Value.TargetEnableControls(NetworkServer.connections[playerController.Key]);
                    timePanels[playerController.Key].StartCountdown(playerController.Key);
                }
                else
                {
                    playerController.Value.TargetDisableControls(NetworkServer.connections[playerController.Key]);
                }
            }
        }
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
    }
}
