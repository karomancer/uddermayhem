using System;
using UnityEngine;
using TMPro;

public class InitialsEntryUI : MonoBehaviour
{
    [Header("Letter Displays")]
    public TMP_Text letter1Text;
    public TMP_Text letter2Text;
    public TMP_Text letter3Text;

    [Header("Visual Feedback")]
    public float selectedScale = 1.2f;
    public float normalScale = 1.0f;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    public event Action<string> OnInitialsSubmitted;

    private char[] letters = new char[3] { 'A', 'A', 'A' };
    private int lastTappedIndex = -1;
    private TMP_Text[] letterTexts;

    void Start()
    {
        letterTexts = new TMP_Text[] { letter1Text, letter2Text, letter3Text };
        UpdateDisplay();
    }

    public void CycleLetter(int index)
    {
        if (index < 0 || index > 2) return;

        letters[index]++;
        if (letters[index] > 'Z') letters[index] = 'A';

        lastTappedIndex = index;
        UpdateDisplay();
    }

    public void CycleLetterBack(int index)
    {
        if (index < 0 || index > 2) return;

        letters[index]--;
        if (letters[index] < 'A') letters[index] = 'Z';

        lastTappedIndex = index;
        UpdateDisplay();
    }

    // Called by Up arrow buttons
    public void OnLetter1Up() => CycleLetterBack(0);
    public void OnLetter2Up() => CycleLetterBack(1);
    public void OnLetter3Up() => CycleLetterBack(2);

    // Called by Down arrow buttons
    public void OnLetter1Down() => CycleLetter(0);
    public void OnLetter2Down() => CycleLetter(1);
    public void OnLetter3Down() => CycleLetter(2);

    // Called by letter tap
    public void OnLetter1Tap() => CycleLetter(0);
    public void OnLetter2Tap() => CycleLetter(1);
    public void OnLetter3Tap() => CycleLetter(2);

    public void OnSubmit()
    {
        string initials = new string(letters);
        OnInitialsSubmitted?.Invoke(initials);
    }

    public string GetInitials()
    {
        return new string(letters);
    }

    public void ResetToDefault()
    {
        letters = new char[] { 'A', 'A', 'A' };
        lastTappedIndex = -1;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < 3; i++)
        {
            if (letterTexts[i] != null)
            {
                letterTexts[i].text = letters[i].ToString();

                // Highlight last tapped letter
                bool isSelected = (i == lastTappedIndex);
                letterTexts[i].color = isSelected ? selectedColor : normalColor;

                float scale = isSelected ? selectedScale : normalScale;
                letterTexts[i].transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
