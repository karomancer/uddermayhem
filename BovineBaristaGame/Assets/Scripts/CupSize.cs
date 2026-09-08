using UnityEngine;

/// <summary>
/// Cup art for one hold length. Create via: Right-click → Create → BovineBarista → Cup Size
/// </summary>
[System.Serializable]
public struct PreHitFrame
{
    public Sprite sprite;
    [Tooltip("How long this frame shows, in beats")]
    public float beats;
}

[CreateAssetMenu(fileName = "CupSize", menuName = "BovineBarista/Cup Size")]
public class CupSize : ScriptableObject
{
    [Tooltip("Hold length this cup represents, in beats (1 = quarter note)")]
    public float holdBeats = 1f;
    [Tooltip("Beats before its note that this cup arrives; <= 0 uses CupConductor.arriveLeadBeats")]
    public float arriveLeadBeatsOverride = 0f;
    [Tooltip("Extra scale for this size's art on top of CupConductor.cupScale")]
    public float scaleMultiplier = 1f;

    [Header("Single-Sprite Frames (used when no layers are assigned)")]
    public Sprite defaultCup;
    [Tooltip("Fill stages shown across the hold, in order; N frames split the hold into N+1 even steps")]
    public Sprite[] inProgress;
    public Sprite perfectCup;
    public Sprite overfilledCup;
    [Tooltip("Also used by the layered cup")]
    public Sprite tippedCup;

    [Header("Layers (all exported from the same artboard)")]
    public Sprite glassBack;
    [Tooltip("Silhouette of the bowl interior; the liquid is clipped to it")]
    public Sprite interiorMask;
    [Tooltip("Liquid seen through the side of a glass; leave empty for an opaque mug")]
    public Sprite liquidBody;
    public Sprite liquidSurface;
    public Sprite latteArt;
    public Sprite glassFront;
    public Sprite overflow;

    [Header("Whole-Cup Motion Frames (optional)")]
    [Tooltip("Shown while the cup slides in from the barista")]
    public Sprite slideInCup;
    [Tooltip("Whole-cup frames leading into the hit, in order; the last one ends exactly on the hit. Leave empty to show the resting cup from arrival until the hit")]
    public PreHitFrame[] preHitFrames;

    [Header("Fill")]
    [Tooltip("Visible fill steps across the hold; the liquid jumps between them on beat subdivisions")]
    public int fillSteps = 2;
    [Tooltip("Surface centre y when full, in sprite units (artboard px from centre / PPU)")]
    public float surfaceFullY = 0f;
    [Tooltip("Surface centre y for the resting cup (the espresso shot showing at the lip before any milk)")]
    public float surfaceEmptyY = 0f;
    [Tooltip("Surface x scale at level 0, for bowls narrower at the bottom")]
    public float surfaceScaleAtBottom = 1f;
    [Tooltip("Clip the surface to the interior too (hides its edges if it is wider than the bowl)")]
    public bool clipSurfaceToInterior = false;
    [Tooltip("Body colour by fill level (0 = first drop, 1 = full); body art should be white or grey")]
    public Gradient liquidTint = EspressoToLatte();
    [Tooltip("Surface colour multiplier by fill level; white keeps the art's own colours")]
    public Gradient surfaceTint = DarkToUnchanged();

    // Frames are anchored to the hit, so the sequence's end lands on the beat whatever the arrival time
    public Sprite PreHitFrameAt(float beatsBeforeHit)
    {
        if (preHitFrames == null || beatsBeforeHit < 0f) return null;

        float end = 0f;
        for (int i = preHitFrames.Length - 1; i >= 0; i--)
        {
            float start = end + preHitFrames[i].beats;
            if (beatsBeforeHit >= end && beatsBeforeHit < start) return preHitFrames[i].sprite;
            end = start;
        }
        return null;
    }

    public bool HasLayers =>
        glassBack != null && interiorMask != null && liquidSurface != null && glassFront != null;

    public float QuantizedLevel(float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        if (fillSteps <= 0) return clamped;
        return Mathf.Floor(clamped * fillSteps) / fillSteps;
    }

    public Sprite InProgressAt(float progress)
    {
        if (inProgress == null || inProgress.Length == 0) return defaultCup;

        int step = Mathf.FloorToInt(progress * (inProgress.Length + 1)) - 1;
        if (step < 0) return defaultCup;
        return inProgress[Mathf.Min(step, inProgress.Length - 1)];
    }

    private static Gradient EspressoToLatte()
    {
        return TwoKeyGradient(new Color(0.24f, 0.12f, 0.05f), new Color(0.65f, 0.38f, 0.20f));
    }

    private static Gradient DarkToUnchanged()
    {
        return TwoKeyGradient(new Color(0.45f, 0.45f, 0.45f), Color.white);
    }

    private static Gradient TwoKeyGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }
}
