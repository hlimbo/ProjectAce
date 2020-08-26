using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using System;
using System.Collections.Generic;
using ProjectAce;
using ProjectAce.CustomNetworkMessages;
using System.Linq;

public class ProjectAceNetworkManager : NetworkManager
{
    public enum GameState
    {
        GAME_LAUNCH,
        LOBBY,
        GAME_IN_PROGRESS,
        GAME_END
    };

    public static GameState currentState;

    [SerializeField]
    private int startingCardCountPerPlayer = 8;
    [SerializeField]
    private int initialTimeLeftPerPlayer = 10; // seconds
    public int InitialTimeLeftPerPlayer => initialTimeLeftPerPlayer;

    [SerializeField]
    private GameObject readyPrefab;
    [SerializeField]
    private GameObject dealerPrefab;
    [SerializeField]
    private GameObject playerPanelPrefab;

    // this needs to be set when server is in SampleScene
    // On client side
    private Text drawPileCountText;

    // TODO: to support headless mode, these should only be maintained by the server with the exception of networkPlayerControllers
    // Maintained in both client and server
    public readonly Dictionary<int, NetworkPlayerController> networkPlayerControllers = new Dictionary<int, NetworkPlayerController>();
    public readonly Dictionary<int, ReadyPanel> readyPanels = new Dictionary<int, ReadyPanel>();
    public readonly Dictionary<int, PlayerPanel> playerPanels = new Dictionary<int, PlayerPanel>();

    // Maintained server-side only
    public readonly Dictionary<int, NameTag> playerNames = new Dictionary<int, NameTag>();

    public readonly Dictionary<int, bool> playerNumbers = new Dictionary<int,bool>()
    {
        { 1, false },
        { 2, false },
        { 3, false },
        { 4, false }
    };


    private int drawPileCount;

    // Only exists Server-Side
    private Dealer dealer;
    // array with index representing turnOrder and value representing connectionId
    public readonly List<int> turnOrder = new List<int>();
    private int currentTurnIndex = 0;

    private PlayAgainPanel playAgainPanel;
    public PlayAgainPanel PlayAgainPanel => playAgainPanel;
    
    private GameObject optionsPanel;
    public GameObject OptionsPanel => optionsPanel;

    private GameObject waitingOnHostPanel;
    public GameObject WaitingOnHostPanel => waitingOnHostPanel;

    // server side ~ give card to dealer animations
    public const int ANIM_VARIATION_COUNT = 5;
    private int animIndex = 0;

    // default to host
    private int gameLeaderIndex = 0;
    public int GameLeaderIndex => gameLeaderIndex;

