using NUnit.Framework;

public class LaneJudgeTests
{
    // 120 BPM, so the 0.1s / 0.2s windows below are 0.2 / 0.4 beats
    private const float SecPerBeat = 0.5f;
    private static readonly TimingWindows Windows = new TimingWindows
    {
        pressPerfect = 0.1f,
        pressHit = 0.2f,
        releasePerfect = 0.1f,
        releaseHit = 0.2f
    };

    private static LaneJudge MakeJudge(params Note[] notes)
    {
        return new LaneJudge(notes, Windows, SecPerBeat);
    }

    [Test]
    public void PerfectPressAndRelease_JudgesOnTime()
    {
        var note = new Note(TeatPosition.FrontLeft, 8f, 1f);
        var judge = MakeJudge(note);
        BeatTiming? press = null, release = null;
        judge.OnPressJudged += (n, t) => press = t;
        judge.OnReleaseJudged += (n, t) => release = t;

        judge.Press(TeatPosition.FrontLeft, 8.05f);
        Assert.AreEqual(NoteState.Holding, note.state);
        Assert.AreEqual(BeatTiming.OnTime, press);

        judge.Release(TeatPosition.FrontLeft, 9.1f);
        Assert.AreEqual(NoteState.Done, note.state);
        Assert.AreEqual(BeatTiming.OnTime, release);
        Assert.AreEqual(BeatTiming.OnTime, note.releaseJudgment);
    }

    [Test]
    public void EarlyRelease_JudgesTooEarly()
    {
        var note = new Note(TeatPosition.BackRight, 8f, 1f);
        var judge = MakeJudge(note);
        BeatTiming? release = null;
        judge.OnReleaseJudged += (n, t) => release = t;

        judge.Press(TeatPosition.BackRight, 8f);
        judge.Release(TeatPosition.BackRight, 8.3f);

        Assert.AreEqual(BeatTiming.TooEarly, release);
        Assert.AreEqual(NoteState.Done, note.state);
    }

    [Test]
    public void LateRelease_JudgesTooLate()
    {
        var note = new Note(TeatPosition.BackRight, 8f, 1f);
        var judge = MakeJudge(note);
        BeatTiming? release = null;
        judge.OnReleaseJudged += (n, t) => release = t;

        judge.Press(TeatPosition.BackRight, 8f);
        judge.Release(TeatPosition.BackRight, 9.3f);

        Assert.AreEqual(BeatTiming.TooLate, release);
    }

    [Test]
    public void NeverPressed_BecomesMissedOnTick()
    {
        var note = new Note(TeatPosition.FrontRight, 8f, 1f);
        var judge = MakeJudge(note);
        Note missed = null;
        judge.OnMissed += n => missed = n;

        judge.Tick(8.39f);
        Assert.AreEqual(NoteState.Pending, note.state);
        Assert.IsNull(missed);

        judge.Tick(8.41f);
        Assert.AreEqual(NoteState.Missed, note.state);
        Assert.AreSame(note, missed);
    }

    [Test]
    public void StrayPress_IsIgnored()
    {
        var note = new Note(TeatPosition.FrontLeft, 8f, 1f);
        var judge = MakeJudge(note);
        int presses = 0;
        judge.OnPressJudged += (n, t) => presses++;

        judge.Press(TeatPosition.FrontLeft, 7.5f);

        Assert.AreEqual(0, presses);
        Assert.AreEqual(NoteState.Pending, note.state);
    }

    [Test]
    public void PressWhileCupIsVisible_BeforeHitWindow_JudgesTooEarly()
    {
        var note = new Note(TeatPosition.FrontLeft, 8f, 4f) { cueBeat = 6.5f };
        var judge = MakeJudge(note);
        BeatTiming? press = null;
        judge.OnPressJudged += (n, t) => press = t;

        judge.Press(TeatPosition.FrontLeft, 7f);

        Assert.AreEqual(BeatTiming.TooEarly, press);
        Assert.AreEqual(NoteState.Done, note.state);
    }

