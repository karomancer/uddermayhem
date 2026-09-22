using UnityEngine;

/// <summary>
/// The barista's reaction bubble in the top-left corner. When the running score locks in a grade band
/// (Good at goodThreshold, Superb at superbThreshold of the chart's ceiling) it plays three frames over one
/// measure: frame 0 for a beat, frame 1 for a quarter beat, frame 2 for two and three quarters, then gone.
/// Pinned to the camera's top-left corner in world units so it sits the same at any aspect ratio.
/// </summary>
public class BaristaReaction : MonoBehaviour
{
    [Header("Frames: 0 standing, 1 transition, 2 thumbs up")]
    public Sprite[] goodFrames;
    public Sprite[] superbFrames;

    [Header("Timing (beats from the first frame)")]
    public float secondFrameBeat = 1f;
    public float thirdFrameBeat = 1.25f;
    [Tooltip("How long the last frame stays up")]
    public float holdBeats = 2.75f;

    [Header("Placement")]
    [Tooltip("Bubble centre from the camera's top-left corner, in world units (+x right, -y down)")]
    public Vector2 cornerOffset = new Vector2(2.5f, -2.5f);
    [Tooltip("Diameter of the speech bubble on screen, in world units")]
    public float bubbleWorldWidth = 4.1f;
    [Tooltip("Diameter of the speech bubble in the art, in pixels")]
    public float bubblePixels = 1272f;

    [Header("Sorting")]
    public string sortingLayerName = "UI";
    public int sortingOrder = 20;

    public int CurrentFrame => playing == null ? -1 : frameIndex;
    public string CurrentReaction => playing == null ? "" : playing == superbFrames ? "superb" : "good";

    private SpriteRenderer spriteRenderer;
    private GameManager gameManager;
    private int maxScore = -1;
    private Sprite[] playing;
    private float startBeat;
    private int frameIndex = -1;
    private bool goodShown;
    private bool superbShown;

    void Awake()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.enabled = false;
    }

    void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        Sprite reference = goodFrames != null && goodFrames.Length > 0 ? goodFrames[0] : null;
        if (reference != null)
        {
            float scale = bubbleWorldWidth / (bubblePixels / reference.pixelsPerUnit);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
        float depth = transform.position.z - camera.transform.position.z;
        Vector3 corner = camera.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
        transform.position = new Vector3(corner.x + cornerOffset.x, corner.y + cornerOffset.y, transform.position.z);

        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null) return;
        if (maxScore < 0) maxScore = gameManager.MaxScoreForChart();
        float beat = gameManager.songPositionInBeats;
        if (beat <= 0f || maxScore <= 0) return;

        if (playing == null)
        {
            int score = gameManager.CurrentScore;
            if (!superbShown && Usable(superbFrames) && score >= gameManager.superbThreshold * maxScore) Begin(superbFrames, beat);
            else if (!goodShown && Usable(goodFrames) && score >= gameManager.goodThreshold * maxScore) Begin(goodFrames, beat);
        }
        if (playing == null) return;

        float t = beat - startBeat;
        if (t < 0f) { Show(-1); return; }
        if (t >= thirdFrameBeat + holdBeats) { playing = null; Show(-1); return; }
        Show(t < secondFrameBeat ? 0 : t < thirdFrameBeat ? 1 : 2);
    }

    private static bool Usable(Sprite[] frames) => frames != null && frames.Length >= 3 && frames[0] != null && frames[1] != null && frames[2] != null;

    // Superb also retires the Good reaction, so a fast run doesn't play two bubbles back to back
    private void Begin(Sprite[] frames, float beat)
    {
        playing = frames;
        startBeat = Mathf.Ceil(beat);
        goodShown = true;
        if (frames == superbFrames) superbShown = true;
        Debug.Log($"[Reaction] {(frames == superbFrames ? "superb" : "good")} locked in at beat {beat:F2}, first frame on beat {startBeat}");
    }

    private void Show(int index)
    {
        frameIndex = index;
        spriteRenderer.enabled = index >= 0;
        if (index >= 0) spriteRenderer.sprite = playing[index];
    }
}
