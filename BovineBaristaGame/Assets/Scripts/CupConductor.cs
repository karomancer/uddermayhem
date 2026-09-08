using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// JSON parsing classes
[System.Serializable]
public class NoteChartData
{
  public NoteData[] notes;
}

[System.Serializable]
public class NoteData
{
  public string type;
  public int measure;
  public float beat;
  public float duration;
}

// When a cup enters, stops, and leaves for one note (all in song beats)
public class CupSchedule
{
  public Note note;
  public CupSize size;
  public float spawnBeat;
  public float arriveBeat;
  public float departBeat;
}

public class CupConductor : MonoBehaviour
{
  public GameObject cupPrefab;
  public static int BPM = 110;

  public static float SecPerBeat = 60f / 110;

  [Header("Cup Choreography (beats)")]
  [Tooltip("How many beats before its note a cup arrives and stops (a pickup before the hit)")]
  public float arriveLeadBeats = 0.5f;
  [Tooltip("How many beats the slide in takes")]
  public float enterBeats = 0.5f;
  [Tooltip("How many beats the exit takes")]
  public float exitBeats = 0.5f;
  [Tooltip("How many beats a finished cup stays before leaving")]
  public float lingerBeats = 0.5f;
  [Tooltip("Beats before the hit at which the cup switches to its pickup frame")]
  public float pickupStartBeatsBeforeHit = 0.375f;
  [Tooltip("Beats before the hit at which the pickup frame ends (0 = holds until the hit)")]
  public float pickupEndBeatsBeforeHit = 0f;
  [Tooltip("In a dense lane, how many beats before the next cup lands the previous one starts leaving")]
  public float departClearanceBeats = 0.25f;
  public float cupScale = 0.558664f;
  [Tooltip("Every cup's foot sits this far below its lane anchor, whatever its size (needs CupSize.footY)")]
  public float footBelowAnchor = 1.6f;

  [Header("Milk Stream")]
  [Tooltip("Stream frames cycled while pouring; leave empty to load Resources/Stream")]
  public Sprite[] streamFrames;
  [Tooltip("Splash frames drawn where the milk lands, cycled with the stream; leave empty to load Resources/Splash")]
  public Sprite[] splashFrames;
  [Tooltip("Raises the splash above the point where the milk lands, in world units")]
  public float splashOffsetY = 0.15f;
  [Tooltip("Stream art scale relative to the teat it pours from")]
  public float streamScale = 0.5f;
  [Tooltip("How many stream frames advance per beat")]
  public float streamFramesPerBeat = 2f;
  [Tooltip("With no cup in the lane, milk lands this far below the lane's cup anchor; 1.37 = the L cup's resting liquid line")]
  public float streamFloorOffset = 1.37f;
  [Tooltip("How far up inside the teat the stream starts, in world units, so its top is hidden behind the tip")]
  public float streamInset = 0.25f;

  [Header("Lanes")]
  [Tooltip("Where cups stop, indexed by TeatPosition; unassigned lanes fall back to CupTagEndVector")]
  public Transform[] laneAnchors = new Transform[4];
  [Tooltip("Cup art per hold length; leave empty to load Resources/CupSizes")]
  public CupSize[] cupSizes;

  public struct CupNote
  {
    public TeatPosition type;

    // On which measure # and beat # (within the measure) the note lies
    public int measure;
    public float beat;
    // Duration of the note (for hold-release)
    // 1.0f is a quarter note, 0.5f is an eighth note, etc
    public float duration;

    public CupNote(TeatPosition _type, int _measure, float _beat, float _duration)
    {
      type = _type;
      measure = _measure;
      beat = _beat;
      duration = _duration;
    }
  }

