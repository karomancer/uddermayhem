using UnityEngine;

/// <summary>
/// ScriptableObject that defines configuration for each difficulty level.
/// Create assets via: Right-click in Project → Create → BovineBarista → Level Config
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "BovineBarista/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Difficulty Info")]
    public string difficultyName;           // "Easy", "Medium", "Hard"

    [Header("Music")]
    public AudioClip song;
    public float bpm = 120f;
    [Tooltip("Offset in seconds before beat 1 starts (for songs with intros)")]
    public float firstBeatOffset = 0f;

    [Header("Gameplay")]
    public bool showTutorial = false;

    [Header("Optional")]
    [Tooltip("Note chart data if using external timing data")]
    public TextAsset noteChart;

    [Tooltip("Speed multiplier for cup movement, etc.")]
    public float speedMultiplier = 1f;

    [Tooltip("How forgiving the timing windows are (1 = normal, higher = easier)")]
    public float timingLeniency = 1f;

    [Header("Visual Effect Thresholds")]
    [Tooltip("Score required before cow bouncing starts (0 = always bounce)")]
    public int cowBounceScoreThreshold = 0;

    [Tooltip("Score required before score text pulsing starts (0 = always pulse)")]
    public int scoreTextPulseScoreThreshold = 0;

    [Tooltip("Streak count required before streak text pulsing starts (0 = always pulse)")]
    public int streakTextPulseScoreThreshold = 0;

    [Tooltip("Score required before beat color rotation starts (0 = always rotate)")]
    public int beatColorScoreThreshold = 0;

    [Tooltip("Score required to activate rainbow effect on score text (0 = always active)")]
    public int rainbowScoreTextThreshold = 5000;

    [Tooltip("Streak count required to activate rainbow effect on streak text (0 = always active)")]
    public int rainbowStreakTextThreshold = 0;
}
