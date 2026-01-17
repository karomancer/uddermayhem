using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum BeatTiming
{
  TooEarly,
  OnTime,
  TooLate,
  Miss
}

public class GameManager : MonoBehaviour
{
  // Events for other systems to subscribe to
  public static event Action<int> OnStreakChanged;
  public static event Action<int, int> OnScoreChanged; // (newScore, streakMultiplier)

  // Level configuration - set by LevelSelectManager before loading scene
  public static LevelConfig currentLevelConfig;

  // Difficulty and score for end screen
  public static string currentDifficulty = "Medium";
  private static int finalScore = 0;
  public static int FinalScore => finalScore;

  public float beatAllowance = 0.5f;
  public float songPositionInBeats = 0f;

  //How many seconds have passed since the song started
  public float dspSongTime;

  //The offset to the first beat of the song in seconds
  public float firstBeatOffset = 0;

  private AudioSource music;

  private float songPosition = 0f;

  private bool musicIsPlaying = false;

  private bool shouldShowScore = false;

  private CupConductor cupConductor;

  public TMP_Text ScoreText;
  public TMP_Text StreakText; // Optional UI for streak display

  [Header("Countdown")]
  public TMP_Text CountdownText; // UI text for "3, 2, 1, MILK!" countdown
  private float firstNoteBeat = -1f;
  private int lastCountdownShown = -1; // Track which countdown we last showed to avoid re-triggering
  private string[] countdownTexts = { "3", "2", "1", "MILK!" };
  private Vector3 countdownOriginalScale;
  private Coroutine countdownAnimCoroutine;
  [Tooltip("Duration of countdown spiral animation in seconds")]
  public float countdownAnimDuration = 0.2f;
  [Tooltip("Number of rotations during spiral animation")]
  public float countdownSpinRotations = 0.5f;

  private float currentScore = 0;
  public int CurrentScore => (int)currentScore;
  private int OnTimeScore = 0;
  private int TooEarlyScore = 0;
  private int TooLateScore = 0;

  // Streak system
  private int currentStreak = 0;
  private int maxStreak = 0;
  public int CurrentStreak => currentStreak;
  public int MaxStreak => maxStreak;

  [Header("Streak Multipliers")]
  [Tooltip("Streak counts at which multiplier increases (must match multipliers array length)")]
  public int[] streakTiers = { 0, 10, 25, 50 };
  [Tooltip("Score multiplier for each tier (must match streakTiers array length)")]
  public int[] multipliers = { 1, 2, 3, 4 };

  private bool keysAreDisabled = true;
  private bool goingToTitleScreen = false;

  void Awake()
  {
    // Initialize components in Awake so they're ready before any Start() calls
    music = GetComponent<AudioSource>();
    cupConductor = GetComponent<CupConductor>();

    // Apply level config in Awake so it's ready before TutorialManager.Start() runs
    ApplyLevelConfig();
  }

  void Start()
  {
    dspSongTime = (float)AudioSettings.dspTime;
    ScoreText.text = "";
    if (CountdownText != null)
    {
      CountdownText.text = "";
      countdownOriginalScale = CountdownText.transform.localScale;
    }

    // Get first note beat for countdown timing
    if (cupConductor != null)
    {
      firstNoteBeat = cupConductor.GetFirstNoteBeat();
      Debug.Log($"First note at beat {firstNoteBeat}, countdown starts at beat {firstNoteBeat - 4}");
    }
  }

  private void ApplyLevelConfig()
  {
    if (currentLevelConfig != null)
    {
      // Set difficulty name
      currentDifficulty = currentLevelConfig.difficultyName;

      // Set song if specified
      if (currentLevelConfig.song != null && music != null)
      {
        music.clip = currentLevelConfig.song;
        Debug.Log($"Set music clip to: {currentLevelConfig.song.name}");
      }
      else
      {
        Debug.LogWarning($"Song not set! song={currentLevelConfig.song}, music={music}");
      }

      // Set BPM on CupConductor
      if (cupConductor != null)
      {
        CupConductor.BPM = (int)currentLevelConfig.bpm;
        CupConductor.SecPerBeat = 60f / currentLevelConfig.bpm;

        // Load note chart if specified
        if (currentLevelConfig.noteChart != null)
        {
          cupConductor.LoadNotesFromJson(currentLevelConfig.noteChart);
        }
        else
        {
          Debug.LogWarning("No note chart assigned in LevelConfig!");
        }
      }

      // Apply per-song first beat offset
      firstBeatOffset = currentLevelConfig.firstBeatOffset;

      Debug.Log($"Applied level config: {currentLevelConfig.difficultyName}, BPM: {currentLevelConfig.bpm}, Offset: {firstBeatOffset}");
    }
    else
    {
      Debug.LogWarning("currentLevelConfig is NULL!");
    }
  }

