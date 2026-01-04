using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndScreenManager : MonoBehaviour
{
    private enum EndScreenState
    {
        BaristaMessage,      // Barista telling you your score
        InitialsEntry,       // Player entering initials
        LeaderboardDisplay   // Showing leaderboard
    }

    [Header("UI References")]
    public GameObject initialsEntryPanel;
    public InitialsEntryUI initialsEntry;
    public GameObject leaderboardPanel;
    public TMP_Text timerText;             // Optional: show countdown for initials entry

    [Header("Barista")]
    public BaristaController barista;

    [Header("Messages - Normal Score (use {tips} for amount)")]
    [TextArea(2, 3)]
    public string[] normalScoreMessages = new string[] {
        "Nice job!",
        "You earned {tips} in tips!",
        "Thanks for playing!"
    };

    [Header("Messages - High Score (use {tips} for amount)")]
    [TextArea(2, 3)]
    public string[] highScoreMessages = new string[] {
        "{tips} in tips?!",
        "You're a top earner!",
        "Enter your initials!"
    };

    [Header("Leaderboard Entries")]
    public LeaderboardEntryUI[] leaderboardEntries;  // 10 entries

    [Header("Transition")]
    public SceneTransitionManager transitionManager;
    public string returnSceneName = "Title";

    [Header("Timing")]
    public float messageDisplayTime = 10f;           // Non-high-score message duration
    public float highScoreMessageTime = 5f;          // High score celebration message duration
    public float initialsEntryTimeout = 30f;         // Time limit for entering initials
    public float leaderboardDisplayTime = 10f;       // How long to show leaderboard

    [Header("Colors")]
    public Color normalEntryColor = Color.white;
    public Color newScoreColor = Color.yellow;

    [Header("Attract Mode")]
    public static bool isAttractMode = false;
    public static string attractModeDifficulty = "Medium";
    public float attractModeAutoAdvanceTime = 10f;

    // State
    private EndScreenState currentState;
    private float stateTimer = float.MaxValue;  // Initialize high to prevent premature expiry
    private bool stateMachineStarted = false;   // Don't process timer until ready
    private int finalScore;
    private string difficulty;
    private bool isNewHighScore;
    private int newScoreRank;
    private bool initialsSubmitted = false;

    void Awake()
    {
        HideAllPanels();
    }

    void Start()
    {
        if (isAttractMode)
        {
            SetupAttractMode();
        }
        else
        {
            SetupPostGameMode();
        }
    }

    void Update()
    {
        if (isAttractMode)
        {
            HandleAttractModeInput();
            return;
        }

        // Don't process timer until state machine has started
        if (!stateMachineStarted)
        {
            return;
        }

        // Update timer
        stateTimer -= Time.deltaTime;

        // Update timer display if in initials entry
        if (currentState == EndScreenState.InitialsEntry && timerText != null)
        {
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, stateTimer)).ToString();
        }

        // Handle state transitions based on timer
        if (stateTimer <= 0)
        {
            OnStateTimerExpired();
        }
    }

    private void SetupAttractMode()
    {
        difficulty = attractModeDifficulty;
        isNewHighScore = false;

        // Hide everything except leaderboard
        HideAllPanels();

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
        }

        PopulateLeaderboard();
        Invoke("OnContinue", attractModeAutoAdvanceTime);
    }

    private void HandleAttractModeInput()
    {
        bool anyInput = Input.anyKeyDown ||
                       Input.GetMouseButtonDown(0) ||
                       (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (anyInput)
        {
            CancelInvoke("OnContinue");
            OnContinue();
        }
    }

    private void SetupPostGameMode()
    {
        // Get score and difficulty from GameManager
        finalScore = GameManager.FinalScore;
        difficulty = GameManager.currentDifficulty;

        // Check if high score
        if (HighScoreManager.Instance != null)
        {
            isNewHighScore = HighScoreManager.Instance.IsTopTen(difficulty, finalScore);
            newScoreRank = HighScoreManager.Instance.GetRank(difficulty, finalScore);
        }

        // Wait for scene transition to complete before starting barista
        if (transitionManager != null && transitionManager.playEnterTransition)
        {
            // Subscribe to event - will fire when transition completes
            transitionManager.OnEnterTransitionComplete += OnSceneReady;
        }
        else
        {
            // No transition - start immediately
            OnSceneReady();
        }
    }

    private void OnSceneReady()
    {
        // Unsubscribe if we were listening
        if (transitionManager != null)
        {
            transitionManager.OnEnterTransitionComplete -= OnSceneReady;
        }

        // Start with barista message
        EnterBaristaMessageState();
    }

    private void EnterBaristaMessageState()
    {
        stateMachineStarted = true;
        currentState = EndScreenState.BaristaMessage;
        HideAllPanels();

        double tips = finalScore / 100.0;
        string tipsFormatted = "$" + tips.ToString("F2");

        // Get the appropriate messages and replace {tips} placeholder
        string[] messages = isNewHighScore ? highScoreMessages : normalScoreMessages;
        string[] processedMessages = new string[messages.Length];
        for (int i = 0; i < messages.Length; i++)
        {
            processedMessages[i] = messages[i].Replace("{tips}", tipsFormatted);
        }

        // Have barista speak the messages
        if (barista != null)
        {
            barista.SetVisible(true);
            barista.SayMultiple(processedMessages, OnBaristaFinishedSpeaking);
        }
        else
        {
            // No barista - just proceed after a delay
            stateTimer = isNewHighScore ? highScoreMessageTime : messageDisplayTime;
        }

        // Set a long timer as backup (barista callback will trigger state change)
        stateTimer = 60f;
    }

    private void OnBaristaFinishedSpeaking()
    {
        // Barista finished - proceed to next state
        if (currentState == EndScreenState.BaristaMessage)
        {
            if (isNewHighScore)
            {
                EnterInitialsEntryState();
            }
            else
            {
                EnterLeaderboardState();
            }
        }
    }

    private void EnterInitialsEntryState()
    {
        currentState = EndScreenState.InitialsEntry;
        HideAllPanels();

        if (initialsEntryPanel != null)
        {
            initialsEntryPanel.SetActive(true);
        }

        // Hide the barista during initials entry
        if (barista != null)
        {
            barista.SetVisible(false);
        }

        if (initialsEntry != null)
        {
            initialsEntry.ResetToDefault();
            initialsEntry.OnInitialsSubmitted += OnInitialsSubmitted;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        stateTimer = initialsEntryTimeout;
        initialsSubmitted = false;
    }

    private void EnterLeaderboardState()
    {
        currentState = EndScreenState.LeaderboardDisplay;
        HideAllPanels();

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
        }

        PopulateLeaderboard();
        stateTimer = leaderboardDisplayTime;
    }

    private void OnStateTimerExpired()
    {
        switch (currentState)
        {
            case EndScreenState.BaristaMessage:
                // Backup timer - normally barista callback handles this
                if (isNewHighScore)
                {
                    EnterInitialsEntryState();
                }
                else
                {
                    EnterLeaderboardState();
                }
                break;

            case EndScreenState.InitialsEntry:
                // Time ran out - submit with default initials
                if (!initialsSubmitted)
                {
                    string initials = initialsEntry != null ? initialsEntry.GetInitials() : "AAA";
                    SaveScoreAndProceed(initials);
                }
                break;

            case EndScreenState.LeaderboardDisplay:
                OnContinue();
                break;
        }
    }

    private void OnInitialsSubmitted(string initials)
    {
        if (initialsSubmitted) return;
        initialsSubmitted = true;

        SaveScoreAndProceed(initials);
    }

    private void SaveScoreAndProceed(string initials)
    {
        // Save the score
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.SaveScore(difficulty, initials, finalScore);
        }

        // Unsubscribe from event
        if (initialsEntry != null)
        {
            initialsEntry.OnInitialsSubmitted -= OnInitialsSubmitted;
        }

        // Update rank for highlighting (it's now saved, so get actual position)
        if (HighScoreManager.Instance != null)
        {
            newScoreRank = GetScoreRankInList(difficulty, finalScore, initials);
        }

        EnterLeaderboardState();
    }

    private int GetScoreRankInList(string diff, int score, string initials)
    {
        var scores = HighScoreManager.Instance.GetScores(diff);
        for (int i = 0; i < scores.Count; i++)
        {
            if (scores[i].score == score && scores[i].initials == initials)
            {
                return i + 1;
            }
        }
        return 0;
    }

    public void OnSubmitButtonPressed()
    {
        if (currentState == EndScreenState.InitialsEntry && initialsEntry != null && !initialsSubmitted)
        {
            string initials = initialsEntry.GetInitials();
            OnInitialsSubmitted(initials);
        }
    }

    private void HideAllPanels()
    {
        if (initialsEntryPanel != null) initialsEntryPanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (barista != null) barista.ShutUp();
    }

    private void PopulateLeaderboard()
    {
        if (HighScoreManager.Instance == null || leaderboardEntries == null) return;

        List<HighScoreEntry> scores = HighScoreManager.Instance.GetScores(difficulty);

        for (int i = 0; i < leaderboardEntries.Length; i++)
        {
            if (leaderboardEntries[i] == null) continue;

            if (i < scores.Count)
            {
                double tips = scores[i].score / 100.0;

                // Highlight new score
                bool isThisNewScore = isNewHighScore && (i == newScoreRank - 1);
                Color color = isThisNewScore ? newScoreColor : normalEntryColor;
                leaderboardEntries[i].SetEntry(i + 1, scores[i].initials, tips, color);
            }
            else
            {
                leaderboardEntries[i].SetEmpty(i + 1, normalEntryColor);
            }
        }
    }

    public void OnContinue()
    {
        if (transitionManager != null)
        {
            transitionManager.TransitionToScene(returnSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(returnSceneName);
        }
    }

    void OnDestroy()
    {
        if (initialsEntry != null)
        {
            initialsEntry.OnInitialsSubmitted -= OnInitialsSubmitted;
        }
        if (transitionManager != null)
        {
            transitionManager.OnEnterTransitionComplete -= OnSceneReady;
        }
    }
}
