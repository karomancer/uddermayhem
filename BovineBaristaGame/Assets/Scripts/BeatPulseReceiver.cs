using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BeatPulseReceiver : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Base scale multiplier for pulse (1.01 = 1% larger)")]
    public float baseScale = 1.01f;
    [Tooltip("Additional scale added at max intensity")]
    public float intensityBonusScale = 0.10f;
    [Tooltip("Duration of the pulse animation")]
    public float pulseDuration = 0.15f;

    [Header("Filtering")]
    [Tooltip("Only pulse every N beats (1 = every beat, 2 = every other beat)")]
    public int beatsPerPulse = 1;
    [Tooltip("Minimum intensity required to trigger pulse (0-1)")]
    public float minimumIntensity = 0f;

    [Header("Score Thresholds")]
    [Tooltip("Score required before pulsing starts (0 = always pulse)")]
    public int pulseScoreThreshold = 0;
    [Tooltip("Score required before color rotation starts (0 = always rotate)")]
    public int colorScoreThreshold = 0;

    [Header("Color Rotation")]
    [Tooltip("Colors to rotate through on each beat (make sure alpha is 255!)")]
    public Color[] colors = new Color[0];
    [Tooltip("Only change color every N beats")]
    public int beatsPerColorChange = 1;
    [Tooltip("Preserve original alpha when changing colors")]
    public bool preserveAlpha = true;

    private Vector3 originalScale;
    private bool isPulsing = false;
    private int beatCount = 0;
    private int colorIndex = 0;
    private Color originalColor;

    // Cached component references
    private TMP_Text tmpText;
    private SpriteRenderer spriteRenderer;
    private Image image;
    private GameManager gameManager;

    void Start()
    {
        originalScale = transform.localScale;

        // Cache component references for color changes
        tmpText = GetComponent<TMP_Text>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();
        gameManager = FindObjectOfType<GameManager>();

        // Store original color
        if (tmpText != null) originalColor = tmpText.color;
        else if (spriteRenderer != null) originalColor = spriteRenderer.color;
        else if (image != null) originalColor = image.color;
    }

    void OnEnable()
    {
        BeatManager.OnBeat += HandleBeat;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= HandleBeat;
    }

    private void HandleBeat(float intensity)
    {
        beatCount++;

        int currentScore = gameManager != null ? gameManager.CurrentScore : 0;

        // Handle color rotation
        if (colors != null && colors.Length > 0 && currentScore >= colorScoreThreshold)
        {
            if (beatCount % beatsPerColorChange == 0)
            {
                colorIndex = (colorIndex + 1) % colors.Length;
                SetColor(colors[colorIndex]);
            }
        }

        // Check if we should pulse on this beat
        if (beatCount % beatsPerPulse != 0) return;
        if (intensity < minimumIntensity) return;
        if (isPulsing) return;
        if (currentScore < pulseScoreThreshold) return;

        StartCoroutine(Pulse(intensity));
    }

    private void SetColor(Color color)
    {
        // Optionally preserve original alpha to prevent disappearing text
        if (preserveAlpha)
        {
            color.a = originalColor.a;
        }

        if (tmpText != null) tmpText.color = color;
        else if (spriteRenderer != null) spriteRenderer.color = color;
        else if (image != null) image.color = color;
    }

    public void ResetColor()
    {
        SetColor(originalColor);
        colorIndex = 0;
    }

    private IEnumerator Pulse(float intensity)
    {
        isPulsing = true;

        // Calculate scale based on intensity
        float scaleMultiplier = baseScale + (intensityBonusScale * intensity);
        Vector3 targetScale = originalScale * scaleMultiplier;
        float halfDuration = pulseDuration / 2f;

        // Scale up
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            // Use smooth step for nicer easing
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        isPulsing = false;
    }

    // Allow manual pulse trigger
    public void TriggerPulse(float intensity = 1f)
    {
        if (!isPulsing)
        {
            StartCoroutine(Pulse(intensity));
        }
    }

    // Reset to original scale and color (useful if interrupted)
    public void ResetScale()
    {
        StopAllCoroutines();
        transform.localScale = originalScale;
        isPulsing = false;
        ResetColor();
    }
}