  void Update()
  {
    if (musicIsPlaying)
    {
      //determine how many seconds since the song started
      songPosition = (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);

      //determine how many beats since the song started
      songPositionInBeats = songPosition / CupConductor.SecPerBeat;
      // Debug.Log(songPositionInBeats);

      // Update countdown display
      UpdateCountdown();

      if (shouldShowScore) {
        double tips = currentScore / 100.0;
        ScoreText.text = $"Tip jar: ${tips:F2}";
      }

      cupConductor.Conduct(songPositionInBeats);
    }

    // In attract mode, AutoPlayController handles the fade-out timing
    if (!music.isPlaying && !keysAreDisabled && !goingToTitleScreen && !AttractModeManager.IsAttractModeActive) {
      Invoke("GotToTitleScene", 1f);
      goingToTitleScreen = true;
    }

    if (Input.GetKeyDown(KeyCode.Space))
    {
      pauseOrResume();
    }
  }

  public void ShowScore() {
    shouldShowScore = true;
  }

  private void UpdateCountdown()
  {
    if (CountdownText == null || firstNoteBeat < 0) return;

    float countdownStart = firstNoteBeat - 4;
    float countdownEnd = firstNoteBeat;

    // Before countdown starts or after it ends
    if (songPositionInBeats < countdownStart || songPositionInBeats >= countdownEnd)
    {
      if (lastCountdownShown >= 0)
      {
        // Stop any running animation and hide
        if (countdownAnimCoroutine != null) StopCoroutine(countdownAnimCoroutine);
        CountdownText.text = "";
        CountdownText.transform.localScale = countdownOriginalScale;
        CountdownText.transform.rotation = Quaternion.identity;
        lastCountdownShown = -1;
      }
      return;
    }

    // Determine which countdown index we're on (0="3", 1="2", 2="1", 3="MILK!")
    int countdownIndex = Mathf.FloorToInt(songPositionInBeats - countdownStart);
    countdownIndex = Mathf.Clamp(countdownIndex, 0, countdownTexts.Length - 1);

    // Only update if we're showing a new countdown number
    if (countdownIndex != lastCountdownShown)
    {
      // Stop previous animation if running
      if (countdownAnimCoroutine != null) StopCoroutine(countdownAnimCoroutine);

      CountdownText.text = countdownTexts[countdownIndex];
      lastCountdownShown = countdownIndex;
      Debug.Log($"Countdown: {countdownTexts[countdownIndex]} at beat {songPositionInBeats}");

      // Start spiral animation
      countdownAnimCoroutine = StartCoroutine(AnimateCountdownSpiral());
    }
  }

  private IEnumerator AnimateCountdownSpiral()
  {
    Transform t = CountdownText.transform;
    float elapsed = 0f;

    // Start at scale 0 and rotated
    float startRotation = countdownSpinRotations * 360f;

    while (elapsed < countdownAnimDuration)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / countdownAnimDuration;

      // Ease out for snappy feel
      float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

      // Scale from 0 to original
      t.localScale = countdownOriginalScale * easedProgress;

      // Rotate from startRotation to 0
      float currentRotation = Mathf.Lerp(startRotation, 0f, easedProgress);
      t.rotation = Quaternion.Euler(0f, 0f, currentRotation);

      yield return null;
    }

