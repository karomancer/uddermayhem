using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class NameEntryController : MonoBehaviour
{
  public static NameEntryController Instance { get; private set; }
  private ScoreManager scoreManager;

  public TMP_Text message;
  public GameObject cow;
  public GameObject barista;
  public GameObject speechBubble;
  public GameObject nameEntryCanvas;
  public GameObject scoreboard;
  private AudioSource speechBubbleSound;
  private float speechBubbleSoundLength;
  private Animator baristaAnimator;

  public AlphaScroll[] scrollWheels;
  private int currentScrollWheelIndex = 0;
  private int confirmedSelections = 0;
  private bool scoreSaved = false;

  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
    }
  }

  void Start()
  {
    scoreManager = ScoreManager.Instance;
    if (nameEntryCanvas == null)
    {
      Debug.LogWarning("NameEntryCanvas not set in inspector.");
      return;
    }
    nameEntryCanvas.SetActive(false);
    speechBubble.SetActive(false);
    scoreboard.SetActive(false);
    cow.SetActive(false);
    barista.SetActive(true);
    baristaAnimator = barista.GetComponent<Animator>();
    speechBubbleSound = speechBubble.GetComponent<AudioSource>();
    speechBubbleSoundLength = speechBubbleSound.clip.length;

    Invoke("Blink", 0.1f);
    Invoke("Talk", 0.5f);
  }

  void HideBarista() {
    speechBubble.SetActive(false);
    barista.SetActive(false);

    cow.SetActive(true);
    nameEntryCanvas.SetActive(true);
    SetActiveScrollWheel(0);
  }

  void Talk() {
    StopBlinking();

    float talkTime = message.text.Length * 0.03f;

    speechBubbleSound.time = Random.Range(0f, speechBubbleSoundLength - talkTime);
    speechBubbleSound.Play();

    speechBubble.SetActive(true);
    baristaAnimator.SetBool("isTalking", true);

    Invoke("StopTalking", talkTime);
    Invoke("HideBarista", 4f);
  }

  void Blink() {
    baristaAnimator.SetBool("isBlinking", true);
    Invoke("StopBlinking", 1f);
    Invoke("Blink", Random.Range(0, 10f));
  }

  void StopBlinking() {
    baristaAnimator.SetBool("isBlinking", false);
  }

  void StopTalking() {
    baristaAnimator.SetBool("isTalking", false);
    speechBubbleSound.Stop();
  }

  public void ConfirmSelection()
  {
    if (scoreSaved) return;

    confirmedSelections++;

    if (confirmedSelections == scrollWheels.Length)
    {
      FinishNameEntry();
      return;
    }

    currentScrollWheelIndex++;
    SetActiveScrollWheel(currentScrollWheelIndex);
  }

  void SetActiveScrollWheel(int index)
  {
    for (int i = 0; i < scrollWheels.Length; i++)
    {
      scrollWheels[i].IsActive = (i == index);
    }
  }

  private void FinishNameEntry()
  {
    SaveHighScore();
    scoreSaved = true;
    cow.SetActive(false);
    nameEntryCanvas.SetActive(false);
    ShowScoreboard();
  }

  private void ShowScoreboard()
  {
    scoreboard.SetActive(true);
    var scoreboardController = scoreboard.GetComponent<ScoreboardController>();
    scoreboardController.HydrateScoreboard(scoreManager.GetHighScores());
    Invoke("GoToTitleScene", 7f);
  }

  void SaveHighScore()
  {
    if (!scoreManager) {
      Debug.LogWarning("ScoreManager not found in scene. Cannot save score.");
      return;
    }

    string playerName = "";
    foreach (var scrollWheel in scrollWheels) {
      playerName += scrollWheel.GetSelectedLetter();
    }

    int finalScore = scoreManager.GetCurrentScore();
    scoreManager.AddScore(finalScore, playerName);
  }

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

  public void GoToTitleScene() { SceneManager.LoadScene("Title"); }
}
