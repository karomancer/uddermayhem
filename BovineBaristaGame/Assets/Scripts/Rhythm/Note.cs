public enum NoteState
{
    Pending,
    Holding,
    Done,
    Missed
}

public class Note
{
    public readonly TeatPosition lane;
    public readonly float startBeat;
    public readonly float endBeat;

    public NoteState state = NoteState.Pending;
    public float pressBeat;
    public float releaseBeat;
    public BeatTiming? pressJudgment;
    public BeatTiming? releaseJudgment;

    // Next note in the same lane, or null
    public Note next;

    // Beat at which this note's cup appears; a press from then on belongs to this note even
    // before the hit window opens (and is judged early). Null keeps the hit window alone.
    public float? cueBeat;

    public float HoldBeats => endBeat - startBeat;

    public Note(TeatPosition lane, float startBeat, float holdBeats)
    {
        this.lane = lane;
        this.startBeat = startBeat;
        endBeat = startBeat + holdBeats;
    }

    // Charts count measures from 0 and beats within a measure from 1
    public static float ToAbsoluteBeat(int measure, float beatInMeasure)
    {
        return (measure * 4) + beatInMeasure - 1;
    }
}
