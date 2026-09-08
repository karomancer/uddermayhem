using UnityEngine;

/// <summary>
/// Debug tool that plays a tone for the duration each cup note should be held (toggle with T)
/// and shows press/release timing error plus the input offset calibration (toggle with Y, adjust with [ ]).
/// Attach to GameManager.
/// </summary>
public class NoteTimingDebugger : MonoBehaviour
{
    [Header("Settings")]
    public bool enableDebugTones = false;
    public KeyCode toggleKey = KeyCode.T;

    [Header("Tone Settings")]
    [Range(0f, 1f)]
    public float toneVolume = 0.3f;

    [Header("Frequencies by Position")]
    public float frontLeftFrequency = 262f;   // C4
    public float frontRightFrequency = 330f;  // E4
    public float backLeftFrequency = 392f;    // G4
    public float backRightFrequency = 523f;   // C5

    [Header("Timing Readout")]
    public bool showTimingReadout = false;
    public KeyCode readoutToggleKey = KeyCode.Y;
    public KeyCode offsetDownKey = KeyCode.LeftBracket;
    public KeyCode offsetUpKey = KeyCode.RightBracket;
    public float offsetStepMs = 5f;

    private GameManager gameManager;
    private LaneJudge subscribedJudge;
    private string lastPressText = "-";
    private string lastReleaseText = "-";

    private float sampleRate = 44100f;
    private float phase = 0f;
    private float toneEndTime = 0f;
    private bool isPlayingTone = false;
    private float currentFrequency = 440f;

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        gameManager = GetComponent<GameManager>();
    }

    void OnDestroy()
    {
        UnsubscribeFromJudge();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            enableDebugTones = !enableDebugTones;
            Debug.Log("Note Timing Debugger: " + (enableDebugTones ? "ON" : "OFF"));
        }

        if (Input.GetKeyDown(readoutToggleKey))
        {
            showTimingReadout = !showTimingReadout;
        }

        if (showTimingReadout)
        {
            if (Input.GetKeyDown(offsetDownKey)) NudgeInputOffset(-offsetStepMs);
            if (Input.GetKeyDown(offsetUpKey)) NudgeInputOffset(offsetStepMs);
        }

        if (gameManager != null && gameManager.Judge != null && subscribedJudge != gameManager.Judge)
        {
            UnsubscribeFromJudge();
            subscribedJudge = gameManager.Judge;
            subscribedJudge.OnPressJudged += HandlePressJudged;
            subscribedJudge.OnReleaseJudged += HandleReleaseJudged;
        }

        if (isPlayingTone && Time.time >= toneEndTime)
        {
            isPlayingTone = false;
        }
    }

    void OnGUI()
    {
        if (!showTimingReadout) return;

        string text = $"Input offset: {TimingSettings.InputOffsetSeconds * 1000f:+0;-0} ms  ([ / ] to adjust)\n" +
                      $"Press:   {lastPressText}\n" +
                      $"Release: {lastReleaseText}";
        GUI.Label(new Rect(10, 10, 600, 80), text);
    }

    private void HandlePressJudged(Note note, BeatTiming timing)
    {
        lastPressText = $"{BeatsToMs(note.pressBeat - note.startBeat):+0;-0} ms ({timing})";
    }

    private void HandleReleaseJudged(Note note, BeatTiming timing)
    {
        lastReleaseText = $"{BeatsToMs(note.releaseBeat - note.endBeat):+0;-0} ms ({timing})";
    }

    private static float BeatsToMs(float beats)
    {
        return beats * CupConductor.SecPerBeat * 1000f;
    }

    private void NudgeInputOffset(float deltaMs)
    {
        TimingSettings.InputOffsetSeconds += deltaMs / 1000f;
    }

    private void UnsubscribeFromJudge()
    {
        if (subscribedJudge == null) return;
        subscribedJudge.OnPressJudged -= HandlePressJudged;
        subscribedJudge.OnReleaseJudged -= HandleReleaseJudged;
        subscribedJudge = null;
    }

    /// <summary>
    /// Call this when a cup should be hit. Tone plays for the note's duration.
    /// </summary>
    public void PlayNoteTone(float durationInBeats, TeatPosition lane)
    {
        if (!enableDebugTones) return;

        // Set frequency based on cup position
        currentFrequency = lane switch
        {
            TeatPosition.FrontLeft => frontLeftFrequency,
            TeatPosition.FrontRight => frontRightFrequency,
            TeatPosition.BackLeft => backLeftFrequency,
            TeatPosition.BackRight => backRightFrequency,
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
