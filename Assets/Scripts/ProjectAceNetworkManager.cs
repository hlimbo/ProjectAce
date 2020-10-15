using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using ProjectAce;
using ProjectAce.CustomNetworkMessages;
using System.Linq;
using Mirror.Websocket;

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
    [Header("Server Config Settings")]
    private ServerConfigs serverConfigs;

    public int StartingCardCountPerPlayer => serverConfigs.startingCardCountPerPlayer;   
    // measured in seconds
    public int InitialTimeLeftPerPlayer => serverConfigs.initialTimeLeftPerPlayer;

    public string TcpPort => serverConfigs.tcpPort.ToString();
    public string WebsocketPort => serverConfigs.websocketPort.ToString();

    [Header("GameObject Prefabs")]
    [SerializeField]
    private GameObject readyPrefab;
    [SerializeField]
    private GameObject dealerPrefab;
    [SerializeField]
    private GameObject playerPanelPrefab;

    // this needs to be set when server is on the online scene
    // On client side
    private Text drawPileCountText;

    // Maintained server-side only
    public readonly Dictionary<int, NetworkPlayerController> networkPlayerControllers = new Dictionary<int, NetworkPlayerController>();
    public readonly Dictionary<int, ReadyPanel> readyPanels = new Dictionary<int, ReadyPanel>();
    public readonly Dictionary<int, PlayerPanel> playerPanels = new Dictionary<int, PlayerPanel>();
    public readonly Dictionary<int, NameTag> playerNames = new Dictionary<int, NameTag>();

    public readonly Dictionary<int, bool> playerNumbers = new Dictionary<int,bool>()
    {
        { 1, false },
        { 2, false },
        { 3, false },
        { 4, false }
    };

    public readonly Dictionary<int, string> avatarImageNames = new Dictionary<int, string>();

    public readonly Dictionary<int, List<Card>> pendingCardsByConnectionId = new Dictionary<int, List<Card>>();

    // Only exists Server-Side
    private Dealer dealer;
    public bool IsFaceUpPileEmpty => dealer.TopCard == null;

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
        Utils.LoadAvatarAssets();
        currentState = GameState.GAME_LAUNCH;

        if(isHeadless)
        {
            Debug.Log("[Headless Mode]: Starting Server");
            InitServerPorts();
        }

        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    private void SceneManager_activeSceneChanged(Scene oldScene, Scene newScene)
    {
        Debug.Log("Scene changing to: " + newScene.name);
        ResetGame();
        currentState = GameState.LOBBY;
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
        avatarImageNames.Clear();
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
            dealer.PrepareDeck(isDeckShuffled: false);
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

    public void CheckGameStatus(int clientConnectionId)
    {
        CheckForWinner(clientConnectionId);

        Debug.Log("Current Game Status: " + currentState);

        if (currentState == GameState.GAME_END)
        {
            HandleWinState(clientConnectionId);
        }
        else
        {
            GoToNextTurn();
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
            var npc = networkPlayerControllers[connectionId];
            npc.myCards.Add((Card)newCard);
            npc.TargetOnClientPlayDrawCardSound(npc.connectionToClient);
            npc.TargetDidAddNewCard(npc.connectionToClient, NetworkServer.connections.Count);
            OnServerUpdateDrawPileCount();
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        var player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        // Leader will also be the connection with the lowest connectionId
        if(networkPlayerControllers.Count > 0)
        {
            gameLeaderIndex = gameLeaderIndex > conn.connectionId ? conn.connectionId : gameLeaderIndex;
        }

        // locate ui element references on the server
        if (networkPlayerControllers.Count == 0)
        {
            gameLeaderIndex = conn.connectionId;
            if (NetworkServer.localClientActive)
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
                    playAgainPanel?.gameObject.SetActive(false);
                }
            }

        }

        networkPlayerControllers[conn.connectionId] = player.GetComponent<NetworkPlayerController>();
    }


    // called n times where n = number of clients connected which means calling NetworkServer.Spawn will spawn the same number of objects on 1 client and the next
    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
        if(SceneManager.GetActiveScene().path.Equals(onlineScene))
        {
            Debug.Log("[SERVER]: currentState: " + currentState);

            if(currentState == GameState.LOBBY)
            {
                var readyPanel = Instantiate(readyPrefab);
                readyPanels[conn.connectionId] = readyPanel.GetComponent<ReadyPanel>();

                // Spawn player-panel in Lobby ~ sometimes this doesn't spawn why?
                var playerPanel = Instantiate(playerPanelPrefab);
                Debug.Log("[SERVER]: spawning playerPanel: " + playerPanel);
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

                NetworkServer.Spawn(readyPanel, conn);
                NetworkServer.Spawn(playerPanel, conn);

                DetermineIfAllClientsAreReady();
            }
        }
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);

        if(SceneManager.GetActiveScene().path.Equals(offlineScene))
        {
            Debug.Log("Resetting client pan manager variables");
            ResetGame();
            currentState = GameState.LOBBY;
        }

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
                            playerPanels[nextClientTurnConnectionId].StartCountdown();
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

        if(avatarImageNames.ContainsKey(conn.connectionId))
        {
            avatarImageNames.Remove(conn.connectionId);
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
        drawPileCountText.text = message.cardsLeft.ToString();
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

            // Since Mirror bundles adding multiple objects in a SyncList together,
            // A message will need to be sent to the client indicating this is a special case
            // for when a player receives their initial hand
            npc.TargetPlayerReceivesInitialHand(npc.connectionToClient);
            npc.TargetOnClientPlayDealCardSounds(npc.connectionToClient, cards.Length);

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
            return dealer.GetCards(StartingCardCountPerPlayer);
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
        if(readyPanels.ContainsKey(gameLeaderIndex) && NetworkServer.connections.ContainsKey(gameLeaderIndex))
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
                //npc.TargetMoveRaisedCardsDown(NetworkServer.connections[endingTurnClientConnectionId]);
                
                if(pendingCardsByConnectionId.ContainsKey(endingTurnClientConnectionId) && 
                    pendingCardsByConnectionId[endingTurnClientConnectionId].Count > 0)
                {
                    Card[] pendingCards = pendingCardsByConnectionId[endingTurnClientConnectionId].ToArray();
                    npc.TargetMovePendingCardsBack(NetworkServer.connections[endingTurnClientConnectionId], pendingCards);
                }
                npc.TargetDisableControls(NetworkServer.connections[endingTurnClientConnectionId]);
                npc.CheckIfPlayerDrawsACard();
            }

            if(pendingCardsByConnectionId.ContainsKey(endingTurnClientConnectionId))
            {
                pendingCardsByConnectionId[endingTurnClientConnectionId].Clear();
            }
        }

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        int nextTurnClientConnectionId = turnOrder[currentTurnIndex];
        if(networkPlayerControllers.ContainsKey(nextTurnClientConnectionId))
        {
            Debug.Log("Next Turn connection id: " + nextTurnClientConnectionId);
            networkPlayerControllers[nextTurnClientConnectionId].TargetEnableControls(NetworkServer.connections[nextTurnClientConnectionId]);
            playerPanels[nextTurnClientConnectionId].StartCountdown();
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

        Debug.Log("starting game");

        AddActiveConnectionsToTurnOrderList();
        RandomizePlayerTurnOrders();
        AssignOpponentCardMats();
        AssignRandomAvatars();
        ConnectedPlayersReceiveCards();
        OnServerUpdateDrawPileCount();
    }

    private void AssignRandomAvatars()
    {
        var avatarFileNames = Utils.avatarAssets.Keys.ToArray();
        var connIds = playerPanels.Keys.ToArray();
        for(int i = 0;i < playerPanels.Count; ++i)
        {
            int randomIndex = UnityEngine.Random.Range(0, avatarFileNames.Length);
            if(avatarImageNames.ContainsValue(avatarFileNames[randomIndex]))
            {
                // only allow unique names
                --i;
                continue;
            }

            var playerPanel = playerPanels[connIds[i]];
            avatarImageNames[playerPanel.ConnectionId] = avatarFileNames[randomIndex];
            playerPanel.avatarName = avatarFileNames[randomIndex];
        }
    }

    public void AssignOpponentCardMats()
    {
        foreach (var npc in networkPlayerControllers.Values)
        {
            npc.RpcOnClientStartGame();
        }
    }

    public void CheckForTurn(int connectionId)
    {
        NetworkConnection playerConnection = NetworkServer.connections[connectionId];
        if (turnOrder[currentTurnIndex] == connectionId)
        {
            networkPlayerControllers[connectionId].TargetEnableControls(playerConnection);
            playerPanels[connectionId].StartCountdown();
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

    private void CheckForWinner(int clientConnectionId)
    {
        if(networkPlayerControllers.ContainsKey(clientConnectionId))
        {
            var networkPlayerController = networkPlayerControllers[clientConnectionId];
            if(networkPlayerController.myCards.Count == 0)
            {
                networkPlayerControllers[clientConnectionId].RpcEnablePlayAgainPanel(clientConnectionId, playerNames[clientConnectionId].name);
                networkPlayerControllers[gameLeaderIndex].TargetHostEnablePlayAgainButton(networkPlayerControllers[gameLeaderIndex].connectionToClient);
                currentState = GameState.GAME_END;
            }
        }
    }

    private void HandleWinState(int clientConnectionId)
    {
        // End Game
        if (currentState == GameState.GAME_END)
        {
            foreach (var kvp in NetworkServer.connections)
            {
                int connectionId = kvp.Key;
                playerPanels[connectionId].StopCountdown(connectionId);
                networkPlayerControllers[connectionId].TargetDisableControls(kvp.Value);

                // Play winner/loser voiceover sound
                
                if (connectionId == clientConnectionId)
                {
                    networkPlayerControllers[connectionId].TargetOnClientPlayGameOverSound(networkPlayerControllers[connectionId].connectionToClient, "winner");
                }
                else
                {
                    networkPlayerControllers[connectionId].TargetOnClientPlayGameOverSound(networkPlayerControllers[connectionId].connectionToClient, "loser");
                }
            }

        }
    }

    #region Methods used to evaluate if Card or Cards can be placed on Face Up Pile
    public void CheckPendingPile(int clientConnectionId)
    {
        if (pendingCardsByConnectionId.ContainsKey(clientConnectionId))
        {
            Debug.Log("Check PENDING PILE");
            List<Card> pendingCards = pendingCardsByConnectionId[clientConnectionId];
            bool isMoveValid = false;
            if(pendingCards.Count == 1)
            {
                if(dealer.TopCard != null)
                {
                    Debug.Log("Dealer TopCard: " + dealer.TopCard);
                }
                Debug.Log("Pending Card: " + pendingCards[0]);
                isMoveValid = TryAddCardToFaceUpPile(clientConnectionId, pendingCards[0]);
                Debug.Log("isMoveValid? " + isMoveValid);
            }
            else if(pendingCards.Count > 1)
            {
                isMoveValid = TryAddCardsToFaceUpPile(clientConnectionId, pendingCards.ToArray());
            }

            if(!isMoveValid)
            {
                GoToNextTurn();
            }

            pendingCardsByConnectionId[clientConnectionId].Clear();
        }
        else
        {
            Debug.Log("PENDING CARDS list not found");
            GoToNextTurn();
        }
    }

    public void AddCardToPendingPile(int clientConnectionId, Card card)
    {
        Debug.Log("Adding card to pending pile");

        NetworkPlayerController npc;
        networkPlayerControllers.TryGetValue(clientConnectionId, out npc);

        PlayerPanel playerPanel;
        playerPanels.TryGetValue(clientConnectionId, out playerPanel);

        // Don't Evaluate if it's not the player's turn
        if(playerPanel == null || npc == null || !playerPanel.IsMyTurn)
        {
            npc.TargetCardPlacementFailed(npc.connectionToClient, card);

            if(pendingCardsByConnectionId.ContainsKey(clientConnectionId))
            {
                pendingCardsByConnectionId[clientConnectionId].Clear();
            }

            return;
        }

        if(npc != null)
        {
            if ((IsFaceUpPileEmpty || card.Value >= dealer.TopCard?.Value) &&
                (!pendingCardsByConnectionId.ContainsKey(clientConnectionId) || 
                pendingCardsByConnectionId[clientConnectionId].Count == 0))
            {
                Debug.Log("TryAddCardToFaceUpPile called");
                TryAddCardToFaceUpPile(clientConnectionId, card);
                return;
            }

            if (!pendingCardsByConnectionId.ContainsKey(clientConnectionId))
            {
                pendingCardsByConnectionId[clientConnectionId] = new List<Card>();
            }

            pendingCardsByConnectionId[clientConnectionId].Add(card);

            Debug.Log("Add card to pending list: " + card);

            // Combo
            Card[] pendingCards = pendingCardsByConnectionId[clientConnectionId].ToArray();
            int pendingCardsCount = pendingCardsByConnectionId[clientConnectionId].Count;
            if(pendingCardsCount > 1)
            {
                Debug.Log("Check For Combo");

                // if first and last card don't match by suit, fail validation
                SuitType firstCardSuit = pendingCardsByConnectionId[clientConnectionId][0].suit;
                SuitType lastCardSuit = pendingCardsByConnectionId[clientConnectionId][pendingCardsCount - 1].suit;
                if(firstCardSuit != lastCardSuit)
                {
                    npc.TargetOnComboInvalid(npc.connectionToClient, pendingCards);
                    pendingCardsByConnectionId[clientConnectionId].Clear();
                    return;
                }

                int totalCardValue = pendingCardsByConnectionId[clientConnectionId].Select(c => c.Value).Sum();
                if(totalCardValue > dealer.TopCard?.Value)
                {
                    npc.TargetOnComboInvalid(npc.connectionToClient, pendingCards);
                    pendingCardsByConnectionId[clientConnectionId].Clear();
                    return;
                }

                if(totalCardValue == dealer.TopCard?.Value)
                {
                    bool isComboValid = TryAddCardsToFaceUpPile(clientConnectionId, 
                        pendingCardsByConnectionId[clientConnectionId].ToArray());

                    Debug.Log("Is ComboValid? " + isComboValid);
                    pendingCardsByConnectionId[clientConnectionId].Clear();
                }
            }           
        }
    }

    public bool TryAddCardToFaceUpPile(int clientConnectionId, Card card)
    {
        if (!networkPlayerControllers.ContainsKey(clientConnectionId) ||
            !playerPanels.ContainsKey(clientConnectionId))
        {
            Debug.LogErrorFormat("[Server]: TryAddCardToFaceUpPile clientConnectionId {0} does not exist on server.", clientConnectionId);
            return false;
        }

        NetworkConnectionToClient conn = NetworkServer.connections[clientConnectionId];
        var npc = networkPlayerControllers[clientConnectionId];
        var playerPanel = playerPanels[clientConnectionId];

        // Don't validate card if it isn't the player's turn
        //if (!playerPanel.IsMyTurn)
        //{
        //    npc.TargetCardPlacementFailed(conn, card);
        //    return false;
        //}

        if (GameRules.ValidateCard(dealer.TopCard, card))
        {
            bool canAddCard = dealer.AddCardToFaceUpPile2(card);
            if (canAddCard)
            {
                // Debug.Log("[SERVER]: CARD CAN BE PLAYED THIS TURN!!!!!! " + card);
                npc.hasPlayedCardOrComboThisTurn = true;
                npc.myCards.Remove(card);
                npc.RpcRemoveOpponentCardFromHand(card, animIndex);
                npc.TargetRemoveMyCardFromHand(conn, card, animIndex);
                animIndex = (animIndex + 1) % ANIM_VARIATION_COUNT;

                CheckGameStatus(clientConnectionId);
                return true;
            }
        }

        // Put card down on failed validation
        npc.TargetCardPlacementFailed(conn, card);

        return false;
    }

    public bool TryAddCardsToFaceUpPile(int clientConnectionId, Card[] cards)
    {
        NetworkPlayerController networkPlayerController;
        networkPlayerControllers.TryGetValue(clientConnectionId, out networkPlayerController);

        bool isComboValid = dealer.TopCard != null && GameRules.DoCardsAddUpToTopCardValue((Card)dealer.TopCard, cards);
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

                CheckGameStatus(clientConnectionId);
            }
        }
        else
        {
            if(NetworkServer.connections.TryGetValue(clientConnectionId, out NetworkConnectionToClient clientConnection))
            {
                // Logging
                Debug.Log("[Server]: The following cards cannot be combo-ed together for player " + clientConnectionId);
                Debug.Log("[Server]: Card Length: " + cards.Length);
                foreach (var card in cards)
                {
                    Debug.Log(card);
                }

                networkPlayerController?.TargetOnComboInvalid(clientConnection, cards);
            }
        }

        return isComboValid;
    }
    #endregion

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
            if (isHeadless && SceneManager.GetActiveScene().path.Equals(offlineScene))
            {
                Debug.LogFormat("[Server]: Changing Scenes to: {0}", onlineScene);
                ServerChangeScene(onlineScene);
            }
        }
    }

    public void InitServerPorts()
    {
        serverConfigs = ServerConfigs.GenerateConfigs();
        var tcpTransport = GetComponent<TelepathyTransport>();
        var websocketTransport = GetComponent<WebsocketTransport>();
        tcpTransport.port = serverConfigs.tcpPort;
        websocketTransport.port = serverConfigs.websocketPort;
    }

    public void MoveCardsDown(int connectionId)
    {
        if(networkPlayerControllers.ContainsKey(connectionId))
        {
            var clientConnection = networkPlayerControllers[connectionId].connectionToClient;
            networkPlayerControllers[connectionId].TargetMoveCardsDown(clientConnection);
        }    
    }
}
