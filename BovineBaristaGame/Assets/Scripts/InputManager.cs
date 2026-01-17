using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public enum InputDeviceType
{
    KeyboardMouse,
    Gamepad,
    Unknown
}

/// <summary>
/// Singleton that manages the Input System and provides access to input actions.
/// Add this to a GameObject in your first scene (e.g., Title scene).
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public GameInput InputActions { get; private set; }

    // Track the last used input device type
    public InputDeviceType LastUsedDeviceType { get; private set; } = InputDeviceType.Unknown;

    // Event fired when input device type changes
    public static event System.Action<InputDeviceType> OnInputDeviceChanged;

    // Expose Gameplay actions for easy access
    public InputAction SqueezeFrontLeft => InputActions.Gameplay.SqueezeFrontLeft;
    public InputAction SqueezeFrontRight => InputActions.Gameplay.SqueezeFrontRight;
    public InputAction SqueezeBackLeft => InputActions.Gameplay.SqueezeBackLeft;
    public InputAction SqueezeBackRight => InputActions.Gameplay.SqueezeBackRight;

    // Expose UI actions for easy access
    public InputAction Navigate => InputActions.UI.Navigate;
    public InputAction Submit => InputActions.UI.Submit;
    public InputAction Cancel => InputActions.UI.Cancel;

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

        // Create and enable input actions
        InputActions = new GameInput();
        InputActions.Gameplay.Enable();
        InputActions.UI.Enable();

        // Subscribe to input action changes to track device type
        InputSystem.onActionChange += OnActionChange;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            InputSystem.onActionChange -= OnActionChange;
            InputActions?.Dispose();
            Instance = null;
        }
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        // Only track when an action is performed (button pressed, etc.)
        if (change != InputActionChange.ActionPerformed)
            return;

        var action = obj as InputAction;
        if (action == null || action.activeControl == null)
            return;

        var device = action.activeControl.device;
        InputDeviceType newDeviceType = GetDeviceType(device);

        if (newDeviceType != InputDeviceType.Unknown && newDeviceType != LastUsedDeviceType)
        {
            LastUsedDeviceType = newDeviceType;
            OnInputDeviceChanged?.Invoke(newDeviceType);
            Debug.Log($"Input device changed to: {newDeviceType}");
        }
    }

    private InputDeviceType GetDeviceType(InputDevice device)
    {
        if (device is Gamepad)
            return InputDeviceType.Gamepad;
        if (device is Keyboard || device is Mouse)
            return InputDeviceType.KeyboardMouse;
        return InputDeviceType.Unknown;
    }

    /// <summary>
    /// Check if any gameplay key was pressed this frame
    /// </summary>
    public bool AnyGameplayKeyPressed()
    {
        return SqueezeFrontLeft.WasPressedThisFrame() ||
               SqueezeFrontRight.WasPressedThisFrame() ||
               SqueezeBackLeft.WasPressedThisFrame() ||
               SqueezeBackRight.WasPressedThisFrame();
    }

    /// <summary>
    /// Returns true if the last used input device was a gamepad
    /// </summary>
    public bool IsUsingGamepad()
    {
        return LastUsedDeviceType == InputDeviceType.Gamepad;
    }

    /// <summary>
    /// Returns true if the last used input device was keyboard/mouse (or udder via Teensy)
    /// </summary>
    public bool IsUsingKeyboard()
    {
        return LastUsedDeviceType == InputDeviceType.KeyboardMouse;
    }
}
