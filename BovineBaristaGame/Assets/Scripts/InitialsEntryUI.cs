using System;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Swipe Settings")]
    public float swipeThreshold = 20f;           // Min pixels to register as swipe
    public float velocityMultiplier = 0.05f;     // How much velocity affects spin
    public float spinDecay = 0.92f;              // How quickly spin slows down
    public float letterChangeThreshold = 30f;   // Pixels of spin per letter change

    public event Action<string> OnInitialsSubmitted;

    private char[] letters = new char[3] { 'A', 'A', 'A' };
    private int lastTappedIndex = -1;
    private TMP_Text[] letterTexts;

    // Swipe/spin tracking
    private int activeSwipeIndex = -1;
    private Vector2 swipeStartPos;
    private Vector2 lastSwipePos;
    private float swipeStartTime;
    private float[] spinVelocity = new float[3];
    private float[] spinAccumulator = new float[3];

    void Start()
    {
        letterTexts = new TMP_Text[] { letter1Text, letter2Text, letter3Text };
        UpdateDisplay();
    }

    void Update()
    {
        // Handle spin momentum for each letter
        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(spinVelocity[i]) > 0.1f)
            {
                spinAccumulator[i] += spinVelocity[i] * Time.deltaTime * 60f;
                spinVelocity[i] *= spinDecay;

                // Change letter when accumulator crosses threshold
                while (spinAccumulator[i] >= letterChangeThreshold)
                {
                    spinAccumulator[i] -= letterChangeThreshold;
                    CycleLetterInternal(i, 1);
                }
                while (spinAccumulator[i] <= -letterChangeThreshold)
                {
                    spinAccumulator[i] += letterChangeThreshold;
                    CycleLetterInternal(i, -1);
                }
            }
            else
            {
                spinVelocity[i] = 0f;
                spinAccumulator[i] = 0f;
            }
        }
    }

    // Internal cycle without stopping momentum
    private void CycleLetterInternal(int index, int direction)
    {
        if (index < 0 || index > 2) return;

        letters[index] = (char)(letters[index] + direction);
        if (letters[index] > 'Z') letters[index] = 'A';
        if (letters[index] < 'A') letters[index] = 'Z';

        lastTappedIndex = index;
        UpdateDisplay();
    }

    public void CycleLetter(int index)
    {
        if (index < 0 || index > 2) return;
        spinVelocity[index] = 0f; // Stop any momentum
        CycleLetterInternal(index, 1);
    }

    public void CycleLetterBack(int index)
    {
        if (index < 0 || index > 2) return;
        spinVelocity[index] = 0f; // Stop any momentum
        CycleLetterInternal(index, -1);
    }

    // Called by Up arrow buttons
    public void OnLetter1Up() => CycleLetterBack(0);
    public void OnLetter2Up() => CycleLetterBack(1);
    public void OnLetter3Up() => CycleLetterBack(2);

    // Called by Down arrow buttons
    public void OnLetter1Down() => CycleLetter(0);
    public void OnLetter2Down() => CycleLetter(1);
    public void OnLetter3Down() => CycleLetter(2);

    // Called by letter tap (still works as single increment)
    public void OnLetter1Tap() => CycleLetter(0);
    public void OnLetter2Tap() => CycleLetter(1);
    public void OnLetter3Tap() => CycleLetter(2);

    // Swipe handling - call these from EventTrigger components
    public void OnLetterPointerDown(int index)
    {
        activeSwipeIndex = index;
        swipeStartPos = GetPointerPosition();
        lastSwipePos = swipeStartPos;
        swipeStartTime = Time.time;
        spinVelocity[index] = 0f; // Stop existing momentum
    }

    public void OnLetterPointerUp(int index)
    {
        if (activeSwipeIndex != index) return;

        Vector2 endPos = GetPointerPosition();
        float swipeDistance = endPos.y - swipeStartPos.y;
        float swipeTime = Time.time - swipeStartTime;

        if (Mathf.Abs(swipeDistance) < swipeThreshold)
        {
            // Too short - treat as tap
            CycleLetter(index);
        }
        else if (swipeTime > 0.01f)
        {
            // Apply velocity for momentum spin
            float velocity = (swipeDistance / swipeTime) * velocityMultiplier;
            spinVelocity[index] = -velocity; // Negative because swipe up = earlier letter
        }

        activeSwipeIndex = -1;
    }

    public void OnLetterDrag(int index)
    {
        if (activeSwipeIndex != index) return;

        Vector2 currentPos = GetPointerPosition();
        float delta = lastSwipePos.y - currentPos.y;

        spinAccumulator[index] += delta;

        // Change letter when dragged past threshold
        while (spinAccumulator[index] >= letterChangeThreshold)
        {
            spinAccumulator[index] -= letterChangeThreshold;
            CycleLetterInternal(index, 1);
        }
        while (spinAccumulator[index] <= -letterChangeThreshold)
        {
            spinAccumulator[index] += letterChangeThreshold;
            CycleLetterInternal(index, -1);
        }

        lastSwipePos = currentPos;
    }

    // Convenience methods for EventTrigger (since it can't pass parameters easily)
    public void OnLetter1PointerDown() => OnLetterPointerDown(0);
    public void OnLetter2PointerDown() => OnLetterPointerDown(1);
    public void OnLetter3PointerDown() => OnLetterPointerDown(2);

    public void OnLetter1PointerUp() => OnLetterPointerUp(0);
    public void OnLetter2PointerUp() => OnLetterPointerUp(1);
    public void OnLetter3PointerUp() => OnLetterPointerUp(2);

    public void OnLetter1Drag() => OnLetterDrag(0);
    public void OnLetter2Drag() => OnLetterDrag(1);
    public void OnLetter3Drag() => OnLetterDrag(2);

    private Vector2 GetPointerPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        return Input.mousePosition;
    }

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
