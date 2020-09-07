using UnityEngine;
using ProjectAce;
using UnityEngine.SceneManagement;
using System.Linq;
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

    private void Awake()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

        serverConfigs = ServerConfigs.GenerateConfigs();

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
        StartGame();
    }

    private void OnGameReset()
    {
        if(currentState == GameState.GAME_END)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void Init()
    {
        Utils.LoadCardAssets();
        Utils.LoadAvatarAssets();
        currentState = GameState.GAME_LAUNCH;
    }

    public void TryAddCardToFaceUpPile(Card card)
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

                return;
            }
        }

        // If validation fails, move cards back to player hand
        player.CardPlacementFailed(card);
    }

    public void EvaluateCardsToCombo(Card[] cards)
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

            player.RemoveCards(cards, animIndices);

            CheckGameStatus();
        }
        else
        {
            player.OnComboInvalid(cards);
        }
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
        currentState = GameState.GAME_IN_PROGRESS;
        Debug.Log("starting game");

        player.DisableControls();
        dealer.PrepareDeck();
        AssignRandomAvatar();
        ReceiveCards();

        drawPileCountText.text = dealer.DrawPileCount.ToString();
    }

    private void AssignRandomAvatar()
    {
        var avatarFileNames = Utils.avatarAssets.Keys.ToArray();
        int randomIndex = Random.Range(0, avatarFileNames.Length);
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
        player.DisableControls();
        player.CheckIfPlayerDrawsACard();
        player.MoveRaisedCardsDown(() =>
        {
            // Re-enable controls once all cards are put back in the player's hand
            player.EnableControls();
            playerPanel.StartCountdown();
        });
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
            drawPileCountText.text = dealer.DrawPileCount.ToString();
        }
    }

    public void MoveCardsDown()
    {
        player.MoveCardsDown();
    }

    public void StartTurn()
    {
        player.EnableControls();
        playerPanel.StartCountdown();
    }
}
