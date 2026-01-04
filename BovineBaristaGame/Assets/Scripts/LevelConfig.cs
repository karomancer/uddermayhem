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

    [Header("Gameplay")]
    public bool showTutorial = false;

    [Header("Optional")]
    [Tooltip("Note chart data if using external timing data")]
    public TextAsset noteChart;

    [Tooltip("Speed multiplier for cup movement, etc.")]
    public float speedMultiplier = 1f;

    [Tooltip("How forgiving the timing windows are (1 = normal, higher = easier)")]
    public float timingLeniency = 1f;
}
