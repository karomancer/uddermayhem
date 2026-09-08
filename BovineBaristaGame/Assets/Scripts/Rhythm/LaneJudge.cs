using System;
using System.Collections.Generic;

/// <summary>
/// Scores squeeze presses and releases against a chart by time alone.
/// Notes must be sorted by startBeat. All beats are song positions in beats.
/// </summary>
public class LaneJudge
{
    public event Action<Note, BeatTiming> OnPressJudged;
    public event Action<Note, BeatTiming> OnReleaseJudged;
    public event Action<Note> OnMissed;

    // An Early/Late press tips the cup immediately and the hold is not judged
    public bool failCupOnImperfectPress = true;

    private readonly IReadOnlyList<Note> notes;
    private readonly float pressPerfectBeats;
    private readonly float pressHitBeats;
    private readonly float releasePerfectBeats;
    private readonly float releaseHitBeats;
    private readonly Note[] holding = new Note[4];
    private int firstLiveIndex = 0;

    public float ReleasePerfectBeats => releasePerfectBeats;
    public float ReleaseHitBeats => releaseHitBeats;

    public LaneJudge(IReadOnlyList<Note> notes, TimingWindows windows, float secPerBeat)
    {
        this.notes = notes;
        pressPerfectBeats = windows.pressPerfect / secPerBeat;
        pressHitBeats = windows.pressHit / secPerBeat;
        releasePerfectBeats = windows.releasePerfect / secPerBeat;
        releaseHitBeats = windows.releaseHit / secPerBeat;
    }

    public void Press(TeatPosition lane, float beat)
    {
        if (holding[(int)lane] != null) return;

        Note note = NearestPending(lane, beat);
        if (note == null) return;

        BeatTiming judgment = Judge(beat - note.startBeat, pressPerfectBeats);
        note.pressBeat = beat;
        note.pressJudgment = judgment;

        if (judgment == BeatTiming.OnTime || !failCupOnImperfectPress)
        {
            note.state = NoteState.Holding;
            holding[(int)lane] = note;
        }
        else
        {
            note.state = NoteState.Done;
        }

        OnPressJudged?.Invoke(note, judgment);
    }

    public void Release(TeatPosition lane, float beat)
    {
        Note note = holding[(int)lane];
        if (note == null) return;

        FinishHold(note, beat, Judge(beat - note.endBeat, releasePerfectBeats));
    }

    public void Tick(float beat)
    {
        for (int i = firstLiveIndex; i < notes.Count; i++)
        {
            Note note = notes[i];
            if (note.startBeat - pressHitBeats > beat) break;

            switch (note.state)
            {
                case NoteState.Pending:
                    if (beat > note.startBeat + pressHitBeats)
                    {
                        note.state = NoteState.Missed;
                        OnMissed?.Invoke(note);
                    }
                    break;
                case NoteState.Holding:
                    if (beat > note.endBeat + releaseHitBeats)
                    {
                        FinishHold(note, beat, BeatTiming.TooLate);
                    }
                    break;
            }
        }

        while (firstLiveIndex < notes.Count && IsFinished(notes[firstLiveIndex]))
        {
            firstLiveIndex++;
        }
    }

    private void FinishHold(Note note, float beat, BeatTiming judgment)
    {
        note.releaseBeat = beat;
        note.releaseJudgment = judgment;
        note.state = NoteState.Done;
        holding[(int)note.lane] = null;
        OnReleaseJudged?.Invoke(note, judgment);
    }

    private Note NearestPending(TeatPosition lane, float beat)
    {
        Note best = null;
        float bestDistance = float.MaxValue;

        for (int i = firstLiveIndex; i < notes.Count; i++)
        {
            Note note = notes[i];
            if (note.lane != lane || note.state != NoteState.Pending) continue;

            float earliest = note.startBeat - pressHitBeats;
            if (note.cueBeat.HasValue) earliest = Math.Min(earliest, note.cueBeat.Value);
            if (beat < earliest || beat > note.startBeat + pressHitBeats) continue;

            float distance = Math.Abs(beat - note.startBeat);
            if (distance < bestDistance)
            {
                best = note;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool IsFinished(Note note)
    {
        return note.state == NoteState.Done || note.state == NoteState.Missed;
    }

    private static BeatTiming Judge(float deltaBeats, float perfectBeats)
    {
        if (Math.Abs(deltaBeats) <= perfectBeats) return BeatTiming.OnTime;
        return deltaBeats < 0 ? BeatTiming.TooEarly : BeatTiming.TooLate;
    }
}
