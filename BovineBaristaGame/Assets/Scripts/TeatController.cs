using UnityEngine;
using UnityEngine.InputSystem;

public class TeatController : MonoBehaviour
{
    [Header("Input Configuration")]
    public TeatPosition teatPosition;

    [Header("Legacy Input (optional fallback)")]
    public KeyCode keyPress;

    public bool IsSqueezing => isSqueezing;

    private bool isSqueezing = false;

    private Animator animator;

    private GameManager gameManager;

    private InputAction inputAction;

    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

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
