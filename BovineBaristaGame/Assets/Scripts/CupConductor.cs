using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CupTag
{
  public static readonly string FrontLeft = "FrontLeft";
  public static readonly string FrontRight = "FrontRight";
  public static readonly string BackLeft = "BackLeft";
  public static readonly string BackRight = "BackRight";
}

public class CupConductor : MonoBehaviour
{
  public AutoTeatManager autoTeatManager;
  public GameObject cupPrefab;
  
  public static int BPM = 110;

  public static float SecPerBeat = 60f / 110;

  public static float TimeToSlideInSeconds = 0.5f;

  public struct CupNote
  {
    public string type;

    // On which measure # and beat # (within the measure) the note lies
    public int measure;
    public float beat;
    // Duration of the note (for hold-release)
    // 1.0f is a quarter note, 0.5f is an eighth note, etc
    public float duration;

    public CupNote(string _type, int _measure, float _beat, float _duration)
    {
      type = _type;
      measure = _measure;
      beat = _beat;
      duration = _duration;
    }
  }
  public static CupNote[] CUP_NOTES = {
    new CupNote(CupTag.BackRight, 8, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 8, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 8, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 8, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 9, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 9, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 9, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 9, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 10, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 10, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 10, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 10, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 11, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 11, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 11, 3.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 11, 4.0f, 1.0f),
    new CupNote(CupTag.BackRight, 12, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 12, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 12, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 12, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 13, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 13, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 13, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 13, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 14, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 14, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 14, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 14, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 15, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 15, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 15, 3.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 15, 4.0f, 1.0f),
    new CupNote(CupTag.BackRight, 16, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 16, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 16, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 16, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 17, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 17, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 17, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 17, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 18, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 18, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 18, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 18, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 19, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 19, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 19, 3.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 19, 4.0f, 1.0f),
    new CupNote(CupTag.BackRight, 20, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 20, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 20, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 20, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 21, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 21, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 21, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 21, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 22, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 22, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 22, 3.5f, 0.5f),
    new CupNote(CupTag.FrontLeft, 22, 4.0f, 0.5f),
    new CupNote(CupTag.BackRight, 23, 1.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 23, 2.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 23, 3.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 23, 4.0f, 1.0f),
    new CupNote(CupTag.BackRight, 24, 1.0f, 4.0f),
    new CupNote(CupTag.BackLeft, 25, 1.0f, 4.0f),
    new CupNote(CupTag.FrontRight, 26, 1.0f, 4.0f),
    new CupNote(CupTag.FrontLeft, 27, 1.0f, 2.0f),
    new CupNote(CupTag.BackRight, 27, 3.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 27, 4.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 28, 1.0f, 4.0f),
    new CupNote(CupTag.FrontLeft, 29, 1.0f, 4.0f),
    new CupNote(CupTag.BackRight, 30, 1.0f, 4.0f),
    new CupNote(CupTag.BackLeft, 31, 1.0f, 4.0f),
    new CupNote(CupTag.FrontRight, 32, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 32, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 32, 3.5f, 0.5f),
    new CupNote(CupTag.BackLeft, 32, 4.0f, 0.5f),
    new CupNote(CupTag.FrontRight, 33, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 33, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 33, 3.5f, 0.5f),
    new CupNote(CupTag.BackLeft, 33, 4.0f, 0.5f),
    new CupNote(CupTag.FrontRight, 34, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 34, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 34, 3.5f, 0.5f),
    new CupNote(CupTag.BackLeft, 34, 4.0f, 0.5f),
    new CupNote(CupTag.FrontRight, 35, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 35, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 35, 3.5f, 1.0f),
    new CupNote(CupTag.BackLeft, 35, 4.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 36, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 36, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 36, 3.5f, 0.5f),
    new CupNote(CupTag.BackLeft, 36, 4.0f, 0.5f),
    new CupNote(CupTag.FrontRight, 37, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 37, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 37, 3.5f, 0.5f),
    new CupNote(CupTag.BackLeft, 37, 4.0f, 0.5f),
    new CupNote(CupTag.FrontRight, 38, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 38, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 38, 3.5f, 0.5f),
    new CupNote(CupTag.BackLeft, 38, 4.0f, 0.5f),
    new CupNote(CupTag.FrontRight, 39, 1.0f, 1.0f),
    new CupNote(CupTag.FrontLeft, 39, 2.0f, 1.0f),
    new CupNote(CupTag.BackRight, 39, 3.5f, 1.0f),
    new CupNote(CupTag.BackLeft, 39, 4.0f, 1.0f),
    new CupNote(CupTag.BackLeft, 40, 4.0f, 1.0f),
    new CupNote(CupTag.FrontRight, 40, 4.0f, 1.0f)
  };
  public static Dictionary<string, Vector3> CupTagEndVector = new Dictionary<string, Vector3>
  {
    {CupTag.FrontLeft, new Vector3(-1.7798f, -2.6906f, 0f)},
    {CupTag.FrontRight, new Vector3(2.9357f, -2.6906f, 0f)},
    {CupTag.BackLeft, new Vector3(-3.5646f, -1.27f, 0f)},
    {CupTag.BackRight, new Vector3(1.13f, -1.27f, 0f)}
  };

  private Vector2 edgeVector;

  private int cupIndex = 0;

  private GameManager gameManager;

  void Start()
  {
    edgeVector = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
    gameManager = FindObjectOfType<GameManager>();
  }

  public void Conduct(float songPositionInBeats)
  {
    int measure = (int)Mathf.Floor(songPositionInBeats / 4);
    float beatInMeasure = (songPositionInBeats % 4) + 1;


    CupNote nextCup = CUP_NOTES[cupIndex];
    // Debug.Log("measure: " + measure + " beatInMeasure: " + beatInMeasure + ", nextcup: " + nextCup.measure + " " + nextCup.beat);

    float totalBeatNumber = (nextCup.measure * 4) + nextCup.beat - 1;
    // Debug.Log("total beat: " + totalBeatNumber + " songPosition " + songPositionInBeats);

    if (totalBeatNumber <= songPositionInBeats + 1)
    {
      cupIndex++;

      Vector3 endVector = CupTagEndVector[nextCup.type];
      Vector3 startVector = new Vector3(edgeVector.x - 10, endVector.y, 0f);
      GameObject newCup = Instantiate(cupPrefab, startVector, Quaternion.identity) as GameObject;
      newCup.GetComponent<CoffeeController>().init(
        nextCup.type,
        nextCup.measure,
        nextCup.beat,
        nextCup.duration,
        0.558664f
      );

      if (gameManager.autoPlayEnabled)
      {
        // Calculate how many seconds remain until the correct beat
        float beatsUntilPress = totalBeatNumber - songPositionInBeats;
        float timeUntilPress = beatsUntilPress * SecPerBeat;

        // Schedule a perfect teat press at the right time
        autoTeatManager.ScheduleTeatPress(nextCup.type, timeUntilPress, nextCup.duration);
      }
    }

    // Just for debug purposes
    // Debug.Log("Measure: " + measure + " Beat: " + (beatInMeasure + 1));
    // Debug.Log(songPositionInBeats);
  }
}

