using System.Collections;
using UnityEngine;

public class CoffeeController : MonoBehaviour
{
  public Note Note => schedule?.note;

  private CupSchedule schedule;
  private GameManager gameManager;
  private CupConductor conductor;
  private new SpriteRenderer renderer;
  private CupLayers layers;

  private Vector3 anchor;
  private Vector3 baseScale;
  private NoteState lastState;

  private class CupLayers
  {
    public SpriteRenderer back;
    public SpriteRenderer body;
    public SpriteRenderer surface;
    public SpriteRenderer latte;
    public SpriteRenderer front;
    public SpriteRenderer overflow;
    public SpriteMask interior;
  }

  // Cups come from the barista on the left and are passed to the customer on the right
  public void Bind(CupSchedule _schedule, GameManager _gameManager, CupConductor _conductor, Vector3 _anchor)
  {
    schedule = _schedule;
    gameManager = _gameManager;
    conductor = _conductor;
    anchor = _anchor;
    lastState = schedule.note.state;

    CupSize size = schedule.size;
    float scale = conductor.cupScale * (size != null ? size.scaleMultiplier : 1f);
    baseScale = new Vector3(scale, scale, 1f);
    transform.localScale = baseScale;
    transform.position = new Vector3(conductor.OffscreenLeftX, anchor.y, 0f);

    renderer = GetComponent<SpriteRenderer>();
    renderer.sortingOrder = CupSorting.CupFrontOrder(schedule.note.lane) + 1;
    if (size != null && size.HasLayers)
    {
      BuildLayers(size);
    }
    else if (size != null)
    {
      renderer.sprite = size.defaultCup;
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
    UpdateVisuals(beat);
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

  private void UpdateVisuals(float beat)
  {
    Note note = schedule.note;
    CupSize size = schedule.size;
    if (size == null) return;

    bool justStartedHolding = note.state == NoteState.Holding && lastState != NoteState.Holding;
    if (justStartedHolding && note.pressJudgment == BeatTiming.OnTime)
    {
      StartCoroutine(ScaleBump(1.08f, 0.15f));
    }
    lastState = note.state;

    Sprite wholeCup = WholeCupSpriteFor(note, size, beat);
    if (layers != null)
    {
      if (wholeCup != null)
      {
        SetLayersVisible(false);
        renderer.enabled = true;
        renderer.sprite = wholeCup;
      }
      else
      {
        renderer.enabled = false;
        UpdateLayers(note, size, beat);
      }
    }
    else
    {
      renderer.sprite = wholeCup != null ? wholeCup : SpriteFor(note, size, beat);
    }
  }

  // Whole-cup frames: slide in until arrival, the pickup frame in its window before the hit, and the
  // layered cup otherwise (resting, filling, showing its result); a tipped cup stays tipped
  private Sprite WholeCupSpriteFor(Note note, CupSize size, float beat)
  {
    bool done = note.state == NoteState.Done;
    bool tipped = done && note.releaseJudgment != BeatTiming.OnTime && note.releaseJudgment != BeatTiming.TooLate;
    if (tipped) return size.tippedCup;
    if (note.state != NoteState.Pending) return null;

    float beatsBeforeHit = note.startBeat - beat;
    bool inPickupWindow = beatsBeforeHit <= conductor.pickupStartBeatsBeforeHit && beatsBeforeHit > conductor.pickupEndBeatsBeforeHit;
    if (inPickupWindow && size.pickupCup != null) return size.pickupCup;
    if (beat < schedule.arriveBeat) return size.slideInCup;
    return null;
  }

  private void BuildLayers(CupSize size)
  {
    int behindTeat = CupSorting.CupBackOrder(schedule.note.lane);
    int inFrontOfTeat = CupSorting.CupFrontOrder(schedule.note.lane);
    layers = new CupLayers
    {
      back = Layer("GlassBack", size.glassBack, behindTeat),
      body = size.liquidBody != null ? Layer("LiquidBody", size.liquidBody, behindTeat + 1) : null,
      surface = Layer("LiquidSurface", size.liquidSurface, behindTeat + 2),
      latte = Layer("LatteArt", size.latteArt, behindTeat + 3),
      front = Layer("GlassFront", size.glassFront, inFrontOfTeat),
      overflow = Layer("Overflow", size.overflow, inFrontOfTeat + 1)
    };
    if (layers.body != null)
    {
      layers.body.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
    if (size.clipSurfaceToInterior)
    {
      layers.surface.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    GameObject maskObject = new GameObject("InteriorMask");
    maskObject.transform.SetParent(transform, false);
    layers.interior = maskObject.AddComponent<SpriteMask>();
    layers.interior.sprite = size.interiorMask;
    layers.interior.isCustomRangeActive = true;
    layers.interior.backSortingLayerID = renderer.sortingLayerID;
    layers.interior.frontSortingLayerID = renderer.sortingLayerID;
    layers.interior.backSortingOrder = behindTeat;
    layers.interior.frontSortingOrder = behindTeat + 3;

    renderer.enabled = false;
  }

  private SpriteRenderer Layer(string name, Sprite sprite, int order)
  {
    GameObject child = new GameObject(name);
    child.transform.SetParent(transform, false);
    SpriteRenderer layer = child.AddComponent<SpriteRenderer>();
    layer.sprite = sprite;
    layer.sortingLayerID = renderer.sortingLayerID;
    layer.sortingOrder = order;
    return layer;
  }

  private void UpdateLayers(Note note, CupSize size, float beat)
  {
    bool done = note.state == NoteState.Done;
    bool perfect = done && note.releaseJudgment == BeatTiming.OnTime;
    bool overfilled = done && note.releaseJudgment == BeatTiming.TooLate;

    // Level 0 is the resting cup: the espresso shot already sits at the lip before any milk
    float level = LevelFor(note, size, beat);
    layers.back.enabled = true;
    layers.front.enabled = true;
    layers.surface.enabled = true;

    // The body and surface are drawn at the full level; sliding both down together keeps the crest under the ellipse
    float drop = Mathf.Lerp(size.surfaceEmptyY, size.surfaceFullY, level) - size.surfaceFullY;
    layers.surface.transform.localPosition = new Vector3(0f, drop, 0f);
    layers.surface.transform.localScale = new Vector3(Mathf.Lerp(size.surfaceScaleAtBottom, 1f, level), 1f, 1f);
    layers.surface.color = size.surfaceTint.Evaluate(level);
    if (layers.body != null)
    {
      layers.body.enabled = true;
      layers.body.transform.localPosition = new Vector3(0f, drop, 0f);
      layers.body.color = size.liquidTint.Evaluate(level);
    }

    bool holdingPastPerfect = note.state == NoteState.Holding && gameManager.Judge != null
      && beat > note.endBeat + gameManager.Judge.ReleasePerfectBeats;
    layers.latte.enabled = perfect;
    layers.overflow.enabled = overfilled || holdingPastPerfect;
  }

  private void SetLayersVisible(bool visible)
  {
    layers.back.enabled = visible;
    if (layers.body != null) layers.body.enabled = visible;
    layers.surface.enabled = visible;
    layers.latte.enabled = visible;
    layers.front.enabled = visible;
    layers.overflow.enabled = visible;
  }

  private static float LevelFor(Note note, CupSize size, float beat)
  {
    switch (note.state)
    {
      case NoteState.Holding:
        return size.QuantizedLevel((beat - note.pressBeat) / note.HoldBeats);
      case NoteState.Done:
        return 1f;
      default:
        return 0f;
    }
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
        return size.overfilledCup;

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
