using System.Collections;
using System.Collections.Generic;
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

  private GameManager gameManager;

  private new SpriteRenderer renderer;

  private CupState currentState;

  private float scale;

  private float endX;

  public void init(string _cupTag, int _measure, float _beatInMeasure, float _duration, float _scale)
  {
    cupTag = _cupTag;
    measure = _measure;
    beatInMeasure = _beatInMeasure;
    duration = _duration;
    scale = _scale;
  }

  void Start()
  {
    gameManager = GameObject.Find("GameManager").GetComponent<GameManager>() || GameObject.Find("TutorialManager").GetComponent<TutorialManager>();
    renderer = GetComponent<SpriteRenderer>();
    defaultCup = renderer.sprite;

    renderer.sortingOrder = (cupTag == CupTag.BackLeft || cupTag == CupTag.BackRight) ? 0 : 1;

    transform.localScale = new Vector3(scale, scale, 0f);

    endX = CupConductor.CupTagEndVector[cupTag].x;

    Invoke("Serve", CupConductor.SecPerBeat + duration);
  }

  void Update()
  {
    if (transform.position.x != endX)
    {
      // CupConductor.SecPerBeat;
      float speed = (endX - transform.position.x) * 5;
      Debug.Log("speed: " + speed);
      transform.Translate(Vector2.right * speed * Time.deltaTime);
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.tag == "Teat")
    {
      string otherType = other.gameObject.name.Split("_")[1];
      float animationDuration = (CupConductor.SecPerBeat * duration) / 2;
      if (otherType == cupTag)
      {
        BeatTiming timing = gameManager.IsOnBeat(measure, beatInMeasure);
        if (timing == BeatTiming.OnTime)
        {
          if (duration > 0.5f)
          {
            Invoke("ChangeSpriteToInProgress", animationDuration);
            currentState = CupState.InProgress;
          }
        }
        else
        {
          Invoke("ChangeSpriteToTippedOver", animationDuration);
          currentState = CupState.TippedOver;
        }
      }
    }
  }

  private void OnTriggerExit2D(Collider2D other)
  {
    if (other.gameObject.tag == "Teat")
    {
      string otherType = other.gameObject.name.Split("_")[1];
      float animationDuration = (CupConductor.SecPerBeat * duration) / 2;
      if (otherType == cupTag && currentState != CupState.TippedOver)
      {
        BeatTiming timing = gameManager.IsOnBeat(measure, beatInMeasure + duration);
        if (timing == BeatTiming.OnTime)
        {
          ChangeSpriteToLatteArt();
        }
        else if (timing == BeatTiming.TooLate)
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
