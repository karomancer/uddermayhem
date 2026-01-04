using System.Collections.Generic;

[System.Serializable]
public class HighScoreEntry
{
    public string initials;
    public int score;

    public HighScoreEntry(string initials, int score)
    {
        this.initials = initials;
        this.score = score;
    }
}

[System.Serializable]
public class HighScoreList
{
    public List<HighScoreEntry> entries = new List<HighScoreEntry>();
}