  // Default notes (fallback if no JSON provided)
  public static CupNote[] DEFAULT_NOTES = {
    new CupNote(TeatPosition.BackRight, 8, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 8, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 8, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 8, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 9, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 9, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 9, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 9, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 10, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 10, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 10, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 10, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 11, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 11, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 11, 3.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 11, 4.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 12, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 12, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 12, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 12, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 13, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 13, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 13, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 13, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 14, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 14, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 14, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 14, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 15, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 15, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 15, 3.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 15, 4.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 16, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 16, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 16, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 16, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 17, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 17, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 17, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 17, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 18, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 18, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 18, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 18, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 19, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 19, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 19, 3.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 19, 4.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 20, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 20, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 20, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 20, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 21, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 21, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 21, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 21, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 22, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 22, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 22, 3.5f, 0.5f),
    new CupNote(TeatPosition.FrontLeft, 22, 4.0f, 0.5f),
    new CupNote(TeatPosition.BackRight, 23, 1.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 23, 2.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 23, 3.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 23, 4.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 24, 1.0f, 4.0f),
    new CupNote(TeatPosition.BackLeft, 25, 1.0f, 4.0f),
    new CupNote(TeatPosition.FrontRight, 26, 1.0f, 4.0f),
    new CupNote(TeatPosition.FrontLeft, 27, 1.0f, 2.0f),
    new CupNote(TeatPosition.BackRight, 27, 3.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 27, 4.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 28, 1.0f, 4.0f),
    new CupNote(TeatPosition.FrontLeft, 29, 1.0f, 4.0f),
    new CupNote(TeatPosition.BackRight, 30, 1.0f, 4.0f),
    new CupNote(TeatPosition.BackLeft, 31, 1.0f, 4.0f),
    new CupNote(TeatPosition.FrontRight, 32, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 32, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 32, 3.5f, 0.5f),
    new CupNote(TeatPosition.BackLeft, 32, 4.0f, 0.5f),
    new CupNote(TeatPosition.FrontRight, 33, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 33, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 33, 3.5f, 0.5f),
    new CupNote(TeatPosition.BackLeft, 33, 4.0f, 0.5f),
    new CupNote(TeatPosition.FrontRight, 34, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 34, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 34, 3.5f, 0.5f),
    new CupNote(TeatPosition.BackLeft, 34, 4.0f, 0.5f),
    new CupNote(TeatPosition.FrontRight, 35, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 35, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 35, 3.5f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 35, 4.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 36, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 36, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 36, 3.5f, 0.5f),
    new CupNote(TeatPosition.BackLeft, 36, 4.0f, 0.5f),
    new CupNote(TeatPosition.FrontRight, 37, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 37, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 37, 3.5f, 0.5f),
    new CupNote(TeatPosition.BackLeft, 37, 4.0f, 0.5f),
    new CupNote(TeatPosition.FrontRight, 38, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 38, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 38, 3.5f, 0.5f),
    new CupNote(TeatPosition.BackLeft, 38, 4.0f, 0.5f),
    new CupNote(TeatPosition.FrontRight, 39, 1.0f, 1.0f),
    new CupNote(TeatPosition.FrontLeft, 39, 2.0f, 1.0f),
    new CupNote(TeatPosition.BackRight, 39, 3.5f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 39, 4.0f, 1.0f),
    new CupNote(TeatPosition.BackLeft, 40, 4.0f, 1.0f),
    new CupNote(TeatPosition.FrontRight, 40, 4.0f, 1.0f)
  };
  public static Dictionary<TeatPosition, Vector3> CupTagEndVector = new Dictionary<TeatPosition, Vector3>
  {
    {TeatPosition.FrontLeft, new Vector3(-1.6f, -2.6906f, 0f)},
    {TeatPosition.FrontRight, new Vector3(2.9357f, -2.6906f, 0f)},
    {TeatPosition.BackLeft, new Vector3(-3.5646f, -1.27f, 0f)},
    {TeatPosition.BackRight, new Vector3(1.13f, -1.27f, 0f)}
  };

  public Sprite[] SplashFrames
  {
    get
    {
      if (splashFrames == null || splashFrames.Length == 0)
      {
        splashFrames = Resources.LoadAll<Sprite>("Splash").OrderBy(sprite => sprite.name).ToArray();
      }
      return splashFrames;
    }
  }

