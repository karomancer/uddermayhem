using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

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
    public enum CoinFillMode { ScoreShare, GradeBands }

    [Header("Layers (same artboard)")]
    public Sprite jarBack;
    public Sprite jarFront;
    public Sprite handleBack;
    public Sprite handleFront;
    public Sprite coinBody;
    [Tooltip("Top of the pile: while the jar is dancing the frames alternate every beat; otherwise frame 0")]
    public Sprite[] coinTopFrames;

    [Header("Coins (need the coins bone written by Udder Mayhem > Rig Tip Jar)")]
    [Tooltip("ScoreShare: the pile height is the score as a share of the chart's ceiling. GradeBands: 1/3 when OK is locked in, 2/3 at Superb, full only for a perfect run")]
    public CoinFillMode coinFillMode = CoinFillMode.ScoreShare;
    [Tooltip("The pile rises in this many discrete steps (ScoreShare mode)")]
    public int coinSteps = 20;
    [Tooltip("Distance along the jar's axis from the pile's full position (its top at the jar's shoulder) down to fully hidden below the screen edge, in sprite units")]
    public float coinTravel = 6.75f;
    [Tooltip("How fast the pile moves to a new level, in levels per second")]
    public float coinRiseSpeed = 2f;

    [Header("Handle")]
    [Tooltip("Degrees to swing the handle about its hooks; 0 = as drawn")]
    public float handleAngle = 0f;

    [Header("Placement")]
    [Tooltip("Jar (artboard) centre measured from the camera's bottom-left corner, in world units; the screen is always 2 x orthographic size tall")]
    public Vector2 cornerOffset = new Vector2(2.1f, 0.52f);
    [Tooltip("How wide the jar art is on screen, in world units")]
    public float worldWidth = 5.29f;

    [Header("Tips number")]
    [Tooltip("Move the score text so it stays beside the TIPS plate at any resolution")]
    public bool placeScoreText = true;
    [Tooltip("Bottom-left corner of the score text's rect, measured from the jar centre in world units")]
    public Vector2 scoreTextOffset = new Vector2(-0.02f, -0.72f);

    [Header("Entrance")]
    [Tooltip("The jar waits below the screen until the score is shown (after the tutorial), then slides up over this many beats")]
    public float slideInBeats = 1f;

    [Header("Dance (needs the bones written by Udder Mayhem > Rig Tip Jar)")]
    [Tooltip("Squash on each beat by streak multiplier: index 0 = 1x, 1 = 2x, ... (fraction of the jar's height)")]
    public float[] bounceByMultiplier = { 0f, 0.05f, 0.11f, 0.2f };
    [Tooltip("How fast the bounce dies out within the beat (higher = settles sooner)")]
    public float bounceDecay = 4f;
    [Tooltip("Squash-stretch oscillations per beat")]
    public float bounceCycles = 1.5f;
    [Tooltip("Sideways widening of the whole jar per unit of squash")]
    public float widthPerSquash = 0.6f;
    [Tooltip("Extra belly bulge per unit of squash")]
    public float bellyPerSquash = 0.9f;
    [Tooltip("How quickly the bounce grows or dies when the multiplier changes, in amplitude per second")]
    public float amplitudeChangeRate = 0.6f;

    [Header("Sorting")]
    public string sortingLayerName = "UI";
    public int sortingOrder = 0;

    private SpriteRenderer jarBackRenderer;
    private SpriteRenderer jarFrontRenderer;
    private SpriteRenderer coinBodyRenderer;
    private SpriteRenderer coinTopRenderer;
    private Transform boneBase;
    private Transform boneBelly;
    private Transform coinRoot;
    private float amplitude;
    private bool dancing;
    private float coinLevel;

    public float CoinLevel => coinLevel;

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
        Layer("JarBack", jarBack, 1, out jarBackRenderer);
        Layer("CoinBody", coinBody, 2, out coinBodyRenderer);
        Layer("CoinTop", coinTopFrames != null && coinTopFrames.Length > 0 ? coinTopFrames[0] : null, 3, out coinTopRenderer);
        Layer("JarFront", jarFront, 4, out jarFrontRenderer);
        Layer("HandleFront", handleFront, 5, out handleFrontRenderer);
        BuildRig();
    }

    // One bone hierarchy under the jar, shared by the front and back skins. Bone data comes from the sprite
    // (written by the TipJarRig editor menu); the skins deform in their own local space, so the bones only
    // need to sit where the sprite says they are, in the jar's space.
    private void BuildRig()
    {
        if (jarFrontRenderer == null || jarFront == null) return;
        SpriteBone[] bones = jarFront.GetBones();
        if (bones == null || bones.Length == 0) return;

        var transforms = new Transform[bones.Length];
        Transform root = null;
        for (int i = 0; i < bones.Length; i++)
        {
            var bone = new GameObject(bones[i].name).transform;
            bone.SetParent(bones[i].parentId >= 0 ? transforms[bones[i].parentId] : transform, false);
            bone.localPosition = bones[i].position;
            bone.localRotation = bones[i].rotation;
            transforms[i] = bone;
            if (bones[i].parentId < 0 && root == null) root = bone;
            if (bones[i].name == "base") boneBase = bone;
            if (bones[i].name == "belly") boneBelly = bone;
        }

        Skin(jarFrontRenderer, root, transforms);
        if (jarBackRenderer != null && jarBack != null && jarBack.GetBindPoses().Length == bones.Length) Skin(jarBackRenderer, root, transforms);

        // The coin pile hangs off the base bone in the same pose, so it squashes with the jar and slides along its axis
        if (boneBase != null && coinBody != null && coinBody.GetBindPoses().Length == 1)
        {
            coinRoot = new GameObject("coins").transform;
            coinRoot.SetParent(boneBase, false);
            var coinBones = new[] { coinRoot };
            if (coinBodyRenderer != null) Skin(coinBodyRenderer, coinRoot, coinBones);
            if (coinTopRenderer != null && coinTopRenderer.sprite != null && coinTopRenderer.sprite.GetBindPoses().Length == 1) Skin(coinTopRenderer, coinRoot, coinBones);
            coinRoot.localPosition = new Vector3(-coinTravel, 0f, 0f);
        }
    }

    // SpriteSkin's bone setters are internal (the Skinning Editor normally fills them), so bind through reflection
    private static void Skin(SpriteRenderer renderer, Transform root, Transform[] bones)
    {
        SpriteSkin skin = renderer.gameObject.AddComponent<SpriteSkin>();
        typeof(SpriteSkin).GetProperty("rootBone").GetSetMethod(true).Invoke(skin, new object[] { root });
        typeof(SpriteSkin).GetProperty("boneTransforms").GetSetMethod(true).Invoke(skin, new object[] { bones });
    }

    void Update()
    {
        Quaternion swing = Quaternion.Euler(0f, 0f, handleAngle);
        if (handleBackRenderer != null) handleBackRenderer.transform.localRotation = swing;
        if (handleFrontRenderer != null) handleFrontRenderer.transform.localRotation = swing;
    }

    // Rubber-hose bounce on every beat: squash on the beat, overshoot into a stretch, settle before the next one.
    // The base bone's local x runs up the jar, so its x is height and its y is width; the belly bone adds the bulge.
    private void Dance()
    {
        if (boneBase == null) return;
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        float target = 0f;
        bool playing = gameManager != null && gameManager.songPositionInBeats > 0f;
        if (playing && bounceByMultiplier != null && bounceByMultiplier.Length > 0)
        {
            int tier = Mathf.Clamp(gameManager.CurrentMultiplier - 1, 0, bounceByMultiplier.Length - 1);
            target = bounceByMultiplier[tier];
        }
        dancing = target > 0f;
        // Ease between tiers so a lost streak deflates the jar over a few beats rather than freezing it mid-bounce
        amplitude = Mathf.MoveTowards(amplitude, target, amplitudeChangeRate * Time.deltaTime);
        float bounce = 0f;
        if (playing && amplitude > 0f)
        {
            float t = gameManager.songPositionInBeats - Mathf.Floor(gameManager.songPositionInBeats);
            bounce = amplitude * Mathf.Exp(-bounceDecay * t) * Mathf.Cos(2f * Mathf.PI * bounceCycles * t);
        }
        boneBase.localScale = new Vector3(1f - bounce, 1f + widthPerSquash * bounce, 1f);
        if (boneBelly != null) boneBelly.localScale = new Vector3(1f, 1f + bellyPerSquash * Mathf.Max(0f, bounce), 1f);
        FillCoins();
    }

    // Pile height along the base bone's axis: hidden below the screen edge at 0, the top at the rim at 1
    private void FillCoins()
    {
        if (coinRoot == null || gameManager == null) return;
        int maxScore = gameManager.MaxScoreForChart();
        float target = 0f;
        if (maxScore > 0)
        {
            int score = gameManager.CurrentScore;
            if (coinFillMode == CoinFillMode.ScoreShare)
            {
                float share = Mathf.Clamp01((float)score / maxScore);
                target = coinSteps > 0 ? Mathf.Floor(share * coinSteps) / coinSteps : share;
            }
            else
            {
                if (score >= maxScore) target = 1f;
                else if (score >= gameManager.superbThreshold * maxScore) target = 2f / 3f;
                else if (score >= gameManager.goodThreshold * maxScore) target = 1f / 3f;
            }
        }
        coinLevel = Mathf.MoveTowards(coinLevel, target, coinRiseSpeed * Time.deltaTime);
        coinRoot.localPosition = new Vector3(-(1f - coinLevel) * coinTravel, 0f, 0f);
        // The mound's stray coin tips sit well above its bulk, so an empty jar hides the pile outright
        bool anyCoins = coinLevel > 0.001f;
        if (coinBodyRenderer != null) coinBodyRenderer.enabled = anyCoins;
        if (coinTopRenderer != null) coinTopRenderer.enabled = anyCoins;

        // The coins jostle with the dance: while the jar bounces the top alternates frames every beat, otherwise it rests on frame 0
        if (coinTopRenderer != null && coinTopFrames != null && coinTopFrames.Length >= 2)
        {
            int index = dancing && gameManager.songPositionInBeats > 0f ? Mathf.FloorToInt(gameManager.songPositionInBeats) % 2 : 0;
            Sprite frame = coinTopFrames[index];
            if (frame != null && coinTopRenderer.sprite != frame) coinTopRenderer.sprite = frame;
        }
    }

    // LateUpdate so the bounce reads this frame's song position, after GameManager has advanced it
    void LateUpdate()
    {
        if (jarFront == null) return;
        Dance();

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
