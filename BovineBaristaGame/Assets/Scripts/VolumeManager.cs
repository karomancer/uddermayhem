using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance { get; private set; }

    [Header("UI Settings")]
    public string buttonIcon = "♪";
    public int buttonSize = 50;
    public int buttonPadding = 15;
    public int panelWidth = 500;
    public int panelHeight = 80;

    [Header("Colors")]
    public Color buttonBackgroundColor = new Color(0, 0, 0, 0.5f);
    public Color buttonTextColor = Color.white;
    public Color panelBackgroundColor = new Color(0, 0, 0, 0.8f);
    public Color handleColor = new Color(0, 0, 0, 0.8f);


    [Header("Font")]
    public TMP_FontAsset font;

    [Header("Scenes to Hide On")]
    public string[] hideOnScenes;

    // Private UI references
    private GameObject volumeCanvas;
    private GameObject volumeButton;
    private GameObject volumePanel;
    private GameObject backgroundBlocker;
    private Slider volumeSlider;

    // State
    private float savedVolume = 1f;
    private bool panelOpen = false;

    private const string VOLUME_PREFS_KEY = "MasterVolume";

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

        // Load saved volume
        savedVolume = PlayerPrefs.GetFloat(VOLUME_PREFS_KEY, 1f);
        AudioListener.volume = savedVolume;

        // Create UI
        CreateUI();

        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if we should hide on this scene
        bool shouldHide = false;

        // Hide during attract mode
        if (AttractModeManager.IsAttractModeActive)
        {
            shouldHide = true;
        }

        // Check scene-specific hiding
        if (!shouldHide && hideOnScenes != null)
        {
            foreach (string sceneName in hideOnScenes)
            {
                if (scene.name == sceneName)
                {
                    shouldHide = true;
                    break;
                }
            }
        }

        if (volumeCanvas != null)
        {
            volumeCanvas.SetActive(!shouldHide);
        }

        // Close panel on scene change
        if (panelOpen)
        {
            ClosePanel();
        }
    }

    private void CreateUI()
    {
        // Create Canvas
        volumeCanvas = new GameObject("VolumeCanvas");
        volumeCanvas.transform.SetParent(transform);
        Canvas canvas = volumeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;  // Below attract mode overlay (999)

        CanvasScaler scaler = volumeCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        volumeCanvas.AddComponent<GraphicRaycaster>();

        // Create background blocker (for tap-outside-to-close)
        CreateBackgroundBlocker();

        // Create volume button
        CreateVolumeButton();

        // Create volume panel (hidden by default)
        CreateVolumePanel();
    }

    private void CreateBackgroundBlocker()
    {
        backgroundBlocker = new GameObject("BackgroundBlocker");
        backgroundBlocker.transform.SetParent(volumeCanvas.transform);

        Image blockerImage = backgroundBlocker.AddComponent<Image>();
        blockerImage.color = new Color(0, 0, 0, 0);  // Fully transparent

        Button blockerButton = backgroundBlocker.AddComponent<Button>();
        blockerButton.onClick.AddListener(ClosePanel);

        // Make it full screen
        RectTransform rt = backgroundBlocker.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        backgroundBlocker.SetActive(false);
    }

    private void CreateVolumeButton()
    {
        volumeButton = new GameObject("VolumeButton");
        volumeButton.transform.SetParent(volumeCanvas.transform);

        // Add background image
        Image buttonBg = volumeButton.AddComponent<Image>();
        buttonBg.color = buttonBackgroundColor;

        // Add button component
        Button button = volumeButton.AddComponent<Button>();
        button.onClick.AddListener(TogglePanel);

        // Position in top-right corner
        RectTransform rt = volumeButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-buttonPadding, -buttonPadding);
        rt.sizeDelta = new Vector2(buttonSize, buttonSize);

        // Add icon text
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(volumeButton.transform);

        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = buttonIcon;
        iconText.fontSize = buttonSize * 0.6f;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = buttonTextColor;

        if (font != null)
        {
            iconText.font = font;
        }

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
    }

    private void CreateVolumePanel()
    {
        volumePanel = new GameObject("VolumePanel");
        volumePanel.transform.SetParent(volumeCanvas.transform);

        // Add background image with rounded corners
        Image panelBg = volumePanel.AddComponent<Image>();
        panelBg.color = panelBackgroundColor;
        panelBg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        panelBg.type = Image.Type.Sliced;

        // Position below the button
        RectTransform rt = volumePanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-buttonPadding, -buttonPadding - buttonSize - 10);
        rt.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Create slider
        CreateVolumeSlider();

        volumePanel.SetActive(false);
    }

    private void CreateVolumeSlider()
    {
        // Slider container
        GameObject sliderObj = new GameObject("VolumeSlider");
        sliderObj.AddComponent<RectTransform>();
        sliderObj.transform.SetParent(volumePanel.transform, false);

        volumeSlider = sliderObj.AddComponent<Slider>();
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = savedVolume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 0.5f);
        sliderRt.anchorMax = new Vector2(1, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = Vector2.zero;
        sliderRt.sizeDelta = new Vector2(-40, 60);  // Larger touch area

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.AddComponent<RectTransform>();
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        bgImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bgImage.type = Image.Type.Sliced;

        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.3f);
        bgRt.anchorMax = new Vector2(1, 0.7f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Fill area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.AddComponent<RectTransform>();
        fillAreaObj.transform.SetParent(sliderObj.transform, false);

        RectTransform fillAreaRt = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0.3f);
        fillAreaRt.anchorMax = new Vector2(1, 0.7f);
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.AddComponent<RectTransform>();
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.7f, 0.3f, 1f);  // Green fill
        fillImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        fillImage.type = Image.Type.Sliced;

        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        volumeSlider.fillRect = fillRt;

        // Handle slide area
        GameObject handleAreaObj = new GameObject("Handle Slide Area");
        handleAreaObj.AddComponent<RectTransform>();
        handleAreaObj.transform.SetParent(sliderObj.transform, false);

        RectTransform handleAreaRt = handleAreaObj.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = Vector2.zero;
        handleAreaRt.offsetMax = Vector2.zero;

        // Handle - large for touch (minimum 44x44 recommended)
        GameObject handleObj = new GameObject("Handle");
        handleObj.AddComponent<RectTransform>();
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = handleColor;
        handleImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        handleImage.type = Image.Type.Simple;

        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(44, 50);

        volumeSlider.handleRect = handleRt;
        volumeSlider.targetGraphic = handleImage;
    }

    private void TogglePanel()
    {
        if (panelOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    private void OpenPanel()
    {
        panelOpen = true;
        volumePanel.SetActive(true);
        backgroundBlocker.SetActive(true);
    }

    private void ClosePanel()
    {
        panelOpen = false;
        volumePanel.SetActive(false);
        backgroundBlocker.SetActive(false);
    }

    private void OnVolumeChanged(float value)
    {
        savedVolume = value;

        // Only apply if not in attract mode
        if (!AttractModeManager.IsAttractModeActive)
        {
            AudioListener.volume = value;
        }

        // Save to PlayerPrefs
        PlayerPrefs.SetFloat(VOLUME_PREFS_KEY, value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Get the user's saved volume level (for use when restoring from attract mode)
    /// </summary>
    public float GetVolume()
    {
        return savedVolume;
    }

    /// <summary>
    /// Set volume programmatically
    /// </summary>
    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        savedVolume = value;

        if (volumeSlider != null)
        {
            volumeSlider.value = value;
        }

        if (!AttractModeManager.IsAttractModeActive)
        {
            AudioListener.volume = value;
        }

        PlayerPrefs.SetFloat(VOLUME_PREFS_KEY, value);
        PlayerPrefs.Save();
    }
}
