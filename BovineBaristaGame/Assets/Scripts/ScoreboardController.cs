using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreboardController : MonoBehaviour
{
  ScoreManager scoreManager;
  public GameObject scoreboard;
  private List<ScoreManager.ScoreData> highScores;

  private void Start()
  {
    scoreManager = ScoreManager.Instance;
    highScores = scoreManager.GetHighScores();
    HydrateScoreboard(highScores);
  }

  public void HydrateScoreboard(List<ScoreManager.ScoreData> highScores) {
    Transform scoreboardTable = scoreboard.transform.Find("ScoreboardTable");
    if (scoreboardTable == null) {
      return;
    }

    int i = 0;
    foreach (ScoreManager.ScoreData scoreData in highScores) {
      Transform scoreboardRow = scoreboardTable.GetChild(i);

      Transform scoreTransform = scoreboardRow.Find("Score");
      if (scoreTransform) {
        scoreTransform.GetComponent<TextMeshProUGUI>().text = (scoreData.score / 100m).ToString("C");
      }

      Transform nameTransform = scoreboardRow.Find("Name");
      if (nameTransform) {
        nameTransform.GetComponent<TextMeshProUGUI>().text = scoreData.playerName;
      }

      i++;
    }
  }
}
