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
    public float maxIntensity = 0.7f;
    [Tooltip("Streak needed for max intensity")]
    public int streakForMaxIntensity = 50;

    [Header("Beat Settings")]
    public int beatsPerMeasure = 4;

    [Header("Beat Source (scenes without a GameManager)")]
    [Tooltip("Follow this music instead of the GameManager, e.g. on the title screen")]
    public AudioSource musicSource;
    public float bpm = 100f;
    [Tooltip("Seconds of music before beat 0")]
    public float firstBeatOffset = 0f;

    public float SongPositionInBeats { get; private set; }
    // True once the music has begun (straight away when following the GameManager)
    public bool HasStarted => musicSource == null ? gameManager != null : musicStarted;

    private GameManager gameManager;
    private int lastBeatNumber = -1;
    private int lastMeasureNumber = -1;
    private int currentStreak = 0;
    private bool musicStarted = false;

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
        if (!TryGetSongPosition(out float position)) return;
        SongPositionInBeats = position;

        int currentBeat = Mathf.FloorToInt(position);
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

    private bool TryGetSongPosition(out float position)
    {
        if (musicSource != null)
        {
            if (musicSource.isPlaying && musicSource.clip != null)
            {
                // Read the playhead itself so beats stay locked to what's audible
                musicStarted = true;
                float seconds = (float)musicSource.timeSamples / musicSource.clip.frequency - firstBeatOffset;
                position = seconds * bpm / 60f;
                return true;
            }
            if (musicStarted)
            {
                // Keep the tempo going once the music ends
                position = SongPositionInBeats + Time.deltaTime * bpm / 60f;
                return true;
            }
            // Still waiting for the music to start
            position = 0f;
            return false;
        }

        if (gameManager != null)
        {
            position = gameManager.songPositionInBeats;
            return true;
        }

        position = 0f;
        return false;
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
