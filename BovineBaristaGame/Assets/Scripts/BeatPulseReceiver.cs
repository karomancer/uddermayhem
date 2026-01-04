using System.Collections;
using UnityEngine;

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

    private Vector3 originalScale;
    private bool isPulsing = false;
    private int beatCount = 0;

    void Start()
    {
        originalScale = transform.localScale;
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

        // Check if we should pulse on this beat
        if (beatCount % beatsPerPulse != 0) return;
        if (intensity < minimumIntensity) return;
        if (isPulsing) return;

        StartCoroutine(Pulse(intensity));
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

    // Reset to original scale (useful if interrupted)
    public void ResetScale()
    {
        StopAllCoroutines();
        transform.localScale = originalScale;
        isPulsing = false;
    }
}
