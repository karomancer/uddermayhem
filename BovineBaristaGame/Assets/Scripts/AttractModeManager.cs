using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AttractModeManager : MonoBehaviour
{
    public static AttractModeManager Instance { get; private set; }

    // State tracking
    public static bool IsAttractModeActive { get; private set; }

    public enum AttractPhase
    {
        Inactive,
        LeaderboardEasy,
        LeaderboardMedium,
        Tutorial
    }
    public static AttractPhase CurrentPhase { get; private set; }

    [Header("Timing")]
    public float titleIdleTimeout = 60f;      // Seconds of idle on title before attract mode starts
    public float leaderboardDisplayTime = 10f;
    public float tutorialFadeOutTime = 25f;

    [Header("Scene Names")]
    public string endScreenSceneName = "EndScreen";
    public string mainSceneName = "Main";
    public string titleSceneName = "Title";

    [Header("Tutorial Config")]
    public LevelConfig tutorialLevelConfig;

    [Header("Overlay")]
    public string overlayMessage = "Tap anywhere to start";
    public TMP_FontAsset overlayFont;  // Assign your custom font here
    public int overlayFontSize = 36;
    public Color overlayTextColor1 = Color.white;
    public Color overlayTextColor2 = Color.yellow;
    public float colorChangeInterval = 1f;
    public Color overlayOutlineColor = Color.black;
    public float overlayOutlineWidth = 0.2f;

    // Audio state
    private float originalAudioVolume;

    // Overlay UI
    private GameObject overlayCanvas;
    private TMP_Text overlayText;
    private float colorTimer = 0f;
    private bool useFirstColor = true;

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create the overlay UI (hidden by default)
        CreateOverlay();

        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Show overlay on Title screen or during attract mode
        if (scene.name == titleSceneName || IsAttractModeActive)
        {
            if (overlayCanvas != null)
            {
                overlayCanvas.SetActive(true);
            }
        }
        else
        {
            // Hide overlay on other screens when not in attract mode
            if (overlayCanvas != null && !IsAttractModeActive)
            {
                overlayCanvas.SetActive(false);
            }
        }
    }

    private void CreateOverlay()
    {
        // Create Canvas
        overlayCanvas = new GameObject("AttractModeOverlay");
        overlayCanvas.transform.SetParent(transform);
        Canvas canvas = overlayCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;  // Always on top
        overlayCanvas.AddComponent<CanvasScaler>();
        overlayCanvas.AddComponent<GraphicRaycaster>();

        // Create text at bottom of screen
        GameObject textObj = new GameObject("OverlayText");
        textObj.transform.SetParent(overlayCanvas.transform);

        overlayText = textObj.AddComponent<TextMeshProUGUI>();
        overlayText.text = overlayMessage;
        overlayText.fontSize = overlayFontSize;
        overlayText.alignment = TextAlignmentOptions.Center;
        overlayText.color = overlayTextColor1;

        // Apply custom font if assigned
        if (overlayFont != null)
        {
            overlayText.font = overlayFont;
        }

        // Add outline for visibility
        overlayText.outlineWidth = overlayOutlineWidth;
        overlayText.outlineColor = overlayOutlineColor;

        // Position at bottom center
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = new Vector2(0, 30);
        rectTransform.sizeDelta = new Vector2(0, 60);

        // Hide initially
        overlayCanvas.SetActive(false);
    }

    void Update()
    {
        // Handle attract mode input
        if (IsAttractModeActive)
        {
            if (DetectAnyInput())
            {
                EndAttractMode();
                return;
            }
        }

        // Alternate overlay text color (when overlay is visible)
        if (overlayText != null && overlayCanvas != null && overlayCanvas.activeSelf)
        {
            colorTimer += Time.deltaTime;
            if (colorTimer >= colorChangeInterval)
            {
                colorTimer = 0f;
                useFirstColor = !useFirstColor;
                overlayText.color = useFirstColor ? overlayTextColor1 : overlayTextColor2;
            }
        }
    }

    public void StartAttractMode()
    {
        Debug.Log("AttractModeManager: Starting attract mode");
        IsAttractModeActive = true;
        CurrentPhase = AttractPhase.LeaderboardEasy;

        // Mute all audio
        originalAudioVolume = AudioListener.volume;
        AudioListener.volume = 0f;

        // Show overlay
        if (overlayCanvas != null)
        {
            overlayCanvas.SetActive(true);
        }

        // Set up EndScreenManager flags for Easy leaderboard
        EndScreenManager.isAttractMode = true;
        EndScreenManager.attractModeDifficulty = "Easy";

        // Transition to EndScreen
        SceneManager.LoadScene(endScreenSceneName);
    }

    public void AdvanceToNextPhase()
    {
        Debug.Log($"AttractModeManager: Advancing from {CurrentPhase}");

        switch (CurrentPhase)
        {
            case AttractPhase.LeaderboardEasy:
                CurrentPhase = AttractPhase.LeaderboardMedium;
                EndScreenManager.attractModeDifficulty = "Medium";
                SceneManager.LoadScene(endScreenSceneName);
                break;

            case AttractPhase.LeaderboardMedium:
                CurrentPhase = AttractPhase.Tutorial;
                EndScreenManager.isAttractMode = false;

                // Set up for tutorial level
                if (tutorialLevelConfig != null)
                {
                    GameManager.currentLevelConfig = tutorialLevelConfig;
                }
                else
                {
                    Debug.LogWarning("AttractModeManager: No tutorial config assigned!");
                }
                SceneManager.LoadScene(mainSceneName);
                break;

            case AttractPhase.Tutorial:
                EndAttractMode();
                break;

            default:
                EndAttractMode();
                break;
        }
    }

    public void EndAttractMode()
    {
        Debug.Log("AttractModeManager: Ending attract mode");

        // Restore audio
        AudioListener.volume = originalAudioVolume;

        // Hide overlay
        if (overlayCanvas != null)
        {
            overlayCanvas.SetActive(false);
        }

        // Reset all flags
        IsAttractModeActive = false;
        CurrentPhase = AttractPhase.Inactive;
        EndScreenManager.isAttractMode = false;

        // Return to title
        SceneManager.LoadScene(titleSceneName);
    }

    private bool DetectAnyInput()
    {
        // Check keyboard (W/Q/S/A specifically, plus any key)
        bool keyPressed = Input.GetKeyDown(KeyCode.W) ||
                          Input.GetKeyDown(KeyCode.Q) ||
                          Input.GetKeyDown(KeyCode.S) ||
                          Input.GetKeyDown(KeyCode.A) ||
                          Input.anyKeyDown;

        // Check touch
        bool touchBegan = Input.touchCount > 0 &&
                          Input.GetTouch(0).phase == TouchPhase.Began;

        // Check mouse
        bool mouseClicked = Input.GetMouseButtonDown(0);

        return keyPressed || touchBegan || mouseClicked;
    }
}
