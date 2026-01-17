using UnityEngine;
using UnityEngine.InputSystem;

public enum TeatPosition
{
    FrontLeft,
    FrontRight,
    BackLeft,
    BackRight
}

public class TeatController : MonoBehaviour
{
    [Header("Input Configuration")]
    public TeatPosition teatPosition;

    [Header("Legacy Input (optional fallback)")]
    public KeyCode keyPress;

    private bool isSqueezing = false;

    private Animator animator;

    private new BoxCollider2D collider;
    private Vector3 defaultColliderSize;

    private GameManager gameManager;

    private InputAction inputAction;

    // Track song position at press/release for accurate timing
    public float songPositionAtPress { get; private set; }
    public float songPositionAtRelease { get; private set; }

    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        collider = gameObject.GetComponent<BoxCollider2D>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        defaultColliderSize = collider.size;

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
        // Unsubscribe from events to prevent memory leaks
        if (inputAction != null)
        {
            inputAction.started -= OnSqueezeStarted;
            inputAction.canceled -= OnSqueezeCanceled;
        }
    }

    private void OnSqueezeStarted(InputAction.CallbackContext context)
    {
        if (!isSqueezing)
        {
            isSqueezing = true;
            songPositionAtPress = gameManager.songPositionInBeats;
            collider.size = new Vector3(defaultColliderSize.x * 1.5f, defaultColliderSize.y * 2f, 0.0f);
            animator.SetBool("isSqueezing", isSqueezing);
        }
    }

    private void OnSqueezeCanceled(InputAction.CallbackContext context)
    {
        if (isSqueezing)
        {
            isSqueezing = false;
            songPositionAtRelease = gameManager.songPositionInBeats;
            collider.size = defaultColliderSize;
            animator.SetBool("isSqueezing", isSqueezing);
        }
    }

    void Update()
    {
        // Fallback to legacy input if InputManager not available
        if (inputAction == null)
        {
            if (Input.GetKeyDown(keyPress))
            {
                isSqueezing = true;
                songPositionAtPress = gameManager.songPositionInBeats;
                collider.size = new Vector3(defaultColliderSize.x * 1.5f, defaultColliderSize.y * 2f, 0.0f);
            }

            if (Input.GetKeyUp(keyPress))
            {
                isSqueezing = false;
                songPositionAtRelease = gameManager.songPositionInBeats;
                collider.size = defaultColliderSize;
            }

            animator.SetBool("isSqueezing", isSqueezing);
        }
    }

    /// <summary>
    /// Programmatically set squeezing state (used by AutoPlayController during attract mode)
    /// </summary>
    public void SetSqueezing(bool squeezing)
    {
        if (squeezing && !isSqueezing)
        {
            // Simulate key down
            isSqueezing = true;
            songPositionAtPress = gameManager.songPositionInBeats;
            collider.size = new Vector3(defaultColliderSize.x * 1.5f, defaultColliderSize.y * 2f, 0.0f);
        }
        else if (!squeezing && isSqueezing)
        {
            // Simulate key up
            isSqueezing = false;
            songPositionAtRelease = gameManager.songPositionInBeats;
            collider.size = defaultColliderSize;
        }

        animator.SetBool("isSqueezing", isSqueezing);
    }
}