    public override void Awake()
    {
        base.Awake();
        Utils.LoadCardAssets();
        currentState = GameState.GAME_LAUNCH;

        if(isHeadless)
        {
            Debug.Log("[Headless Mode]: Starting Server");
            this.StartServer();
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        Debug.LogFormat("[Server]: Scene changed to {0}", sceneName);
        Debug.LogFormat("[Server]: OnlineScene: {0}", onlineScene);
        Debug.LogFormat("[Server]: OnServerSceneChanged current game state: {0}", currentState);

        if (onlineScene.Equals(sceneName))
        {
            Debug.LogFormat("[Server]: Current State: {0}", currentState);
            if(currentState == GameState.GAME_END)
            {
                ResetGame();
                currentState = GameState.LOBBY;
                if (NetworkServer.connections.Count > 0)
                {
                    gameLeaderIndex = NetworkServer.connections.Keys.Min();
                }
            }

            InitializeDealer();
        }

    }

    private void ResetGame()
    {
        playerPanels.Clear();
        readyPanels.Clear();
        networkPlayerControllers.Clear();
        turnOrder.Clear();
        currentTurnIndex = 0;
        animIndex = 0;
        dealer = null;
        gameLeaderIndex = 0;

        var playerNums = playerNumbers.Keys.ToArray();
        foreach(var playerNum in playerNums)
        {
            playerNumbers[playerNum] = false;
        }
    }

    private void InitializeDealer()
    {
        if(dealer == null)
        {
            dealer = Instantiate(dealerPrefab)?.GetComponent<Dealer>();
            dealer.PrepareDeck();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentState = GameState.LOBBY;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        currentState = GameState.GAME_LAUNCH;
    }

    public void TryAddCardToFaceUpPile(int clientConnectionId, Card card)
    {
        NetworkConnectionToClient conn = NetworkServer.connections[clientConnectionId];
        if(GameRules2.ValidateCard(dealer.TopCard, card))
        {
            bool canAddCard = dealer.AddCardToFaceUpPile2(card);
            if (canAddCard)
            {
                if (networkPlayerControllers.ContainsKey(clientConnectionId))
                {
                    networkPlayerControllers[clientConnectionId].hasPlayedCardOrComboThisTurn = true;
                }

                Debug.Log("[SERVER]: CARD CAN BE PLAYED THIS TURN!!!!!! " + card);
                networkPlayerControllers[clientConnectionId].RpcRemoveOpponentCardFromHand(card, animIndex);
                networkPlayerControllers[clientConnectionId].TargetRemoveMyCardFromHand(conn, card, animIndex);
                animIndex = (animIndex + 1) % ANIM_VARIATION_COUNT;
                GoToNextTurn();
            }
            else
            {
                networkPlayerControllers[clientConnectionId].TargetCardPlacementFailed(conn, card);
            }
        }
        else
        {
            networkPlayerControllers[clientConnectionId].TargetCardPlacementFailed(conn, card);
        }
    }

    public void DealerGiveCardToPlayer(int connectionId)
    {
        if(dealer.DrawPileCount == 0)
        {
            Debug.Log("Server UpdateDrawPileCount... draw pile is empty... will transfer all cards but 1 from face up pile to draw pile");
            dealer.TransferCardsFromFaceUpPileToDrawPile();
        }

        var newCard = dealer.GiveCard2();
        if (newCard != null && networkPlayerControllers.ContainsKey(connectionId))
        {
            networkPlayerControllers[connectionId].myCards.Add((Card)newCard);
            OnServerUpdateDrawPileCount();
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        var player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        // locate ui element references on the server
        if(networkPlayerControllers.Count == 0)
        {
            if(NetworkServer.localClientActive)
            {
                // For Hosts (game clients that also act as the server, the host will need to get a reference to the DrawPileCountLabel)
                if (drawPileCountText == null)
                {
                    drawPileCountText = GameObject.Find("DrawPileCountLabel")?.GetComponent<Text>();
                }
                if (playAgainPanel == null)
                {
                    playAgainPanel = FindObjectOfType<PlayAgainPanel>();
                    optionsPanel = playAgainPanel?.transform.Find("OptionsPanel")?.gameObject;
                    waitingOnHostPanel = playAgainPanel?.transform.Find("WaitingOnHostPanel")?.gameObject;
                    waitingOnHostPanel?.SetActive(false);
                    playAgainPanel.gameObject.SetActive(false);
                }
            }

        }

        networkPlayerControllers[conn.connectionId] = player.GetComponent<NetworkPlayerController>();
        playerPanels[conn.connectionId].SetNetworkPlayerControllerNetId(networkPlayerControllers[conn.connectionId].netId);
    }


    // called n times where n = number of clients connected which means calling NetworkServer.Spawn will spawn the same number of objects on 1 client and the next
    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
        if(SceneManager.GetActiveScene().path.Equals(onlineScene))
        {
            if(currentState == GameState.LOBBY)
            {
                var panel = Instantiate(readyPrefab);
                NetworkServer.Spawn(panel, conn);
                readyPanels[conn.connectionId] = panel.GetComponent<ReadyPanel>();
                DetermineIfAllClientsAreReady();

                // Spawn player-panel in Lobby
                var playerPanel = Instantiate(playerPanelPrefab);
                NetworkServer.Spawn(playerPanel, conn);

                playerPanels[conn.connectionId] = playerPanel.GetComponent<PlayerPanel>();
                foreach (var playerNumber in playerNumbers.Keys)
                {
                    bool isTaken = playerNumbers[playerNumber];
                    if (!isTaken)
                    {
                        playerPanels[conn.connectionId].playerNumber = playerNumber;
                        playerNumbers[playerNumber] = true;
                        Debug.LogFormat("[Server]: Player number assigned to connection id {0} is {1}", conn.connectionId, playerNumber);
                        break;
                    }
                }
            }
        }
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);

        Debug.Log("OnClientSceneChanged: " + SceneManager.GetActiveScene().name);

        if(SceneManager.GetActiveScene().path.Equals(onlineScene))
        {
            Debug.Log("OnClientSceneChanged called");
            drawPileCountText = GameObject.Find("DrawPileCountLabel")?.GetComponent<Text>();
            if (playAgainPanel == null)
            {
                playAgainPanel = FindObjectOfType<PlayAgainPanel>();
                playAgainPanel?.RegisterClientSideListeners();
                optionsPanel = playAgainPanel?.transform.Find("OptionsPanel")?.gameObject;
                waitingOnHostPanel = playAgainPanel?.transform.Find("WaitingOnHostPanel")?.gameObject;
                waitingOnHostPanel?.SetActive(false);
                playAgainPanel.gameObject.SetActive(false);
            }
        }
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Debug.Log("[Server]: OnServerDisconnect removing connectionId: " + conn.connectionId);

        if(turnOrder.Count > 0)
        {
            int oldConnectionId = turnOrder[currentTurnIndex];
            turnOrder.Remove(conn.connectionId);
            if (oldConnectionId == conn.connectionId && turnOrder.Count > 0)
            {
                Debug.Log("[Server]: Player whose turn was still ongoing just disconnected from the server. Server will pick the next available player to start their turn.");

                if (currentTurnIndex >= turnOrder.Count)
                {
                    currentTurnIndex = 0;
                }
                else
                {
                    // need to move the index to the left by 1 so we don't skip any connectionIds
                    currentTurnIndex = currentTurnIndex <= 0 ? 0 : currentTurnIndex - 1;
                }
            }

            if (currentState == GameState.GAME_IN_PROGRESS)
            {
                if (oldConnectionId == conn.connectionId && turnOrder.Count > 0)
                {
                    Debug.Log("[Server]: Player whose turn was still ongoing just disconnected from the server. Server will pick the next available player to start their turn.");
                    // Move to the next turn in the event the player that disconnected is in the middle of their turn
                    if (currentTurnIndex >= 0)
                    {
                        int nextClientTurnConnectionId = turnOrder[currentTurnIndex];
                        if (NetworkServer.connections.ContainsKey(nextClientTurnConnectionId))
                        {
                            NetworkConnection nextClientConnection = NetworkServer.connections[nextClientTurnConnectionId];
                            networkPlayerControllers[nextClientTurnConnectionId].TargetEnableControls(nextClientConnection);
                            playerPanels[nextClientTurnConnectionId].StartCountdown(nextClientTurnConnectionId);
                        }
                    }
                }

                if (networkPlayerControllers.ContainsKey(conn.connectionId))
                {
                    // Give cards back to dealer
                    var cards = networkPlayerControllers[conn.connectionId].myCards.ToArray();
                    Debug.LogFormat("[Server]: Giving Cards back to Dealer! Card Count{0}", cards.Length);
                    foreach (var card in cards)
                    {
                        Debug.LogFormat("[Server]: Giving back {0}", card);
                    }

                    dealer.AddCardsBackToDrawPile(cards);
                    OnServerUpdateDrawPileCount();
                }
            }
        }

        if (playerNames.ContainsKey(conn.connectionId))
        {
            playerNames.Remove(conn.connectionId);
        }

        if (networkPlayerControllers.ContainsKey(conn.connectionId))
        {
            networkPlayerControllers.Remove(conn.connectionId);
        }

        // determine new game leader
        if (networkPlayerControllers.Count > 0 && gameLeaderIndex == conn.connectionId)
        {
            gameLeaderIndex = networkPlayerControllers.Keys.ToArray().Min();
        }

        if (currentState == GameState.GAME_END && networkPlayerControllers.ContainsKey(gameLeaderIndex))
        {
            // if game leader index changes let the new leader be able to reset the game
            networkPlayerControllers[gameLeaderIndex].TargetHostEnablePlayAgainButton(NetworkServer.connections[gameLeaderIndex]);
        }

        if (readyPanels.ContainsKey(conn.connectionId))
        {
            readyPanels.Remove(conn.connectionId);
            if (currentState == GameState.LOBBY)
            {
                DetermineIfAllClientsAreReady();
            }
        }

        if (playerPanels.ContainsKey(conn.connectionId))
        {
            int playerNumber = playerPanels[conn.connectionId].playerNumber;
            if(playerNumber != 0)
            {
                playerNumbers[playerNumber] = false;
            }
            playerPanels.Remove(conn.connectionId);
        }

        // Reset game
        if (isHeadless && networkPlayerControllers.Count == 0)
        {
            currentState = GameState.GAME_END;
            playerNames.Clear();
            ServerChangeScene(onlineScene);
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<DrawPileMessage>(OnClientDrawPileMessageReceived, false);
    }

    private void OnClientDrawPileMessageReceived(NetworkConnection conn, DrawPileMessage message)
    {
        drawPileCount = message.cardsLeft;
        drawPileCountText.text = message.cardsLeft.ToString();
    }

    private void SpawnInGamePlayerPanels()
    {
        foreach(var connection in NetworkServer.connections)
        {
            var panel = Instantiate(playerPanelPrefab);
            playerPanels[connection.Key] = panel.GetComponent<PlayerPanel>();

            foreach (var playerNumber in playerNumbers.Keys)
            {
                bool isTaken = playerNumbers[playerNumber];
                if (!isTaken)
                {
                    playerPanels[connection.Key].playerNumber = playerNumber;
                }
            }


            playerPanels[connection.Key].SetNetworkPlayerControllerNetId(networkPlayerControllers[connection.Key].netId);
            //playerPanels[connection.Key].RpcOnClientPlayerPanelSpawned(networkPlayerControllers.Count());
                 
            NetworkServer.Spawn(panel, connection.Value);
        }
    }

    private void ConnectedPlayersReceiveCards()
    {
        foreach (var kvp in networkPlayerControllers)
        {
            var clientConnectionId = kvp.Key;
            var npc = kvp.Value;
            var cards = DealerGiveCards();
            foreach (var card in cards)
            {
                npc.myCards.Add(card);
            }

            Debug.LogFormat("[Server]: Player {0} cards count: {1}: ", clientConnectionId, npc.myCards.Count);

            if(playerPanels.ContainsKey(clientConnectionId))
            {
                playerPanels[clientConnectionId].SetCardsLeft(npc.myCards.Count);
            }
        }
    }

    public void OnServerUpdateDrawPileCount()
    {
        var message = new DrawPileMessage();
        message.cardsLeft = dealer.DrawPileCount;
        NetworkServer.SendToAll(message);
    }

    public Card[] DealerGiveCards()
    {
        if(dealer != null)
        {
            // Debug.Log("DealerGiveCards: dealing x cards = " + startingCardCountPerPlayer);
            return dealer.GetCards(startingCardCountPerPlayer);
        }

        Debug.LogWarning("Dealer Object has not been instantied on the server and is null");
        return new Card[0];
    }

    public void DetermineIfAllClientsAreReady()
    {
        bool canEnableStartGameButton = true;
        foreach (var readyPanel in readyPanels)
        {
            // Debug.LogFormat("[SERVER]: connectionId {0} is ready? {1}", readyPanel.Value.connectionId, readyPanel.Value.IsReady);
            canEnableStartGameButton = canEnableStartGameButton && readyPanel.Value.IsReady;
        }

        // Enable start game button on host machine if all players connected are ready
        if(NetworkServer.connections.ContainsKey(gameLeaderIndex))
        {
            readyPanels[gameLeaderIndex].TargetToggleStartGameButton(NetworkServer.connections[gameLeaderIndex], canEnableStartGameButton);
        }
    }

    public void GoToNextTurn()
    {
        // Don't go to next turn if a player already won
        if(currentState == GameState.GAME_END)
        {
            return;
        }

        // client could randomly disconnect so currentTurnIndex should be re-evaluated
        if(currentTurnIndex < turnOrder.Count)
        {
            int endingTurnClientConnectionId = turnOrder[currentTurnIndex];
            if (networkPlayerControllers.ContainsKey(endingTurnClientConnectionId))
            {
                playerPanels[endingTurnClientConnectionId].StopCountdown(endingTurnClientConnectionId);
                
                var npc = networkPlayerControllers[endingTurnClientConnectionId];
                npc.TargetMoveRaisedCardsDown(NetworkServer.connections[endingTurnClientConnectionId]);
                npc.TargetDisableControls(NetworkServer.connections[endingTurnClientConnectionId]);
            }
        }

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        int nextTurnClientConnectionId = turnOrder[currentTurnIndex];
        if(networkPlayerControllers.ContainsKey(nextTurnClientConnectionId))
        {
            networkPlayerControllers[nextTurnClientConnectionId].TargetEnableControls(NetworkServer.connections[nextTurnClientConnectionId]);
            playerPanels[nextTurnClientConnectionId].StartCountdown(nextTurnClientConnectionId);
        }
    }

    private void AddActiveConnectionsToTurnOrderList()
    {
        foreach(var connId in NetworkServer.connections.Keys)
        {
            turnOrder.Add(connId);
        }
    }

    private void RandomizePlayerTurnOrders()
    {
        // Randomize Turn order if client connection count > 1
        if (turnOrder.Count == 2)
        {
            float randomChance = UnityEngine.Random.Range(0f, 1f);
            // Swap turn orders
            if (randomChance >= 0.5f)
            {
                var temp = turnOrder[0];
                turnOrder[0] = turnOrder[1];
                turnOrder[1] = temp;
            }
        }
        else if (turnOrder.Count > 2)
        {
            // fisher-yates randomization
            int k = NetworkServer.connections.Count;
            while (k > 1)
            {
                --k;
                int i = UnityEngine.Random.Range(0, k);
                var temp = turnOrder[i];
                turnOrder[i] = turnOrder[k];
                turnOrder[k] = temp;
            }
        }

        // debugging
        Debug.Log("TURN ORDER COUNT: " + turnOrder.Count);
        for (int i = 0; i < turnOrder.Count; ++i)
        {
            Debug.LogFormat("Turn {0} goes to player with connectionId {1}", i, turnOrder[i]);
        }
    }

    public void StartGame()
    {
        currentState = GameState.GAME_IN_PROGRESS;

        AddActiveConnectionsToTurnOrderList();
        RandomizePlayerTurnOrders();
        // for each active opponent, decide which card mat gets assigned to the opponent
        AssignOpponentCardMats();
        ConnectedPlayersReceiveCards();
        OnServerUpdateDrawPileCount();
    }

    public void AssignOpponentCardMats()
    {
        //int activePlayerCount = networkPlayerControllers.Count;
        foreach (var kvp in networkPlayerControllers)
        {
            var clientConnection = NetworkServer.connections[kvp.Key];
            var npc = kvp.Value;
            npc.RpcOnClientStartGame();
        }
    }

    public void CheckForTurn(int connectionId)
    {
        NetworkConnection playerConnection = NetworkServer.connections[connectionId];
        if (turnOrder[currentTurnIndex] == connectionId)
        {
            networkPlayerControllers[connectionId].TargetEnableControls(playerConnection);
            playerPanels[connectionId].StartCountdown(connectionId);
        }
        else
        {
            networkPlayerControllers[connectionId].TargetDisableControls(playerConnection);
        }
    }

    public void PlayGameAgain()
    {
        if(currentState == GameState.GAME_END && SceneManager.GetActiveScene().path.Equals(onlineScene))
        {
            ServerChangeScene(onlineScene);
        }
    }

    public void CheckForWinner(int clientConnectionId)
    {
        if(networkPlayerControllers.ContainsKey(clientConnectionId))
        {
            var networkPlayerController = networkPlayerControllers[clientConnectionId];
            if(networkPlayerController.myCards.Count == 0)
            {
                networkPlayerControllers[clientConnectionId].RpcEnablePlayAgainPanel(clientConnectionId, playerNames[clientConnectionId].name);
                networkPlayerControllers[gameLeaderIndex].TargetHostEnablePlayAgainButton(NetworkServer.connections[gameLeaderIndex]);
                currentState = GameState.GAME_END;
            }
        }

        // End Game
        if(currentState == GameState.GAME_END)
        {
            foreach (var kvp in NetworkServer.connections)
            {
                int connectionId = kvp.Key;
                playerPanels[connectionId].StopCountdown(connectionId);
                networkPlayerControllers[connectionId].TargetDisableControls(kvp.Value);
            }
        }
    }

    public void DealerEvaluateCardsToCombo(int clientConnectionId, Card[] cards)
    {
        NetworkPlayerController networkPlayerController;
        networkPlayerControllers.TryGetValue(clientConnectionId, out networkPlayerController);

        bool isComboValid = dealer.TopCard != null && GameRules2.DoCardsAddUpToTopCardValue((Card)dealer.TopCard, cards);
        if(isComboValid)
        {
            if (networkPlayerController != null)
            {
                networkPlayerController.hasPlayedCardOrComboThisTurn = true;
                // Remove cards from synclist
                foreach (var card in cards)
                {
                    networkPlayerController.myCards.Remove(card);
                    dealer.AddCardToFaceUpPile2(card);
                    Debug.Log("[SERVER]: Top card is " + dealer.TopCard);
                }

                int[] animIndices = new int[cards.Length];
                for (int i = 0; i < animIndices.Length; ++i)
                {
                    animIndices[i] = animIndex;
                    animIndex = (animIndex + 1) % ANIM_VARIATION_COUNT;
                }
                
                networkPlayerControllers[clientConnectionId].TargetRemoveCardsFromMyHand(NetworkServer.connections[clientConnectionId], cards, animIndices);
                networkPlayerControllers[clientConnectionId].RpcRemoveOpponentCardsFromHand(cards, animIndices);
                GoToNextTurn();
            }
        }
        else
        {
            if(NetworkServer.connections.TryGetValue(clientConnectionId, out NetworkConnectionToClient clientConnection))
            {
                networkPlayerController?.TargetOnComboInvalid(clientConnection, cards);
            }
        }
    }

    public void Disconnect()
    {
        currentState = GameState.GAME_LAUNCH;
        // Host
        if (NetworkServer.active && NetworkServer.localClientActive)
        {
            Debug.Log("Host is ending the server");
            this.StopHost();
        }
        else
        {
            Debug.Log("Client is disconnecting from the server");
            this.StopClient();
        }
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        base.OnClientError(conn, errorCode);
        Debug.Log("Client error occurred with errorCode: " + errorCode);
    }

    public override void OnServerError(NetworkConnection conn, int errorCode)
    {
        base.OnServerError(conn, errorCode);
        Debug.Log("Server error occurred with errorCode: " + errorCode);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        NetworkClient.UnregisterHandler<DrawPileMessage>();
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        Debug.LogFormat("[Server]: OnServerConnect Client connected! {0}", conn.connectionId);

        // prevent more players from connecting to the server
        if(currentState == GameState.GAME_IN_PROGRESS || currentState == GameState.GAME_END)
        {
            Debug.Log("[Server]: Game is already in progress and cannot accept anymore players");
            NetworkServer.RemovePlayerForConnection(conn, true);
            NetworkServer.RemoveConnection(conn.connectionId);
            conn.Disconnect();
            return;
        }
        
        if(networkPlayerControllers.Count == 0)
        {
            gameLeaderIndex = conn.connectionId;
            if (isHeadless && SceneManager.GetActiveScene().path.Equals(offlineScene))
            {
                Debug.LogFormat("[Server]: Changing Scenes to: {0}", onlineScene);
                ServerChangeScene(onlineScene);
            }
        }
    }

}
