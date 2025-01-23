using System;
using System.Linq;
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
  private ScoreManager scoreManager;
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

  private bool keysAreDisabled = true;
  private bool goingToTitleScreen = false;
  private bool enteringHighScore = false;
  private bool viewingLeaderboard = false;
  public bool autoPlayEnabled = false;

  void Start()
  {
    music = GetComponent<AudioSource>();
    cupConductor = GetComponent<CupConductor>();
    scoreManager = ScoreManager.Instance;
    dspSongTime = (float)AudioSettings.dspTime;
    ScoreText.text = "";
  }

  void Update()
  {
    if (musicIsPlaying) {
      //determine how many seconds since the song started
      songPosition = (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);

      //determine how many beats since the song started
      songPositionInBeats = songPosition / CupConductor.SecPerBeat;

      if (shouldShowScore) {
        int score = scoreManager.GetCurrentScore();
        string tips = (score / 100m).ToString("C");
        int onTimeScore = scoreManager.OnTimeCount;
        int tooEarlyScore = scoreManager.TooEarlyCount;
        int tooLateScore = scoreManager.TooLateCount;
        ScoreText.text = $"Tip jar: {tips}\nOn Time: {onTimeScore}\nToo Early: {tooEarlyScore}\nToo Late: {tooLateScore}";
    }

      cupConductor.Conduct(songPositionInBeats);
    }

    if (enteringHighScore) {
      if (Input.GetKeyDown(KeyCode.Return)) {
        enteringHighScore = false;
        // GoToLeaderboard();
      }
    }

    if (!music.isPlaying && !keysAreDisabled && !goingToTitleScreen && !enteringHighScore && !viewingLeaderboard) {
      Invoke("GotToTitleScene", 1f);
      goingToTitleScreen = true;
    }

    if (Input.GetKeyDown(KeyCode.Space)) {
      pauseOrResume();
    }
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
    CheckHighScore();
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


  void GotToTitleScene() { SceneManager.LoadScene("Title"); }

  void GoToNameEntry() { SceneManager.LoadScene("NameEntry"); }

  public void ShowScore() { shouldShowScore = true; }

  private void CheckHighScore()
  {
    int currentScore = scoreManager.GetCurrentScore();
    bool hasHighScore = scoreManager.IsHighScore(currentScore);
    if (hasHighScore) {
      enteringHighScore = true;
      Invoke("GoToNameEntry", 0f);
    } else {
      goingToTitleScreen = true;
      Invoke("GotToTitleScene", 1f);
    }
  }

  public void DisplayHighScores(TMP_Text highScoreText)
  {
    highScoreText.text = "Earningest Esteemed Employees:\n";
    List<ScoreManager.ScoreData> highScores = scoreManager.GetHighScores();
    for (int i = 0; i < highScores.Count; i++)
    {
      highScoreText.text += $"{i + 1}. {highScores[i].playerName}: {highScores[i].score}\n";
    }
  }
}
