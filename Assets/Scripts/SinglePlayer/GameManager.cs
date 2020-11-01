using ProjectAce;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        GAME_LAUNCH,
        LOBBY,
        GAME_IN_PROGRESS,
        GAME_END
    };

    private static GameState currentState;

    [SerializeField]
    [Header("Server Config Settings")]
    private ServerConfigs serverConfigs;

    public int StartingCardCountPerPlayer => serverConfigs.startingCardCountPerPlayer;
    // measured in seconds
    public int InitialTimeLeftPerPlayer => serverConfigs.initialTimeLeftPerPlayer;

    private Dealer dealer;
    private SinglePlayerPanel playerPanel;
    private ClientSideController player;

    private Text drawPileCountText;

    private float[] rotations = new float[] { 0f, 15f, 30f, -15f, -30f };
    private int animIndex = 0;

    public static GameManager instance;

    private PlayAgainPanel playAgainPanel;
    private Button playAgainButton;

    public bool IsFaceUpPileEmpty => dealer.TopCard == null;
 
    [SerializeField]
    private List<Card> pendingCards = new List<Card>();

    [SerializeField]
    private Button startGameButton;
    [SerializeField]
    private Button exitGameButton;

    [SerializeField]
    private GameObject lobbyPanel;

    public FaceUpPile faceUpPile;

    private void Awake()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

#if UNITY_STANDALONE
        serverConfigs = ServerConfigs.GenerateConfigs();
