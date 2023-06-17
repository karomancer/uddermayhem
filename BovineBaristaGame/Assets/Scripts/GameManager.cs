using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BeatTiming
{
  TooEarly,
  OnTime,
  TooLate
}

public class GameManager : MonoBehaviour
{
  public float beatAllowance = 0.5f;
  public float songPositionInBeats = 0f;

  //How many seconds have passed since the song started

  //The offset to the first beat of the song in seconds
  public float firstBeatOffset = 0;

  private AudioSource music;
  private AudioClip milkywaycafeClip;

  private float songPosition = 0f;

  private bool musicIsPlaying = false;

  private CupConductor cupConductor;

  private int OnTimeScore = 0;
  private int TooEarlyScore = 0;
  private int TooLateScore = 0;

  private bool keysAreDisabled = true;
  private bool goingToTitleScreen = false;

  void Start()
  {
    music = GetComponent<AudioSource>();
    cupConductor = GetComponent<CupConductor>();

  }

  void Update()
  {    
    if (musicIsPlaying)
    {
      //determine how many seconds since the song started
      songPosition = (float)(AudioSettings.dspTime - firstBeatOffset);
      Debug.Log(songPosition);

      //determine how many beats since the song started
      songPositionInBeats = songPosition / CupConductor.SecPerBeat;

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
  
  public void StartSong() {
    music.Play();
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
    }
  }

  public void SubmitCustomerFeedback(BeatTiming bt)
  {
    switch (bt)
    {
      case BeatTiming.OnTime:
        OnTimeScore++;
        break;
      case BeatTiming.TooEarly:
        TooEarlyScore++;
        break;
      case BeatTiming.TooLate:
        TooLateScore++;
        break;
      default:
        break;
    }
  }

  void GotToTitleScene() {
    SceneManager.LoadScene("Title");
  }

  public BeatTiming IsOnBeat()
  {
    float currentPosition = (float)(AudioSettings.dspTime - firstBeatOffset);
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
    float expectedSongPosition = (measure * 4) + beat - 1;
    bool isAcceptablyEarly = songPositionInBeats > (expectedSongPosition - beatAllowance);
    bool isAcceptablyLate = songPositionInBeats < (expectedSongPosition + beatAllowance);
    Debug.Log("Expected " + expectedSongPosition + " Got: " + songPositionInBeats);
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
