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
  public string CupTagName => cupTag;  // Expose for AutoPlayController

  private GameManager gameManager;

  private new SpriteRenderer renderer;

  private CupState currentState;

  private float scale;

  private float endX;

  private bool wasHit = false; // Track if cup was interacted with
  private bool hasBeenProcessed = false; // Prevent multiple collision processing

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
    gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
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
      transform.Translate(Vector2.right * speed * Time.deltaTime);
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.tag == "Teat")
    {
      string otherType = other.gameObject.name.Split("_")[1];
      float animationDuration = (CupConductor.SecPerBeat * duration) / 2;
      if (otherType == cupTag && !hasBeenProcessed)
      {
        hasBeenProcessed = true; // Prevent re-processing
        wasHit = true; // Mark as interacted with

        // Cancel any pending sprite changes
        CancelInvoke("ChangeSpriteToInProgress");
        CancelInvoke("ChangeSpriteToTippedOver");

        TeatController teatController = other.gameObject.GetComponent<TeatController>();
        float inputTime = teatController.songPositionAtPress;
        BeatTiming timing = gameManager.IsOnBeat(measure, beatInMeasure, inputTime);

        // Adjust delay to account for time elapsed since actual input
        float timeSincePress = (gameManager.songPositionInBeats - inputTime) * CupConductor.SecPerBeat;
        float adjustedDelay = Mathf.Max(0, animationDuration - timeSincePress);

        if (timing == BeatTiming.OnTime)
        {
          // Immediate visual feedback - subtle scale bump
          StartCoroutine(ScaleBump(1.08f, 0.15f));

          if (duration > 0.5f)
          {
            Invoke("ChangeSpriteToInProgress", adjustedDelay);
            currentState = CupState.InProgress;
          }
        }
        else
        {
          Invoke("ChangeSpriteToTippedOver", adjustedDelay);
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
        TeatController teatController = other.gameObject.GetComponent<TeatController>();
        float inputTime = teatController.songPositionAtRelease;
        BeatTiming timing = gameManager.IsOnBeat(measure, beatInMeasure + duration, inputTime);
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

  private void OnDestroy()
  {
    // Report miss if cup was never interacted with
    if (!wasHit && gameManager != null)
    {
      gameManager.ReportMiss();
    }
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

  private IEnumerator ScaleBump(float bumpScale, float duration)
  {
    Vector3 originalScale = transform.localScale;
    Vector3 targetScale = originalScale * bumpScale;
    float halfDuration = duration / 2f;

    // Scale up
    float elapsed = 0f;
    while (elapsed < halfDuration)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / halfDuration;
      transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
      yield return null;
    }

    // Scale down
    elapsed = 0f;
    while (elapsed < halfDuration)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / halfDuration;
      transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
      yield return null;
    }

    transform.localScale = originalScale;
  }
}
