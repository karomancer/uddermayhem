using UnityEngine;

/// <summary>
/// The tip jar: back wall, (money, later), front wall, with the bucket handle split into a back half
/// behind everything and a front half in front. Each handle sprite's pivot is its hook, so
/// handleAngle swings both halves about where they hang from the jar.
/// The jar is pinned to the camera's bottom-left corner in world units, and the tips number is
/// pinned to the jar, so the corner looks the same at every screen size and aspect ratio.
/// It stays below the screen through the tutorial and slides up once the score is shown.
/// </summary>
public class TipJar : MonoBehaviour
{
    [Header("Layers (same artboard)")]
    public Sprite jarBack;
    public Sprite jarFront;
    public Sprite handleBack;
    public Sprite handleFront;

    [Header("Handle")]
    [Tooltip("Degrees to swing the handle about its hooks; 0 = as drawn")]
    public float handleAngle = 0f;

    [Header("Placement")]
    [Tooltip("Jar (artboard) centre measured from the camera's bottom-left corner, in world units; the screen is always 2 x orthographic size tall")]
    public Vector2 cornerOffset = new Vector2(2.44f, 0.67f);
    [Tooltip("How wide the jar art is on screen, in world units")]
    public float worldWidth = 6.55f;

    [Header("Tips number")]
    [Tooltip("Move the score text so it stays beside the TIPS plate at any resolution")]
    public bool placeScoreText = true;
    [Tooltip("Bottom-left corner of the score text's rect, measured from the jar centre in world units")]
    public Vector2 scoreTextOffset = new Vector2(-0.06f, -0.72f);

    [Header("Entrance")]
    [Tooltip("The jar waits below the screen until the score is shown (after the tutorial), then slides up over this many beats")]
    public float slideInBeats = 1f;

    [Header("Sorting")]
    public string sortingLayerName = "UI";
    public int sortingOrder = 0;

    private SpriteRenderer handleBackRenderer;
    private SpriteRenderer handleFrontRenderer;
    private GameManager gameManager;
    private RectTransform scoreRect;
    private Canvas scoreCanvas;
    private readonly Vector3[] corners = new Vector3[4];
    private float entranceStartTime = -1f;

    void Awake()
    {
        Layer("HandleBack", handleBack, 0, out handleBackRenderer);
        Layer("JarBack", jarBack, 1, out _);
        Layer("JarFront", jarFront, 3, out _);
        Layer("HandleFront", handleFront, 4, out handleFrontRenderer);
    }

    void Update()
    {
        Quaternion swing = Quaternion.Euler(0f, 0f, handleAngle);
        if (handleBackRenderer != null) handleBackRenderer.transform.localRotation = swing;
        if (handleFrontRenderer != null) handleFrontRenderer.transform.localRotation = swing;
    }

    void LateUpdate()
    {
        if (jarFront == null) return;

        float artWidth = jarFront.rect.width / jarFront.pixelsPerUnit;
        float scale = worldWidth / artWidth;
        transform.localScale = new Vector3(scale, scale, 1f);

        Camera camera = Camera.main;
        if (camera == null) return;
        float depth = transform.position.z - camera.transform.position.z;
        Vector3 corner = camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));

        // Hidden = the whole artboard just below the bottom edge; the score text rides along since it is placed off the jar
        float artHeight = jarFront.rect.height / jarFront.pixelsPerUnit * scale;
        float hiddenY = corner.y - artHeight * 0.5f - 0.1f;
        float shownY = corner.y + cornerOffset.y;
        float y = Mathf.Lerp(hiddenY, shownY, EaseOutCubic(EntranceProgress()));
        transform.position = new Vector3(corner.x + cornerOffset.x, y, transform.position.z);

        if (placeScoreText) PlaceScoreText(camera);
    }

    private float EntranceProgress()
    {
        if (entranceStartTime < 0f)
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null || !gameManager.ScoreVisible) return 0f;
            entranceStartTime = Time.time;
        }
        float duration = slideInBeats * CupConductor.SecPerBeat;
        return duration <= 0f ? 1f : Mathf.Clamp01((Time.time - entranceStartTime) / duration);
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    private void PlaceScoreText(Camera camera)
    {
        if (scoreRect == null)
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null || gameManager.ScoreText == null) return;
            scoreRect = gameManager.ScoreText.rectTransform;
            scoreCanvas = scoreRect.GetComponentInParent<Canvas>();
        }

        // Jar-relative world point -> screen -> the canvas's own space, then shift the rect so its bottom-left lands there
        Vector3 target = transform.position + (Vector3)scoreTextOffset;
        Vector2 screen = camera.WorldToScreenPoint(target);
        Camera uiCamera = scoreCanvas != null && scoreCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? scoreCanvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(scoreRect, screen, uiCamera, out Vector3 desired)) return;
        scoreRect.GetWorldCorners(corners);
        scoreRect.position += desired - corners[0];
    }

    // Every sprite shares the artboard, so a child sits at its own pivot's artboard offset and the art lines up
    private void Layer(string name, Sprite sprite, int order, out SpriteRenderer renderer)
    {
        renderer = null;
        if (sprite == null) return;

        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        Vector2 pivotOffset = (sprite.pivot - sprite.rect.size * 0.5f) / sprite.pixelsPerUnit;
        child.transform.localPosition = pivotOffset;

        renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder + order;
    }
}
