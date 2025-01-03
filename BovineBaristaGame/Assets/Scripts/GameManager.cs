using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum BeatTiming
{
  TooEarly,
  OnTime,
  TooLate
}
public class GameManager : MonoBehaviour
{
  public float beatAllowance = 0.33f;
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
  private float currentScore = 0;
  private int OnTimeScore = 0;
  private int TooEarlyScore = 0;
  private int TooLateScore = 0;

  private bool keysAreDisabled = true;
  private bool goingToTitleScreen = false;
  public bool autoPlayEnabled = false;

  void Start()
  {
    music = GetComponent<AudioSource>();
    cupConductor = GetComponent<CupConductor>();
    dspSongTime = (float)AudioSettings.dspTime;
    ScoreText.text = "";
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

      if (shouldShowScore) {
        double tips = currentScore / 100;
        ScoreText.text = $"Tip jar: ${tips:F2}\nOn Time: {OnTimeScore}\nToo Early: {TooEarlyScore}\nToo Late: {TooLateScore}";
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

  public void StartSong(float startTime) {
    music.Play();
    dspSongTime = startTime;
    keysAreDisabled = false;
    musicIsPlaying = true;
    Invoke("songIsOver", music.clip.length);
  }

  void songIsOver() {
    Debug.Log("SONG IS OVER");
    musicIsPlaying = false;
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
    switch (bt)
    {
      case BeatTiming.OnTime:
        OnTimeScore++;
        currentScore += 24;
        break;
      case BeatTiming.TooEarly:
        TooEarlyScore++;
        currentScore += 10;
        break;
      case BeatTiming.TooLate:
        TooLateScore++;
        currentScore += 12;
        break;
      default:
        break;
    }
  }

  void GotToTitleScene() {
    SceneManager.LoadScene("Title");
  }

  public BeatTiming Judge(float timeDelta)
  {
    if (Mathf.Abs(timeDelta) <= beatAllowance)
    {
      // Debug.Log("On Time // Delta: " + timeDelta);
      SubmitCustomerFeedback(BeatTiming.OnTime);
      return BeatTiming.OnTime;
    }

    if (timeDelta > beatAllowance)
    {
      // Debug.Log("Too Late // Delta: " + timeDelta);
      SubmitCustomerFeedback(BeatTiming.TooLate);
      return BeatTiming.TooLate;
    }

    // Debug.Log("Too Early // Delta: " + timeDelta);
    SubmitCustomerFeedback(BeatTiming.TooEarly);
    return BeatTiming.TooEarly;
  }

  // public BeatTiming Judge(int measure, float beat, float timeDelta)
  // {
  //   float expectedSongPosition = (measure * 4) + beat - 1;
  //   bool isAcceptablyEarly = songPositionInBeats > (expectedSongPosition - beatAllowance);
  //   bool isAcceptablyLate = songPositionInBeats < (expectedSongPosition + beatAllowance);
  //   // Debug.Log("Expected " + expectedSongPosition + " Got: " + songPositionInBeats);
  //   if (isAcceptablyEarly && isAcceptablyLate)
  //   {
  //     SubmitCustomerFeedback(BeatTiming.OnTime);
  //     return BeatTiming.OnTime;
  //   }

  //   if (!isAcceptablyLate)
  //   {
  //     SubmitCustomerFeedback(BeatTiming.TooLate);
  //     return BeatTiming.TooLate;
  //   }

  //   SubmitCustomerFeedback(BeatTiming.TooEarly);
  //   return BeatTiming.TooEarly;
  // }
}
