using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleSceneController : MonoBehaviour
{
  // public TMP_Text highScoreText;
  // private int highScore;

  // Start is called before the first frame update
  void Start()
  {
    // highScore = PlayerPrefs.GetInt("HighScore", 0);
    // highScoreText.text = highScore.ToString();
  }

  // Update is called once per frame
  void Update()
  {
    if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A))
    {
      Invoke("LoadMainScene", 0.5f);
    }
  }

  void LoadMainScene()
  {
    SceneManager.LoadScene("Main");
  }
}
