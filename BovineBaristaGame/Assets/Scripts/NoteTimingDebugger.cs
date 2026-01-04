using UnityEngine;

/// <summary>
/// Debug tool that plays a tone for the duration each cup note should be held.
/// Attach to GameManager. Toggle with T key during gameplay.
/// </summary>
public class NoteTimingDebugger : MonoBehaviour
{
    [Header("Settings")]
    public bool enableDebugTones = true;
    public KeyCode toggleKey = KeyCode.T;

    [Header("Tone Settings")]
    [Range(0f, 1f)]
    public float toneVolume = 0.3f;

    [Header("Frequencies by Position")]
    public float frontLeftFrequency = 262f;   // C4
    public float frontRightFrequency = 330f;  // E4
    public float backLeftFrequency = 392f;    // G4
    public float backRightFrequency = 523f;   // C5

    private float sampleRate = 44100f;
    private float phase = 0f;
    private float toneEndTime = 0f;
    private bool isPlayingTone = false;
    private float currentFrequency = 440f;

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            enableDebugTones = !enableDebugTones;
            Debug.Log("Note Timing Debugger: " + (enableDebugTones ? "ON" : "OFF"));
        }

        if (isPlayingTone && Time.time >= toneEndTime)
        {
            isPlayingTone = false;
        }
    }

    /// <summary>
    /// Call this when a cup should be hit. Tone plays for the note's duration.
    /// </summary>
    public void PlayNoteTone(float durationInBeats, string cupType)
    {
        if (!enableDebugTones) return;

        // Set frequency based on cup position
        currentFrequency = cupType switch
        {
            "FrontLeft" => frontLeftFrequency,
            "FrontRight" => frontRightFrequency,
            "BackLeft" => backLeftFrequency,
            "BackRight" => backRightFrequency,
            _ => 440f
        };

        float durationInSeconds = durationInBeats * CupConductor.SecPerBeat;
        toneEndTime = Time.time + durationInSeconds;
        isPlayingTone = true;
        phase = 0f;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isPlayingTone) return;

        float increment = currentFrequency * 2f * Mathf.PI / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            phase += increment;
            float sample = Mathf.Sin(phase) * toneVolume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] += sample;
            }

            if (phase > 2f * Mathf.PI)
            {
                phase -= 2f * Mathf.PI;
            }
        }
    }
}
