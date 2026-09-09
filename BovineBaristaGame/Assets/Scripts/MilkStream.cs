using UnityEngine;

/// <summary>
/// Milk pouring from a teat tip along the teat's own axis. The sprite hangs from its top-centre pivot
/// and is 9-sliced so the curl at the top and the flare at the bottom keep their shape at any length.
/// </summary>
public class MilkStream : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer splashRenderer;
    private Sprite[] frames;
    private Sprite[] splashFrames;
    private float scale;
    private float framesPerBeat;
    private float splashOffsetY;

    public static MilkStream Create(Sprite[] frames, Sprite[] splashFrames, float splashOffsetY, float scale, float framesPerBeat, int sortingLayerID, int sortingOrder)
    {
        GameObject streamObject = new GameObject("MilkStream");
        MilkStream stream = streamObject.AddComponent<MilkStream>();
        stream.frames = frames;
        stream.scale = scale;
        stream.framesPerBeat = framesPerBeat;
        stream.splashOffsetY = splashOffsetY;

        // The splash sits where the milk lands, upright and just behind the stream so it wraps around it
        if (splashFrames != null && splashFrames.Length > 0)
        {
            stream.splashFrames = splashFrames;
            GameObject splashObject = new GameObject("MilkSplash");
            stream.splashRenderer = splashObject.AddComponent<SpriteRenderer>();
            stream.splashRenderer.sprite = splashFrames[0];
            stream.splashRenderer.sortingLayerID = sortingLayerID;
            stream.splashRenderer.sortingOrder = sortingOrder - 1;
            splashObject.transform.localScale = new Vector3(scale, scale, 1f);
            splashObject.SetActive(false);
        }

        stream.spriteRenderer = streamObject.AddComponent<SpriteRenderer>();
        stream.spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        stream.spriteRenderer.sortingLayerID = sortingLayerID;
        stream.spriteRenderer.sortingOrder = sortingOrder;
        stream.spriteRenderer.sprite = frames[0];

        streamObject.transform.localScale = new Vector3(scale, scale, 1f);
        streamObject.SetActive(false);
        return stream;
    }

    public void Show(Vector3 tip, Vector2 direction, float worldLength, float beat)
    {
        gameObject.SetActive(true);

        int frameIndex = Mathf.FloorToInt(Mathf.Max(0f, beat) * framesPerBeat);
        Sprite frame = frames[frameIndex % frames.Length];
        spriteRenderer.sprite = frame;

        if (splashRenderer != null)
        {
            splashRenderer.gameObject.SetActive(true);
            splashRenderer.sprite = splashFrames[frameIndex % splashFrames.Length];
            splashRenderer.transform.position = tip + (Vector3)(direction.normalized * worldLength) + Vector3.up * splashOffsetY;
        }

        transform.position = tip;
        transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.down, direction));

        // Only the middle band stretches; a pour shorter than the two caps squashes the whole stream instead
        float width = frame.rect.width / frame.pixelsPerUnit;
        float capsLength = (frame.border.y + frame.border.w) / frame.pixelsPerUnit;
        float capsWorldLength = capsLength * scale;
        if (worldLength < capsWorldLength)
        {
            transform.localScale = new Vector3(scale, scale * worldLength / capsWorldLength, 1f);
            spriteRenderer.size = new Vector2(width, capsLength);
        }
        else
        {
            transform.localScale = new Vector3(scale, scale, 1f);
            spriteRenderer.size = new Vector2(width, worldLength / scale);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (splashRenderer != null) splashRenderer.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (splashRenderer != null) Destroy(splashRenderer.gameObject);
    }
}
