using UnityEngine;

/// <summary>
/// Keeps a UI text parked at a world-space spot, such as the cow's belly above the udder, at any
/// resolution and while the cow is still sliding into place.
/// </summary>
public class StreakTextAnchor : MonoBehaviour
{
    [Tooltip("World object to sit above (the udder base)")]
    public Transform target;
    [Tooltip("Offset from the target in world units (+y = up)")]
    public Vector2 worldOffset = new Vector2(0f, 1.1f);

    private RectTransform rect;
    private Canvas canvas;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void LateUpdate()
    {
        Camera camera = Camera.main;
        if (target == null || camera == null || rect == null) return;
        Vector3 world = target.position + (Vector3)worldOffset;
        Vector2 screen = camera.WorldToScreenPoint(world);
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screen, uiCamera, out Vector3 point)) rect.position = point;
    }
}
