using UnityEngine;

[System.Serializable]
public struct TimingWindows
{
    [Tooltip("A press within this many seconds of the note start is OnTime")]
    public float pressPerfect;
    [Tooltip("A press within this many seconds of the note start belongs to it (Early/Late beyond perfect); further away is ignored")]
    public float pressHit;
    [Tooltip("A release within this many seconds of the note end is OnTime")]
    public float releasePerfect;
    [Tooltip("Holding past the note end by more than this auto-releases as TooLate")]
    public float releaseHit;

    public static TimingWindows Default => new TimingWindows
    {
        pressPerfect = 0.25f,
        pressHit = 0.4f,
        releasePerfect = 0.25f,
        releaseHit = 0.4f
    };
}
