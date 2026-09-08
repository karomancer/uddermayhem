using UnityEngine;

/// <summary>
/// Cup art for one hold length. Create via: Right-click → Create → BovineBarista → Cup Size
/// </summary>
[CreateAssetMenu(fileName = "CupSize", menuName = "BovineBarista/Cup Size")]
public class CupSize : ScriptableObject
{
    [Tooltip("Hold length this cup represents, in beats (1 = quarter note)")]
    public float holdBeats = 1f;
    [Tooltip("Beats before its note that this cup arrives; <= 0 uses CupConductor.arriveLeadBeats")]
    public float arriveLeadBeatsOverride = 0f;

    [Header("Sprites")]
    public Sprite defaultCup;
    [Tooltip("Fill stages shown across the hold, in order; N frames split the hold into N+1 even steps")]
    public Sprite[] inProgress;
    public Sprite perfectCup;
    public Sprite overfilledCup;
    public Sprite tippedCup;

    public Sprite InProgressAt(float progress)
    {
        if (inProgress == null || inProgress.Length == 0) return defaultCup;

        int step = Mathf.FloorToInt(progress * (inProgress.Length + 1)) - 1;
        if (step < 0) return defaultCup;
        return inProgress[Mathf.Min(step, inProgress.Length - 1)];
    }
}
