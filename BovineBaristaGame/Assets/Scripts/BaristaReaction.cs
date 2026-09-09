using UnityEngine;

/// <summary>
/// The barista's reaction bubble in the top-left corner. When the running score locks in a grade band
/// (OK at goodThreshold, Superb at superbThreshold of the chart's ceiling) it plays a three-frame beat:
/// frame 0 on the next beat, frame 1 half a beat later, frame 2 on the beat after that, held, then gone.
/// Pinned to the camera's top-left corner in world units so it sits the same at any aspect ratio.
/// </summary>
public class BaristaReaction : MonoBehaviour
{
    [Header("Frames: 0 on the beat, 1 half a beat later, 2 on the next beat (held)")]
    public Sprite[] okFrames;
    public Sprite[] superbFrames;

    [Header("Timing (beats from the first frame)")]
    public float secondFrameBeat = 0.5f;
    public float thirdFrameBeat = 1f;
    [Tooltip("How long the last frame stays up")]
    public float holdBeats = 7f;

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

    private SpriteRenderer spriteRenderer;
    private GameManager gameManager;
    private int maxScore = -1;
    private Sprite[] playing;
    private float startBeat;
    private int frameIndex = -1;
    private bool okShown;
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
        Sprite reference = okFrames != null && okFrames.Length > 0 ? okFrames[0] : null;
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
            else if (!okShown && Usable(okFrames) && score >= gameManager.goodThreshold * maxScore) Begin(okFrames, beat);
        }
        if (playing == null) return;

        float t = beat - startBeat;
        if (t < 0f) { Show(-1); return; }
        if (t >= thirdFrameBeat + holdBeats) { playing = null; Show(-1); return; }
        Show(t < secondFrameBeat ? 0 : t < thirdFrameBeat ? 1 : 2);
    }

    private static bool Usable(Sprite[] frames) => frames != null && frames.Length >= 3 && frames[0] != null && frames[1] != null && frames[2] != null;

    // Superb also retires the OK reaction, so a fast run doesn't play two bubbles back to back
    private void Begin(Sprite[] frames, float beat)
    {
        playing = frames;
        startBeat = Mathf.Ceil(beat);
        okShown = true;
        if (frames == superbFrames) superbShown = true;
        Debug.Log($"[Reaction] {(frames == superbFrames ? "superb" : "ok")} locked in at beat {beat:F2}, first frame on beat {startBeat}");
    }

    private void Show(int index)
    {
        frameIndex = index;
        spriteRenderer.enabled = index >= 0;
        if (index >= 0) spriteRenderer.sprite = playing[index];
    }
}