  public Sprite[] StreamFrames
  {
    get
    {
      if (streamFrames == null || streamFrames.Length == 0)
      {
        streamFrames = Resources.LoadAll<Sprite>("Stream").OrderBy(sprite => sprite.name).ToArray();
      }
      return streamFrames;
    }
  }

  private readonly List<Note> notes = new List<Note>();
  public IReadOnlyList<Note> Notes => notes;

  private readonly List<CupSchedule> schedule = new List<CupSchedule>();
  private int scheduleIndex = 0;

  private GameManager gameManager;
  public float OffscreenLeftX { get; private set; }
  public float OffscreenRightX { get; private set; }

  [Header("Debug")]
  public NoteTimingDebugger debugger;
  private int debugToneIndex = 0;

  void Awake()
  {
    gameManager = GetComponent<GameManager>();
  }

  void Start()
  {
    OffscreenLeftX = Camera.main.ViewportToWorldPoint(new Vector2(0, 0)).x - 10;
    OffscreenRightX = Camera.main.ViewportToWorldPoint(new Vector2(1, 0)).x + 10;
    EnsureNotes();
  }

  /// <summary>
  /// Load notes from a JSON TextAsset. Call this before the song starts.
  /// </summary>
  public void LoadNotesFromJson(TextAsset noteChartJson)
  {
    if (noteChartJson == null)
    {
      Debug.Log("No note chart provided, using default notes");
      SetNotes(DEFAULT_NOTES);
      return;
    }

    try
    {
      NoteChartData chartData = JsonUtility.FromJson<NoteChartData>(noteChartJson.text);
      if (chartData != null && chartData.notes != null && chartData.notes.Length > 0)
      {
        CupNote[] parsed = new CupNote[chartData.notes.Length];
        for (int i = 0; i < chartData.notes.Length; i++)
        {
          NoteData n = chartData.notes[i];
          TeatPosition lane = (TeatPosition)Enum.Parse(typeof(TeatPosition), n.type);
          parsed[i] = new CupNote(lane, n.measure, n.beat, n.duration);
        }
        SetNotes(parsed);
        Debug.Log($"Loaded {notes.Count} notes from JSON");
      }
      else
      {
        Debug.LogWarning("Note chart JSON was empty or invalid, using default notes");
        SetNotes(DEFAULT_NOTES);
      }
    }
    catch (Exception e)
    {
      Debug.LogError($"Failed to parse note chart JSON: {e.Message}");
      SetNotes(DEFAULT_NOTES);
    }
  }

  private void EnsureNotes()
  {
    if (notes.Count == 0)
    {
      SetNotes(DEFAULT_NOTES);
    }
  }

  private void SetNotes(IEnumerable<CupNote> source)
  {
    notes.Clear();
    schedule.Clear();
    scheduleIndex = 0;
    debugToneIndex = 0;

    foreach (CupNote cupNote in source)
    {
      notes.Add(new Note(cupNote.type, Note.ToAbsoluteBeat(cupNote.measure, cupNote.beat), cupNote.duration));
    }
    notes.Sort((a, b) => a.startBeat != b.startBeat ? a.startBeat.CompareTo(b.startBeat) : a.lane.CompareTo(b.lane));

    LinkLanes();
    BuildSchedule();
  }

  private void LinkLanes()
  {
    Note[] lastInLane = new Note[4];
    foreach (Note note in notes)
    {
      Note previous = lastInLane[(int)note.lane];
      if (previous != null) previous.next = note;
      lastInLane[(int)note.lane] = note;
    }
  }

