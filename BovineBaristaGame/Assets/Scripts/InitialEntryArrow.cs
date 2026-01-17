using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InitialEntryArrow : MonoBehaviour
{
    public enum ArrowDirection { Up, Down }

    [Header("Direction")]
    public ArrowDirection direction = ArrowDirection.Up;

    [Header("References")]
    public TMP_Text arrowText;

    [Header("Arrow Characters")]
    public string upCharacter = "▲";
    public string downCharacter = "▼";

    private LetterGroupController letterGroup;

    void Start()
    {
        // Find parent LetterGroupController
        letterGroup = GetComponentInParent<LetterGroupController>();

        // Set arrow text based on direction
        UpdateArrowDisplay();
    }

    void OnValidate()
    {
        // Update in editor when direction changes
        UpdateArrowDisplay();
    }

    private void UpdateArrowDisplay()
    {
        if (arrowText != null)
        {
            arrowText.text = (direction == ArrowDirection.Up) ? upCharacter : downCharacter;
        }
    }

    // Called by Button OnClick
    public void OnArrowPressed()
    {
        if (letterGroup == null)
        {
            letterGroup = GetComponentInParent<LetterGroupController>();
        }

        if (letterGroup != null)
        {
            if (direction == ArrowDirection.Up)
            {
                letterGroup.OnUpPressed();
            }
            else
            {
                letterGroup.OnDownPressed();
            }
        }
    }
}
