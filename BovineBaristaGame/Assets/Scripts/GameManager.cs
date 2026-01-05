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
  private float currentScore = 0;
  private int OnTimeScore = 0;
  private int TooEarlyScore = 0;
  private int TooLateScore = 0;

  // Streak system
  private int currentStreak = 0;
  private int maxStreak = 0;
  public int CurrentStreak => currentStreak;
  public int MaxStreak => maxStreak;

  // Multiplier tiers
  private int[] streakTiers = { 0, 10, 25, 50 };
  private int[] multipliers = { 1, 2, 3, 4 };

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

      Debug.Log($"Applied level config: {currentLevelConfig.difficultyName}, BPM: {currentLevelConfig.bpm}");
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
      Debug.Log(songPositionInBeats);

      if (shouldShowScore) {
        double tips = currentScore / 100;
        ScoreText.text = "Tip jar: $" + tips;
      }

      cupConductor.Conduct(songPositionInBeats);
    }

    if (!music.isPlaying && !keysAreDisabled && !goingToTitleScreen) {
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

  public void SkipToSong() {
    ShowScore();
    StartSong((float)AudioSettings.dspTime);
  }

  public void StartSong(float startTime) {
    Debug.Log($"StartSong called. Clip: {music.clip?.name}, Length: {music.clip?.length}");
    music.Play();
    dspSongTime = startTime;
    keysAreDisabled = false;
    musicIsPlaying = true;
    Invoke("songIsOver", music.clip.length);
    Debug.Log($"Music isPlaying: {music.isPlaying}");
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
        ResetStreak();
        return;
      default:
        break;
    }

    if (isHit)
    {
      IncrementStreak();
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
    float expectedSongPosition = (measure * 4) + beat - 1;
    bool isAcceptablyEarly = inputSongPosition > (expectedSongPosition - beatAllowance);
    bool isAcceptablyLate = inputSongPosition < (expectedSongPosition + beatAllowance);
    Debug.Log("Expected " + expectedSongPosition + " Got: " + inputSongPosition);
    if (isAcceptablyEarly && isAcceptablyLate)
    {
      SubmitCustomerFeedback(BeatTiming.OnTime);
      return BeatTiming.OnTime;
    }

    if (!isAcceptablyLate)
    {
      SubmitCustomerFeedback(BeatTiming.TooLate);
      return BeatTiming.TooLate;
    }

    SubmitCustomerFeedback(BeatTiming.TooEarly);
    return BeatTiming.TooEarly;
  }
}
