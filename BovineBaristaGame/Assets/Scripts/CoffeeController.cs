using System.Collections;
using UnityEngine;

public class CoffeeController : MonoBehaviour
{
  public Note Note => schedule?.note;

  private CupSchedule schedule;
  private GameManager gameManager;
  private CupConductor conductor;
  private new SpriteRenderer renderer;

  private Vector3 anchor;
  private Vector3 baseScale;
  private NoteState lastState;

  // Cups come from the barista on the left and are passed to the customer on the right
  public void Bind(CupSchedule _schedule, GameManager _gameManager, CupConductor _conductor, Vector3 _anchor)
  {
    schedule = _schedule;
    gameManager = _gameManager;
    conductor = _conductor;
    anchor = _anchor;
    lastState = schedule.note.state;

    baseScale = new Vector3(conductor.cupScale, conductor.cupScale, 1f);
    transform.localScale = baseScale;
    transform.position = new Vector3(conductor.OffscreenLeftX, anchor.y, 0f);

    renderer = GetComponent<SpriteRenderer>();
    TeatPosition lane = schedule.note.lane;
    renderer.sortingOrder = (lane == TeatPosition.BackLeft || lane == TeatPosition.BackRight) ? 0 : 1;
    if (schedule.size != null)
    {
      renderer.sprite = schedule.size.defaultCup;
    }
  }

  void Update()
  {
    if (schedule == null) return;

    float beat = gameManager.songPositionInBeats;
    if (beat >= schedule.departBeat + conductor.exitBeats)
    {
      Destroy(gameObject);
      return;
    }

    UpdateMotion(beat);
    UpdateSprite(beat);
  }

  private void UpdateMotion(float beat)
  {
    if (beat < schedule.arriveBeat)
    {
      float t = Progress(beat - schedule.spawnBeat, conductor.enterBeats);
      SetPosition(Mathf.Lerp(conductor.OffscreenLeftX, anchor.x, EaseOutCubic(t)), anchor.y);
    }
    else if (beat < schedule.departBeat)
    {
      SetPosition(anchor.x, anchor.y);
    }
    else
    {
      float t = Progress(beat - schedule.departBeat, conductor.exitBeats);
      SetPosition(Mathf.Lerp(anchor.x, conductor.OffscreenRightX, EaseInCubic(t)), anchor.y);
    }
  }

  private void UpdateSprite(float beat)
  {
    Note note = schedule.note;
    if (schedule.size == null) return;

    bool justStartedHolding = note.state == NoteState.Holding && lastState != NoteState.Holding;
    if (justStartedHolding && note.pressJudgment == BeatTiming.OnTime)
    {
      StartCoroutine(ScaleBump(1.08f, 0.15f));
    }
    lastState = note.state;

    renderer.sprite = SpriteFor(note, schedule.size, beat);
  }

  private Sprite SpriteFor(Note note, CupSize size, float beat)
  {
    switch (note.state)
    {
      case NoteState.Holding:
        LaneJudge judge = gameManager.Judge;
        if (judge != null && beat > note.endBeat + judge.ReleasePerfectBeats)
        {
          return size.overfilledCup;
        }
        // Milk flows from the squeeze at the cup's own rate, so the fill always feels the same
        return size.InProgressAt((beat - note.pressBeat) / note.HoldBeats);

      case NoteState.Done:
        if (note.releaseJudgment == BeatTiming.OnTime) return size.perfectCup;
        if (note.releaseJudgment == BeatTiming.TooLate) return size.overfilledCup;
        return size.tippedCup;

      default:
        return size.defaultCup;
    }
  }

  private void SetPosition(float x, float y)
  {
    transform.position = new Vector3(x, y, 0f);
  }

  private static float Progress(float elapsedBeats, float durationBeats)
  {
    return durationBeats <= 0f ? 1f : Mathf.Clamp01(elapsedBeats / durationBeats);
  }

  private static float EaseOutCubic(float t)
  {
    return 1f - Mathf.Pow(1f - t, 3f);
  }

  private static float EaseInCubic(float t)
  {
    return t * t * t;
  }

  private IEnumerator ScaleBump(float bumpScale, float duration)
  {
    Vector3 targetScale = baseScale * bumpScale;
    float halfDuration = duration / 2f;

    float elapsed = 0f;
    while (elapsed < halfDuration)
    {
      elapsed += Time.deltaTime;
      transform.localScale = Vector3.Lerp(baseScale, targetScale, elapsed / halfDuration);
      yield return null;
    }

    elapsed = 0f;
    while (elapsed < halfDuration)
    {
      elapsed += Time.deltaTime;
      transform.localScale = Vector3.Lerp(targetScale, baseScale, elapsed / halfDuration);
      yield return null;
    }

    transform.localScale = baseScale;
  }
}
