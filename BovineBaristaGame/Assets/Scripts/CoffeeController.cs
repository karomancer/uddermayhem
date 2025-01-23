using System.Linq;
using UnityEngine;

enum CupState
{
  Default,
  InProgress,
  OverFilled,
  TippedOver,
  Perfect
}

public class CoffeeController : MonoBehaviour
{
  // TODO: refactor this bullshit
  public Sprite defaultCup;
  public Sprite tooEarlyCup;
  public Sprite tooLateCup;
  public Sprite inProgressCup;
  public Sprite perfectCup;

  public int measure = 4;
  public float beatInMeasure = 1.0f;
  public float duration = 1.0f;

  private string cupTag;

  private ScoreManager scoreManager;
  private GameManager gameManager;

  private new SpriteRenderer renderer;

  private CupState currentState;

  private float scale;

  private float endX;
  private bool initialOnTime = false;
  private float squeezingStartTime = 0f;
  private int collisionCount = 0;
  private float perfectPressTime;
  private float perfectReleaseTime;

  public void init(string _cupTag, int _measure, float _beatInMeasure, float _duration, float _scale)
  {
    cupTag = _cupTag;
    measure = _measure;
    beatInMeasure = _beatInMeasure;
    duration = _duration;
    scale = _scale;

    // Retrieve pre-calculated perfect press and release times
    CupConductor.CupNote cupNote = CupConductor.CUP_NOTES.First(note => note.type == _cupTag && note.measure == _measure && note.beat == _beatInMeasure);
    perfectPressTime = cupNote.perfectPressTime;
    perfectReleaseTime = cupNote.perfectReleaseTime;
  }

  void Start()
  {
    scoreManager = ScoreManager.Instance;
    gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    renderer = GetComponent<SpriteRenderer>();
    defaultCup = renderer.sprite;

    renderer.sortingOrder = (cupTag == CupTag.BackLeft || cupTag == CupTag.BackRight) ? 0 : 1;

    transform.localScale = new Vector3(scale, scale, 0f);

    endX = CupConductor.CupTagEndVector[cupTag].x;

    Invoke("Serve", CupConductor.SecPerBeat + duration * CupConductor.SecPerBeat + 1.0f * CupConductor.SecPerBeat);
  }

  void Update()
  {
    if (transform.position.x != endX)
    {
      float speed = (endX - transform.position.x) * 5;
      transform.Translate(Vector2.right * speed * Time.deltaTime);
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.tag == "Teat")
    {
      string otherType = other.gameObject.name.Split("_")[1];
      if (otherType != cupTag) return;

      collisionCount++;
      float animationDuration = (CupConductor.SecPerBeat * duration) / 2;
      float currentTime = (float)(AudioSettings.dspTime - gameManager.dspSongTime);
      float timeDelta = currentTime - perfectPressTime;
      scoreManager.Judge(timeDelta);

      if (Mathf.Abs(currentTime - perfectPressTime) <= scoreManager.beatAllowance)
      {
        initialOnTime = true;
        squeezingStartTime = Time.time;
        if (duration > 0.5f)
        {
          Invoke("ChangeSpriteToInProgress", animationDuration);
          currentState = CupState.InProgress;
        }
      }
      else
      {
        initialOnTime = false;
        Invoke("ChangeSpriteToTippedOver", 1.0f * CupConductor.SecPerBeat);
        currentState = CupState.TippedOver;
      }
    }
  }

  private void OnTriggerExit2D(Collider2D other)
  {
    if (other.gameObject.tag == "Teat")
    {
      string otherType = other.gameObject.name.Split("_")[1];
      if (otherType != cupTag) return;

      float animationDuration = (CupConductor.SecPerBeat * duration) / 2;
      if (currentState != CupState.TippedOver)
      {
        float squeezingDuration = Time.time - squeezingStartTime;
        float currentTime = (float)(AudioSettings.dspTime - gameManager.dspSongTime);
        float timeDelta = currentTime - perfectReleaseTime;
        scoreManager.Judge(timeDelta);

        if (initialOnTime
          && Mathf.Abs(currentTime - perfectReleaseTime) <= scoreManager.beatAllowance
          && Mathf.Abs(duration * CupConductor.SecPerBeat - squeezingDuration) <= scoreManager.beatAllowance * 2
        )
        {
          ChangeSpriteToLatteArt();
        }
        else if (currentTime > perfectReleaseTime + scoreManager.beatAllowance)
        {
          ChangeSpriteToOverFilled();
        }
        else
        {
          ChangeSpriteToTippedOver();
        }
      }
    }
  }

  private void Serve()
  {
    Vector2 edgeVector = Camera.main.ViewportToWorldPoint(new Vector2(1, 0));
    endX = edgeVector.x + 10;
    Destroy(gameObject, CupConductor.SecPerBeat * duration);
  }

  private void ChangeSpriteToInProgress()
  {
    renderer.sprite = inProgressCup;
  }

  private void ChangeSpriteToTippedOver()
  {
    renderer.sprite = tooEarlyCup;
  }

  private void ChangeSpriteToOverFilled()
  {
    renderer.sprite = tooLateCup;
  }

  private void ChangeSpriteToLatteArt()
  {
    renderer.sprite = perfectCup;
  }

  private void ChangeSpriteToDefault()
  {
    renderer.sprite = defaultCup;
  }
}
