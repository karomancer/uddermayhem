using System;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    // Events for beat-synced animations
    public static event Action<float> OnBeat; // intensity (0-1 based on performance)
    public static event Action<int> OnMeasure; // measure number

    [Header("Intensity Settings")]
    [Tooltip("Base intensity when streak is 0")]
    public float baseIntensity = 0.1f;
    [Tooltip("Maximum intensity at high streaks")]
    public float maxIntensity = 1.0f;
    [Tooltip("Streak needed for max intensity")]
    public int streakForMaxIntensity = 50;

    [Header("Beat Settings")]
    public int beatsPerMeasure = 4;

    private GameManager gameManager;
    private int lastBeatNumber = -1;
    private int lastMeasureNumber = -1;
    private int currentStreak = 0;

    void Start()
    {
        GameObject gmObject = GameObject.Find("GameManager");
        if (gmObject != null)
        {
            gameManager = gmObject.GetComponent<GameManager>();
        }

        // Subscribe to streak changes
        GameManager.OnStreakChanged += HandleStreakChanged;
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        GameManager.OnStreakChanged -= HandleStreakChanged;
    }

    void Update()
    {
        if (gameManager == null) return;

        int currentBeat = Mathf.FloorToInt(gameManager.songPositionInBeats);
        int currentMeasure = currentBeat / beatsPerMeasure;

        // Detect new beat
        if (currentBeat > lastBeatNumber && currentBeat >= 0)
        {
            float intensity = CalculateIntensity();
            OnBeat?.Invoke(intensity);
            lastBeatNumber = currentBeat;
        }

        // Detect new measure
        if (currentMeasure > lastMeasureNumber && currentMeasure >= 0)
        {
            OnMeasure?.Invoke(currentMeasure);
            lastMeasureNumber = currentMeasure;
        }
    }

    private void HandleStreakChanged(int streak)
    {
        currentStreak = streak;
    }

    private float CalculateIntensity()
    {
        if (currentStreak >= streakForMaxIntensity)
        {
            return maxIntensity;
        }

        float t = (float)currentStreak / streakForMaxIntensity;
        return Mathf.Lerp(baseIntensity, maxIntensity, t);
    }

    // Reset beat tracking (useful when song restarts)
    public void ResetTracking()
    {
        lastBeatNumber = -1;
        lastMeasureNumber = -1;
    }
}