    // Ensure final state
    t.localScale = countdownOriginalScale;
    t.rotation = Quaternion.identity;
    countdownAnimCoroutine = null;
  }

  public void SkipToSong() {
    ShowScore();
    StartSong();
  }

  public void StartSong(float startTime = -1f) {
    Debug.Log($"StartSong called. Clip: {music.clip?.name}, Length: {music.clip?.length}");
    music.Play();
    // Capture dspTime right after Play() for accurate sync
    dspSongTime = (startTime < 0) ? (float)AudioSettings.dspTime : startTime;
    keysAreDisabled = false;
    musicIsPlaying = true;
    Invoke("songIsOver", music.clip.length);
    Debug.Log($"Music isPlaying: {music.isPlaying}, dspSongTime: {dspSongTime}");
  }

  void songIsOver() {
    Debug.Log("SONG IS OVER");
    musicIsPlaying = false;
    finalScore = (int)currentScore;
  }

  void pauseOrResume()
  {

    if (musicIsPlaying)
    {
      music.Pause();
      musicIsPlaying = false;
    }
    else
    {
      music.Play();
      musicIsPlaying = true;
      dspSongTime = (float)AudioSettings.dspTime;
    }
  }

  public void SubmitCustomerFeedback(BeatTiming bt)
  {
    SubmitCustomerFeedback(bt, affectStreak: true);
  }

  public void SubmitCustomerFeedback(BeatTiming bt, bool affectStreak)
  {
    int baseScore = 0;
    bool isHit = false;

    switch (bt)
    {
      case BeatTiming.OnTime:
        OnTimeScore++;
        baseScore = 24;
        isHit = true;
        break;
      case BeatTiming.TooEarly:
        TooEarlyScore++;
        baseScore = 10;
        isHit = true; // Early/late still count as hits for streak
        break;
      case BeatTiming.TooLate:
        TooLateScore++;
        baseScore = 12;
        isHit = true;
        break;
      case BeatTiming.Miss:
        if (affectStreak) ResetStreak();
        return;
      default:
        break;
    }

    if (isHit)
    {
      if (affectStreak) IncrementStreak();
      int multiplier = GetMultiplier();
      currentScore += baseScore * multiplier;
      OnScoreChanged?.Invoke((int)currentScore, multiplier);
    }
  }

  private void IncrementStreak()
  {
    currentStreak++;
    if (currentStreak > maxStreak)
    {
      maxStreak = currentStreak;
    }
    OnStreakChanged?.Invoke(currentStreak);
    UpdateStreakUI();
  }

  public void ResetStreak()
  {
    if (currentStreak > 0)
    {
      currentStreak = 0;
      OnStreakChanged?.Invoke(currentStreak);
      UpdateStreakUI();
    }
  }

  private int GetMultiplier()
  {
    // Safeguard: if arrays are empty or mismatched, return 1x
    if (streakTiers == null || multipliers == null ||
        streakTiers.Length == 0 || multipliers.Length == 0 ||
        streakTiers.Length != multipliers.Length)
    {
      return 1;
    }

    int multiplier = multipliers[0];
    for (int i = streakTiers.Length - 1; i >= 0; i--)
    {
      if (currentStreak >= streakTiers[i])
      {
        multiplier = multipliers[i];
        break;
      }
    }
    return multiplier;
  }

  private void UpdateStreakUI()
  {
    if (StreakText != null)
    {
      if (currentStreak > 0)
      {
        StreakText.text = currentStreak + "x";
      }
      else
      {
        StreakText.text = "";
      }
    }
  }

  // Called by CoffeeController when a cup exits without being hit
  public void ReportMiss()
  {
    SubmitCustomerFeedback(BeatTiming.Miss);
  }

  void GotToTitleScene() {
    SceneManager.LoadScene("EndScreen");
  }

  public BeatTiming IsOnBeat()
  {
    float currentPosition = (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);
    float hit = currentPosition % CupConductor.SecPerBeat;
    float nearestBeat = Mathf.Round(currentPosition % CupConductor.SecPerBeat);

    if (hit < beatAllowance)
    {
      Debug.Log("Hit!");
      return BeatTiming.OnTime;
    }

    if (currentPosition > nearestBeat)
    {
      Debug.Log("Late!");
      return BeatTiming.TooLate;
    }

    Debug.Log("Early!");
    return BeatTiming.TooEarly;

  }

  public BeatTiming IsOnBeat(int measure, float beat)
  {
    return IsOnBeat(measure, beat, songPositionInBeats);
  }

  public BeatTiming IsOnBeat(int measure, float beat, float inputSongPosition)
  {
    return IsOnBeat(measure, beat, inputSongPosition, affectStreak: true);
  }

  public BeatTiming IsOnBeat(int measure, float beat, float inputSongPosition, bool affectStreak)
  {
    float expectedSongPosition = (measure * 4) + beat - 1;
    bool isAcceptablyEarly = inputSongPosition > (expectedSongPosition - beatAllowance);
    bool isAcceptablyLate = inputSongPosition < (expectedSongPosition + beatAllowance);
    Debug.Log("Expected " + expectedSongPosition + " Got: " + inputSongPosition);
    if (isAcceptablyEarly && isAcceptablyLate)
    {
      SubmitCustomerFeedback(BeatTiming.OnTime, affectStreak);
      return BeatTiming.OnTime;
    }

    if (!isAcceptablyLate)
    {
      SubmitCustomerFeedback(BeatTiming.TooLate, affectStreak);
      return BeatTiming.TooLate;
    }

    SubmitCustomerFeedback(BeatTiming.TooEarly, affectStreak);
    return BeatTiming.TooEarly;
  }
}