    [Test]
    public void PressBeforeCupIsVisible_IsIgnored()
    {
        var note = new Note(TeatPosition.FrontLeft, 8f, 4f) { cueBeat = 6.5f };
        var judge = MakeJudge(note);
        int presses = 0;
        judge.OnPressJudged += (n, t) => presses++;

        judge.Press(TeatPosition.FrontLeft, 6.4f);

        Assert.AreEqual(0, presses);
        Assert.AreEqual(NoteState.Pending, note.state);
    }

    [Test]
    public void PressInAnotherLane_IsIgnored()
    {
        var note = new Note(TeatPosition.FrontLeft, 8f, 1f);
        var judge = MakeJudge(note);
        int presses = 0;
        judge.OnPressJudged += (n, t) => presses++;

        judge.Press(TeatPosition.BackLeft, 8f);

        Assert.AreEqual(0, presses);
        Assert.AreEqual(NoteState.Pending, note.state);
    }

    [Test]
    public void DenseLane_PressAssociatesToNearestNote()
    {
        var first = new Note(TeatPosition.FrontLeft, 8f, 0.5f);
        var second = new Note(TeatPosition.FrontLeft, 8.5f, 0.5f);
        var judge = MakeJudge(first, second);

        judge.Press(TeatPosition.FrontLeft, 8.3f);

        Assert.AreEqual(NoteState.Pending, first.state);
        Assert.AreEqual(NoteState.Holding, second.state);
    }

    [Test]
    public void HeldPastReleaseWindow_AutoReleasesTooLate()
    {
        var note = new Note(TeatPosition.BackLeft, 8f, 1f);
        var judge = MakeJudge(note);
        BeatTiming? release = null;
        judge.OnReleaseJudged += (n, t) => release = t;

        judge.Press(TeatPosition.BackLeft, 8f);
        judge.Tick(9.39f);
        Assert.AreEqual(NoteState.Holding, note.state);
        Assert.IsNull(release);

        judge.Tick(9.41f);
        Assert.AreEqual(NoteState.Done, note.state);
        Assert.AreEqual(BeatTiming.TooLate, release);
    }

    [Test]
    public void ImperfectPress_TipsCupWithoutReleaseJudgment()
    {
        var note = new Note(TeatPosition.FrontRight, 8f, 1f);
        var judge = MakeJudge(note);
        BeatTiming? press = null;
        int releases = 0;
        judge.OnPressJudged += (n, t) => press = t;
        judge.OnReleaseJudged += (n, t) => releases++;

        judge.Press(TeatPosition.FrontRight, 8.3f);
        Assert.AreEqual(BeatTiming.TooLate, press);
        Assert.AreEqual(NoteState.Done, note.state);
        Assert.IsNull(note.releaseJudgment);

        judge.Release(TeatPosition.FrontRight, 9f);
        Assert.AreEqual(0, releases);
    }

    [Test]
    public void ImperfectPress_WhenAllowed_StartsHold()
    {
        var note = new Note(TeatPosition.FrontRight, 8f, 1f);
        var judge = MakeJudge(note);
        judge.failCupOnImperfectPress = false;

        judge.Press(TeatPosition.FrontRight, 8.3f);

        Assert.AreEqual(NoteState.Holding, note.state);
        Assert.AreEqual(BeatTiming.TooLate, note.pressJudgment);
    }

    [Test]
    public void MissedNote_DoesNotBlockLaterPressInSameLane()
    {
        var first = new Note(TeatPosition.BackLeft, 8f, 1f);
        var second = new Note(TeatPosition.BackLeft, 12f, 1f);
        var judge = MakeJudge(first, second);

        judge.Tick(9f);
        judge.Press(TeatPosition.BackLeft, 12f);

        Assert.AreEqual(NoteState.Missed, first.state);
        Assert.AreEqual(NoteState.Holding, second.state);
    }

    [Test]
    public void SecondPressWhileHolding_IsIgnored()
    {
        var first = new Note(TeatPosition.BackLeft, 8f, 4f);
        var second = new Note(TeatPosition.BackLeft, 8.2f, 1f);
        var judge = MakeJudge(first, second);
        int presses = 0;
        judge.OnPressJudged += (n, t) => presses++;

        judge.Press(TeatPosition.BackLeft, 8f);
        judge.Press(TeatPosition.BackLeft, 8.2f);

        Assert.AreEqual(1, presses);
        Assert.AreEqual(NoteState.Pending, second.state);
    }
}
