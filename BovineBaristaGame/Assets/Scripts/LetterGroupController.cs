using UnityEngine;
using TMPro;

public class LetterGroupController : MonoBehaviour
{
    [Header("Which letter this group controls (0, 1, or 2)")]
    public int letterIndex = 0;

    [Header("References")]
    public TMP_Text letterText;

    private InitialsEntryUI parentUI;

    void Start()
    {
        // Find parent InitialsEntryUI
        parentUI = GetComponentInParent<InitialsEntryUI>();
        if (parentUI == null)
        {
            parentUI = FindObjectOfType<InitialsEntryUI>();
        }
    }

    // Called by Up arrow button OnClick
    public void OnUpPressed()
    {
        if (parentUI != null)
        {
            parentUI.CycleLetterBack(letterIndex);
        }
    }

    // Called by Down arrow button OnClick
    public void OnDownPressed()
    {
        if (parentUI != null)
        {
            parentUI.CycleLetter(letterIndex);
        }
    }

    // Called by Letter tap
    public void OnLetterTapped()
    {
        if (parentUI != null)
        {
            parentUI.CycleLetter(letterIndex);
        }
    }

    // For swipe support
    public void OnPointerDown()
    {
        if (parentUI != null)
        {
            parentUI.OnLetterPointerDown(letterIndex);
        }
    }

    public void OnPointerUp()
    {
        if (parentUI != null)
        {
            parentUI.OnLetterPointerUp(letterIndex);
        }
    }

    public void OnDrag()
    {
        if (parentUI != null)
        {
            parentUI.OnLetterDrag(letterIndex);
        }
    }
}
