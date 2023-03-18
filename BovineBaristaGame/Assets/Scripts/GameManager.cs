using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BeatTiming
{
  TooEarly,
  OnTime,
  TooLate
}

public class GameManager : MonoBehaviour
{
  public float beatAllowance = 0.1f;
  public float songPositionInBeats = 0f;

  //How many seconds have passed since the song started
  public float dspSongTime;

  //The offset to the first beat of the song in seconds
  public float firstBeatOffset = 0;

  private AudioSource music;
  private float songPosition = 0f;

  private bool musicIsPlaying = false;

  private CupConductor cupConductor;

  private int OnTimeScore = 0;
  private int TooEarlyScore = 0;
  private int TooLateScore = 0;

  void Start()
  {
    music = GetComponent<AudioSource>();
    cupConductor = GetComponent<CupConductor>();

    music.Play();
    musicIsPlaying = true;
    dspSongTime = (float)AudioSettings.dspTime;
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Space))
    {
      pauseOrResume();
    }

    if (musicIsPlaying)
    {
      //determine how many seconds since the song started
      songPosition = (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);

      //determine how many beats since the song started
      songPositionInBeats = songPosition / CupConductor.SecPerBeat;

      cupConductor.Conduct(songPositionInBeats);
    }
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