  // Cups in one lane behave like a conveyor: the next cup never lands before the previous hold
  // ends (plus clearance), and the previous cup leaves before the next one lands.
  private void BuildSchedule()
  {
    EnsureCupSizes();
    CupSchedule[] previousInLane = new CupSchedule[4];

    foreach (Note note in notes)
    {
      CupSize size = SizeFor(note.HoldBeats);
      float lead = (size != null && size.arriveLeadBeatsOverride > 0f) ? size.arriveLeadBeatsOverride : arriveLeadBeats;

      CupSchedule entry = new CupSchedule
      {
        note = note,
        size = size,
        arriveBeat = note.startBeat - lead,
        departBeat = note.endBeat + lingerBeats
      };

      CupSchedule previous = previousInLane[(int)note.lane];
      if (previous != null)
      {
        float earliestArrival = previous.note.endBeat + departClearanceBeats;
        entry.arriveBeat = Mathf.Min(Mathf.Max(entry.arriveBeat, earliestArrival), note.startBeat);
        previous.departBeat = Mathf.Max(previous.note.endBeat, Mathf.Min(previous.departBeat, entry.arriveBeat - departClearanceBeats));
      }

      entry.spawnBeat = entry.arriveBeat - enterBeats;
      note.cueBeat = entry.spawnBeat;
      schedule.Add(entry);
      previousInLane[(int)note.lane] = entry;
    }

    schedule.Sort((a, b) => a.spawnBeat.CompareTo(b.spawnBeat));
  }

  private void EnsureCupSizes()
  {
    if (cupSizes == null || cupSizes.Length == 0)
    {
      cupSizes = Resources.LoadAll<CupSize>("CupSizes");
    }
    if (cupSizes.Length == 0)
    {
      Debug.LogError("No CupSize assets found in Resources/CupSizes");
    }
  }

  private CupSize SizeFor(float holdBeats)
  {
    CupSize best = null;
    float bestDistance = float.MaxValue;
    foreach (CupSize size in cupSizes)
    {
      float distance = Mathf.Abs(size.holdBeats - holdBeats);
      bool closer = distance < bestDistance;
      bool tieBreakLarger = Mathf.Approximately(distance, bestDistance) && best != null && size.holdBeats > best.holdBeats;
      if (closer || tieBreakLarger)
      {
        best = size;
        bestDistance = distance;
      }
    }
    return best;
  }

  /// <summary>
  /// Get the beat position of the first note (for tutorial timing calculations)
  /// </summary>
  public float GetFirstNoteBeat()
  {
    EnsureNotes();
    return notes.Count > 0 ? notes[0].startBeat : 0f;
  }

  /// <summary>
  /// Get the time in seconds until the first cup arrives (from song start)
  /// </summary>
  public float GetSecondsUntilFirstCup()
  {
    return GetFirstNoteBeat() * SecPerBeat;
  }

  public void Conduct(float songPositionInBeats)
  {
    EnsureNotes();

    while (scheduleIndex < schedule.Count && schedule[scheduleIndex].spawnBeat <= songPositionInBeats)
    {
      Spawn(schedule[scheduleIndex]);
      scheduleIndex++;
    }

    // Play debug tones at exact beat timing (separate from cup spawning)
    if (debugger != null && debugToneIndex < notes.Count)
    {
      Note debugNote = notes[debugToneIndex];
      if (songPositionInBeats >= debugNote.startBeat)
      {
        debugger.PlayNoteTone(debugNote.HoldBeats, debugNote.lane);
        debugToneIndex++;
      }
    }
  }

  private void Spawn(CupSchedule entry)
  {
    Vector3 anchor = AnchorFor(entry.note.lane);
    GameObject newCup = Instantiate(cupPrefab, new Vector3(OffscreenLeftX, anchor.y, 0f), Quaternion.identity);
    newCup.GetComponent<CoffeeController>().Bind(entry, gameManager, this, anchor);
  }

  public Vector3 AnchorFor(TeatPosition lane)
  {
    int index = (int)lane;
    Transform anchor = (laneAnchors != null && index < laneAnchors.Length) ? laneAnchors[index] : null;
    return anchor != null ? anchor.position : CupTagEndVector[lane];
  }
}
