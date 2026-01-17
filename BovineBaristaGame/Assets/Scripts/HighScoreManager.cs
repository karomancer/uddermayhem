using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    private const int MaxEntries = 10;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private string GetKey(string difficulty)
    {
        return "HighScores_" + difficulty;
    }

    public List<HighScoreEntry> GetScores(string difficulty)
    {
        string key = GetKey(difficulty);
        string json = PlayerPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(json))
        {
            return new List<HighScoreEntry>();
        }

        HighScoreList list = JsonUtility.FromJson<HighScoreList>(json);
        return list.entries ?? new List<HighScoreEntry>();
    }

    public void SaveScore(string difficulty, string initials, int score)
    {
        List<HighScoreEntry> scores = GetScores(difficulty);

        scores.Add(new HighScoreEntry(initials.ToUpper(), score));

        // Sort descending by score, take top 10
        scores = scores.OrderByDescending(s => s.score).Take(MaxEntries).ToList();

        HighScoreList list = new HighScoreList { entries = scores };
        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(GetKey(difficulty), json);
        PlayerPrefs.Save();
    }

    public bool IsTopTen(string difficulty, int score)
    {
        List<HighScoreEntry> scores = GetScores(difficulty);

        // If less than 10 entries, any score qualifies
        if (scores.Count < MaxEntries)
        {
            return true;
        }

        // Check if score beats the lowest entry
        int lowestScore = scores.Min(s => s.score);
        return score > lowestScore;
    }

    public int GetRank(string difficulty, int score)
    {
        List<HighScoreEntry> scores = GetScores(difficulty);

        // Find where this score would rank (1-based)
        int rank = 1;
        foreach (var entry in scores.OrderByDescending(s => s.score))
        {
            if (score > entry.score)
            {
                return rank;
            }
            rank++;
        }

        // Would be after all existing scores
        return scores.Count < MaxEntries ? scores.Count + 1 : 0;
    }

    public void ClearScores(string difficulty)
    {
        PlayerPrefs.DeleteKey(GetKey(difficulty));
        PlayerPrefs.Save();
    }

    public void ClearAllScores()
    {
        ClearScores("Easy");
        ClearScores("Medium");
        ClearScores("Hard");
    }
}
