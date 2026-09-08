using UnityEngine;
using UnityEngine.InputSystem;

public class TeatController : MonoBehaviour
{
    [Header("Input Configuration")]
    public TeatPosition teatPosition;

    [Header("Legacy Input (optional fallback)")]
    public KeyCode keyPress;

    [Header("Milk")]
    [Tooltip("Distance from this teat's pivot (its top) down to the tip while squeezed, in sprite units")]
    public float tipHeight = 4.37f;

    public Vector3 TipWorldPosition => transform.TransformPoint(new Vector3(0f, -tipHeight, 0f));

    private MilkStream stream;
    private CupConductor conductor;
    private CoffeeController pouringInto;
    private float pourSurfaceY;

    public bool IsSqueezing => isSqueezing;
    public int SortingOrder => spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

    private static readonly TeatController[] byLane = new TeatController[4];

    public static TeatController ForLane(TeatPosition lane)
    {
        return byLane[(int)lane];
    }

    private bool isSqueezing = false;
    private SpriteRenderer spriteRenderer;

    // A held cup reports where its liquid surface is so the milk stops there
    public void SetPourTarget(CoffeeController cup, float surfaceWorldY)
    {
        pouringInto = cup;
        pourSurfaceY = surfaceWorldY;
    }

    public void ClearPourTarget(CoffeeController cup)
    {
        if (pouringInto == cup) pouringInto = null;
    }

    private Animator animator;

    private GameManager gameManager;

    private InputAction inputAction;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        byLane[(int)teatPosition] = this;
    }

    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        conductor = gameManager.GetComponent<CupConductor>();

        // Get the appropriate input action based on teat position
        SetupInputAction();
    }

    void SetupInputAction()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogWarning("InputManager not found, falling back to legacy input");
            return;
        }

        switch (teatPosition)
        {
            case TeatPosition.FrontLeft:
                inputAction = InputManager.Instance.SqueezeFrontLeft;
                break;
            case TeatPosition.FrontRight:
                inputAction = InputManager.Instance.SqueezeFrontRight;
                break;
            case TeatPosition.BackLeft:
                inputAction = InputManager.Instance.SqueezeBackLeft;
                break;
            case TeatPosition.BackRight:
                inputAction = InputManager.Instance.SqueezeBackRight;
                break;
        }

        // Subscribe to input events
        if (inputAction != null)
        {
            inputAction.started += OnSqueezeStarted;
            inputAction.canceled += OnSqueezeCanceled;
        }
    }

    void OnDestroy()
    {
        if (byLane[(int)teatPosition] == this) byLane[(int)teatPosition] = null;
        if (stream != null) Destroy(stream.gameObject);

        // Unsubscribe from events to prevent memory leaks
        if (inputAction != null)
        {
            inputAction.started -= OnSqueezeStarted;
            inputAction.canceled -= OnSqueezeCanceled;
        }
    }

    private void OnSqueezeStarted(InputAction.CallbackContext context)
    {
        Press();
    }

    private void OnSqueezeCanceled(InputAction.CallbackContext context)
    {
        Release();
    }

    void Update()
    {
        // Fallback to legacy input if InputManager not available
        if (inputAction != null) return;

        if (Input.GetKeyDown(keyPress)) Press();
        if (Input.GetKeyUp(keyPress)) Release();
    }

    // Runs after the cups have placed their surfaces for this frame
    void LateUpdate()
    {
        if (!isSqueezing || conductor == null)
        {
            if (stream != null) stream.Hide();
            return;
        }

        Sprite[] frames = conductor.StreamFrames;
        if (frames == null || frames.Length == 0) return;

        if (stream == null)
        {
            float scale = conductor.streamScale * transform.lossyScale.x;
            int order = CupSorting.StreamOrder(teatPosition);
            stream = MilkStream.Create(frames, scale, conductor.streamFramesPerBeat, spriteRenderer.sortingLayerID, order);
        }

        Vector2 direction = -transform.up;
        Vector3 start = TipWorldPosition - (Vector3)(direction * conductor.streamInset);
        float targetY = pouringInto != null ? pourSurfaceY : conductor.AnchorFor(teatPosition).y - conductor.streamFloorOffset;
        float drop = Mathf.Max(0f, start.y - targetY);
        float length = drop / Mathf.Max(0.2f, -direction.y);
        float beat = gameManager.songPositionInBeats > 0f ? gameManager.songPositionInBeats : Time.time * 2f;
        stream.Show(start, direction, length, beat);
    }

    /// <summary>
    /// Programmatically set squeezing state (used by AutoPlayController during attract mode)
    /// </summary>
    public void SetSqueezing(bool squeezing)
    {
        if (squeezing) Press();
        else Release();
    }

    private void Press()
    {
        if (isSqueezing) return;
        isSqueezing = true;
        animator.SetBool("isSqueezing", true);
        gameManager.Judge?.Press(teatPosition, gameManager.CurrentInputBeat);
    }

    private void Release()
    {
        if (!isSqueezing) return;
        isSqueezing = false;
        animator.SetBool("isSqueezing", false);
        gameManager.Judge?.Release(teatPosition, gameManager.CurrentInputBeat);
    }
}
