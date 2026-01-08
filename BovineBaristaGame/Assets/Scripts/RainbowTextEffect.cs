using UnityEngine;
using TMPro;

public class RainbowTextEffect : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Score required to activate rainbow effect (0 = always active)")]
    public int scoreThreshold = 5000;

    [Header("Apply To")]
    [Tooltip("Apply rainbow effect to text fill (per-character gradient)")]
    public bool applyToFill = true;
    [Tooltip("Apply rainbow effect to text outline (uniform color cycling)")]
    public bool applyToOutline = false;

    [Header("Rainbow Settings")]
    [Tooltip("How fast the rainbow moves across the text")]
    public float speed = 1f;
    [Tooltip("How spread out the colors are (lower = more colors visible at once) - only affects fill")]
    public float frequency = 0.5f;
    [Tooltip("Saturation of the rainbow colors (0-1)")]
    [Range(0f, 1f)]
    public float saturation = 1f;
    [Tooltip("Brightness of the rainbow colors (0-1)")]
    [Range(0f, 1f)]
    public float brightness = 1f;

    [Header("Outline Settings")]
    [Tooltip("Outline thickness when rainbow outline is active (0 = use existing)")]
    public float outlineThickness = 0.2f;

    private TMP_Text textComponent;
    private GameManager gameManager;
    private bool isActive = false;
    private float timeOffset = 0f;
    private Color originalColor;
    private Color originalOutlineColor;
    private float originalOutlineWidth;
    private bool wasActive = false;

    void Start()
    {
        textComponent = GetComponent<TMP_Text>();
        gameManager = FindObjectOfType<GameManager>();

        if (textComponent != null)
        {
            originalColor = textComponent.color;

            // Store original outline settings
            originalOutlineColor = textComponent.outlineColor;
            originalOutlineWidth = textComponent.outlineWidth;
        }
    }

    void Update()
    {
        if (textComponent == null) return;

        int currentScore = gameManager != null ? gameManager.CurrentScore : 0;
        isActive = currentScore >= scoreThreshold;

        if (isActive)
        {
            wasActive = true;
            timeOffset += Time.deltaTime * speed;

            if (applyToFill)
            {
                UpdateRainbowFill();
            }
            if (applyToOutline)
            {
                UpdateRainbowOutline();
            }
        }
        else if (wasActive)
        {
            // Reset to original colors when deactivated
            ResetColors();
            wasActive = false;
        }
    }

    void UpdateRainbowFill()
    {
        textComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = textComponent.textInfo;

        if (textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            // Calculate rainbow color based on character position and time
            float t = (i * frequency + timeOffset) % 1f;
            Color rainbowColor = GetRainbowColor(t);

            // Apply to all 4 vertices of the character
            vertexColors[vertexIndex + 0] = rainbowColor;
            vertexColors[vertexIndex + 1] = rainbowColor;
            vertexColors[vertexIndex + 2] = rainbowColor;
            vertexColors[vertexIndex + 3] = rainbowColor;
        }

        // Update the mesh with new colors
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    void UpdateRainbowOutline()
    {
        // Outline color cycles uniformly across all characters
        float t = timeOffset % 1f;
        Color rainbowColor = GetRainbowColor(t);

        // Set outline thickness if specified
        if (outlineThickness > 0)
        {
            textComponent.outlineWidth = outlineThickness;
        }

        // Apply rainbow color to outline
        textComponent.outlineColor = rainbowColor;
    }

    Color GetRainbowColor(float t)
    {
        // HSV to RGB for smooth rainbow
        // t goes from 0 to 1, mapping to hue 0-360
        return Color.HSVToRGB(t, saturation, brightness);
    }

    void ResetColors()
    {
        if (textComponent == null) return;

        // Reset fill colors
        if (applyToFill)
        {
            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;
                Color32 original = originalColor;

                vertexColors[vertexIndex + 0] = original;
                vertexColors[vertexIndex + 1] = original;
                vertexColors[vertexIndex + 2] = original;
                vertexColors[vertexIndex + 3] = original;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        // Reset outline color and width
        if (applyToOutline)
        {
            textComponent.outlineColor = originalOutlineColor;
            textComponent.outlineWidth = originalOutlineWidth;
        }
    }

    public void ForceActivate(bool active)
    {
        isActive = active;
        if (!active)
        {
            ResetColors();
        }
    }
}