#endif
        
        Init();

        // Only allow 1 instance of this game object to live
        if(instance != null && instance != this)
        {
            Destroy(instance);
        }

        instance = this;
        DontDestroyOnLoad(this);

    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

        playAgainButton.onClick.RemoveListener(OnGameReset);
        startGameButton.onClick.RemoveListener(OnGameStart);
        exitGameButton.onClick.RemoveListener(OnGameExit);
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if(scene.name.Equals("MainMenu"))
        {
            // Mark this singleton for destruction when not replaying game
            Destroy(gameObject);
            instance = null;
        }
    }

    private void Start()
    {
        player = FindObjectOfType<ClientSideController>();
        playerPanel = FindObjectOfType<SinglePlayerPanel>();
        dealer = FindObjectOfType<Dealer>();
        playAgainPanel = FindObjectOfType<PlayAgainPanel>();
        playAgainButton = playAgainPanel.transform.Find("OptionsPanel/PlayAgainButton")?.GetComponent<Button>();
        drawPileCountText = GameObject.Find("DrawPileCountLabel")?.GetComponent<Text>();

        playAgainPanel.gameObject.SetActive(false);
        playAgainButton.onClick.AddListener(OnGameReset);

        lobbyPanel.SetActive(true);
        startGameButton.onClick.AddListener(OnGameStart);
        exitGameButton.onClick.AddListener(OnGameExit);
    }

    private void OnGameReset()
    {
        if(currentState == GameState.GAME_END)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnGameStart()
    {
#if UNITY_WEBGL
        StartCoroutine(FetchJsonConfigs((configs) =>
        {
            Debug.Log("Data successfully loaded");
            serverConfigs = configs;
            StartGame();
        }));
#else
        StartGame();
#endif
    }

    private void OnGameExit()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Init()
    {
        Utils.LoadCardAssets();
        Utils.LoadAvatarAssets();
        currentState = GameState.GAME_LAUNCH;
        faceUpPile = FindObjectOfType<FaceUpPile>();
    }

    public void VerifyConfirmedSelection()
    {
        if (pendingCards.Count == 1)
        {
            TryAddCardToFaceUpPile(pendingCards[0]);
        }
        else if (pendingCards.Count > 1)
        {
            TryAddCardsToFaceUpPile(pendingCards.ToArray());
        }

        pendingCards.Clear();
    }

    public void CheckPendingPile()
    {
        if (pendingCards.Count > 0)
        {
            bool isMoveValid = false;
            if (pendingCards.Count == 1)
            {
                isMoveValid = TryAddCardToFaceUpPile(pendingCards[0]);
            }
            else if (pendingCards.Count > 1)
            {
                isMoveValid = TryAddCardsToFaceUpPile(pendingCards.ToArray());
            }

            if (!isMoveValid)
            {
                ResetTurn();
            }

            pendingCards.Clear();
        }
        else
        {
            ResetTurn();
        }

        player.ToggleConfirmButton(false);
    }

    public void AddCardToPendingPile(Card card)
    {
        if((IsFaceUpPileEmpty || card.Value >= dealer.TopCard?.Value) &&
            pendingCards.Count == 0)
        {
            bool isValidMove = TryAddCardToFaceUpPile(card);
            return;
        }

        pendingCards.Add(card);
        player.MoveToFaceUpPilePending(card, faceUpPile.transform);

        // Combo
        if(pendingCards.Count > 1)
        {
            Card[] pendingArray = pendingCards.ToArray();

            SuitType firstCardSuit = pendingCards[0].suit;
            SuitType lastCardSuit = pendingCards[pendingCards.Count - 1].suit;
            if(firstCardSuit != lastCardSuit)
            {
                player.OnComboInvalid(pendingArray);
                player.ToggleConfirmButton(false);
                pendingCards.Clear();
                return;
            }

            int totalCardValue = pendingCards.Select(p => p.Value).Sum();
            if(totalCardValue > dealer.TopCard?.Value)
            {
                player.OnComboInvalid(pendingArray);
                player.ToggleConfirmButton(false);
                pendingCards.Clear();
                return;
            }
            
            if(totalCardValue == dealer.TopCard?.Value)
            {
                TryAddCardsToFaceUpPile(pendingArray);
                player.ToggleConfirmButton(false);
                pendingCards.Clear();
            }
        }
    }

    public bool TryAddCardToFaceUpPile(Card card)
    {
        if(GameRules.ValidateCard(dealer.TopCard, card))
        {
            bool canAddCard = dealer.AddCardToFaceUpPile2(card);
            if (canAddCard)
            {
                player.hasPlayedCardOrComboThisTurn = true;
                player.RemoveCard(card, rotations[animIndex]);
                animIndex = (animIndex + 1) % rotations.Length;
                playerPanel.SetCardsLeft(player.myCards.Count);
                CheckGameStatus();

                return true;
            }
        }

        // If validation fails, move cards back to player hand
        player.CardPlacementFailed(card);
        return false;
    }

    public bool TryAddCardsToFaceUpPile(Card[] cards)
    {
        bool isComboValid = dealer.TopCard != null &&
            GameRules.DoCardsAddUpToTopCardValue((Card)dealer.TopCard, cards);

        if(isComboValid)
        {

            player.hasPlayedCardOrComboThisTurn = true;

            int[] animIndices = new int[cards.Length];
            for(int i = 0;i < animIndices.Length; ++i)
            {
                animIndices[i] = animIndex;
                animIndex = (animIndex + 1) % rotations.Length;
            }

            foreach(var card in cards)
            {
                dealer.AddCardToFaceUpPile2(card);
            }

            player.RemoveCards(cards, animIndices);
            playerPanel.SetCardsLeft(player.myCards.Count);

            CheckGameStatus();
        }
        else
        {
            player.OnComboInvalid(cards);
        }

        return isComboValid;
    }

    public void CheckGameStatus()
    {
        if (player.IsHandEmpty)
        {
            playerPanel.StopCountdown();
            player.DisableControls();
            player.PlayGameOverSound("winner");
            currentState = GameState.GAME_END;

            playAgainPanel.gameObject.SetActive(true);
            playAgainPanel.SetWinnerText(playerPanel.playerName);
            playAgainPanel.SetWinningAvatarSingle(playerPanel);
        }
        else
        {
            ResetTurn();
        }
    }

    public void StartGame()
    {
        lobbyPanel.SetActive(false);

        currentState = GameState.GAME_IN_PROGRESS;

        player.DisableControls();
        dealer.PrepareDeck();
        AssignRandomAvatar();
        ReceiveCards();

        drawPileCountText.text = dealer.DrawPileCount.ToString();
    }

    private void AssignRandomAvatar()
    {
        var avatarFileNames = Utils.avatarAssets.Keys.ToArray();
        int randomIndex = UnityEngine.Random.Range(0, avatarFileNames.Length);
        playerPanel.avatarName = avatarFileNames[randomIndex];
        playerPanel.SetAvatarImage(Utils.avatarAssets[playerPanel.avatarName]);
    }

    private void ReceiveCards()
    {
        var cards = dealer.GetCards(StartingCardCountPerPlayer);
        foreach(var card in cards)
        {
            player.myCards.Add(card);
        }

        player.OnCardsAdded(cards);
        playerPanel.SetCardsLeft(cards.Length);
    }

    public void ResetTurn()
    {
        playerPanel.StopCountdown();
        player.CheckIfPlayerDrawsACard();
        player.DisableControls();

        playerPanel.StartCountdown();
        player.EnableControls();
    }

    public void DealerGiveCardToPlayer()
    {
        if (dealer.DrawPileCount == 0)
        {
            Debug.Log("UpdateDrawPileCount... draw pile is empty... will transfer all cards but 1 from face up pile to draw pile");
            dealer.TransferCardsFromFaceUpPileToDrawPile();
        }

        var newCard = dealer.GiveCard2();
        if (newCard != null)
        {
            player.myCards.Add((Card)newCard);
            player.OnCardAdded((Card)newCard);
            playerPanel.SetCardsLeft(player.myCards.Count);
        }

        drawPileCountText.text = dealer.DrawPileCount.ToString();
    }

    public void StartTurn()
    {
        player.EnableControls();
        playerPanel.StartCountdown();
    }

    private IEnumerator FetchJsonConfigs(Action<ServerConfigs> onDataReceived)
    {
        string serverConfigsPath = $"{Application.streamingAssetsPath}/serverConfigs.json";
        using (var request = UnityWebRequest.Get(serverConfigsPath))
        {
            yield return request.SendWebRequest();


            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Error: {0}", request.error);
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
                var configs = JsonUtility.FromJson<ServerConfigs>(request.downloadHandler.text);
                onDataReceived(configs);
            }
        }
    }
}
