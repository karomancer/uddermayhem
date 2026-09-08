using UnityEngine;

/// <summary>
/// Per-cabinet input latency calibration, persisted in PlayerPrefs.
/// Positive values treat inputs as having happened earlier than they arrived.
/// </summary>
public static class TimingSettings
{
    private const string INPUT_OFFSET_PREFS_KEY = "InputOffsetSeconds";
    private static float? inputOffsetSeconds;

    public static float InputOffsetSeconds
    {
        get
        {
            if (!inputOffsetSeconds.HasValue)
            {
                inputOffsetSeconds = PlayerPrefs.GetFloat(INPUT_OFFSET_PREFS_KEY, 0f);
            }
            return inputOffsetSeconds.Value;
        }
        set
        {
            inputOffsetSeconds = value;
            PlayerPrefs.SetFloat(INPUT_OFFSET_PREFS_KEY, value);
            PlayerPrefs.Save();
        }
    }
}
