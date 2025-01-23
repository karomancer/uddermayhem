using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
  public static ScoreManager Instance { get; private set; }

  [System.Serializable]
  public struct ScoreData
  {
    public int score;
    public string playerName;

    public ScoreData(int score, string playerName)
    {
      this.score = score;
      this.playerName = playerName;
    }
  }

  public List<ScoreData> highScores = new List<ScoreData>();
  public int currentScore = 0;
  public float beatAllowance = 0.33f;
  public const int MaxHighScores = 5;
  private int onTimeCount = 0;
  private int tooEarlyCount = 0;
  private int tooLateCount = 0;

  public int OnTimeCount => onTimeCount;
  public int TooEarlyCount => tooEarlyCount;
  public int TooLateCount => tooLateCount;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    LoadScores();
  }

  public int GetCurrentScore()
  {
    return currentScore;
  }

  public void AddScore(int score, string playerName)
  {
    if (string.IsNullOrEmpty(playerName)) playerName = "MOO";

    ScoreData newScore = new ScoreData(score, playerName);
    highScores.Add(newScore);
    highScores = highScores.OrderByDescending(s => s.score).Take(MaxHighScores).ToList();
    SaveScores();
  }

  public List<ScoreData> GetHighScores()
  {
    return highScores;
  }

  public bool IsHighScore(float score)
  {
    if (highScores.Count < MaxHighScores) return true;
    return score > highScores.Min(s => s.score) || highScores.Count < MaxHighScores;
  }

  void SaveScores()
  {
    for (int i = 0; i < highScores.Count; i++)
    {
      string key = $"HighScore{i}";
      PlayerPrefs.SetString(key, JsonUtility.ToJson(highScores[i]));
    }

    PlayerPrefs.Save();
  }

  void LoadScores()
  {
    highScores.Clear();
    for(int i = 0; i < MaxHighScores; i++)
    {
      string key = $"HighScore{i}";
      if (PlayerPrefs.HasKey(key))
      {
        string json = PlayerPrefs.GetString(key);
        ScoreData loadedScore = JsonUtility.FromJson<ScoreData>(json);
        highScores.Add(loadedScore);
      }
    }
    highScores = highScores.OrderByDescending(s => s.score).Take(MaxHighScores).ToList();
  }

  public BeatTiming Judge(float timeDelta)
  {
    if (Mathf.Abs(timeDelta) <= beatAllowance)
    {
      SubmitCustomerFeedback(BeatTiming.OnTime);
      return BeatTiming.OnTime;
    }

    if (timeDelta > beatAllowance)
    {
      SubmitCustomerFeedback(BeatTiming.TooLate);
      return BeatTiming.TooLate;
    }

    SubmitCustomerFeedback(BeatTiming.TooEarly);
    return BeatTiming.TooEarly;
  }

  public void SubmitCustomerFeedback(BeatTiming bt)
  {
    switch (bt)
    {
      case BeatTiming.OnTime:
        onTimeCount++;
        currentScore += 24;
        break;
      case BeatTiming.TooEarly:
        tooEarlyCount++;
        currentScore += 10;
        break;
      case BeatTiming.TooLate:
        tooLateCount++;
        currentScore += 12;
        break;
      default:
        break;
    }
  }
}
