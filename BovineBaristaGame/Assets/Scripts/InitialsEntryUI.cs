using System;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Input")]
    public float inputRepeatDelay = 0.3f;  // Time before input starts repeating
    public float inputRepeatRate = 0.1f;   // Time between repeated inputs

    public event Action<string> OnInitialsSubmitted;

    private char[] letters = new char[3] { 'A', 'A', 'A' };
    private int selectedIndex = 0;  // Currently selected letter position
    private TMP_Text[] letterTexts;

    // Input repeat tracking
    private float verticalHoldTime = 0f;
    private float horizontalHoldTime = 0f;
    private float lastVerticalRepeat = 0f;
    private float lastHorizontalRepeat = 0f;
    private int lastVerticalDirection = 0;
    private int lastHorizontalDirection = 0;

    void Start()
    {
        letterTexts = new TMP_Text[] { letter1Text, letter2Text, letter3Text };
        selectedIndex = 0;
        UpdateDisplay();
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        Vector2 navigation = Vector2.zero;
        bool submitPressed = false;

        // New Input System
        if (InputManager.Instance != null)
        {
            navigation = InputManager.Instance.Navigate.ReadValue<Vector2>();
            submitPressed = InputManager.Instance.Submit.WasPressedThisFrame();
        }
        // Legacy fallback
        else
        {
            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) h = -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h = 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) v = 1f;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) v = -1f;
            navigation = new Vector2(h, v);
            submitPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        }

        // Handle horizontal navigation (left/right between letters)
        int hDir = navigation.x < -0.5f ? -1 : (navigation.x > 0.5f ? 1 : 0);
        if (hDir != 0)
        {
            if (hDir != lastHorizontalDirection)
            {
                // Direction changed - move immediately
                MoveSelection(hDir);
                horizontalHoldTime = 0f;
                lastHorizontalRepeat = 0f;
            }
            else
            {
                // Same direction held
                horizontalHoldTime += Time.deltaTime;
                if (horizontalHoldTime > inputRepeatDelay)
                {
                    if (Time.time - lastHorizontalRepeat > inputRepeatRate)
                    {
                        MoveSelection(hDir);
                        lastHorizontalRepeat = Time.time;
                    }
                }
            }
        }
        else
        {
            horizontalHoldTime = 0f;
        }
        lastHorizontalDirection = hDir;

        // Handle vertical navigation (up/down to change letter)
        int vDir = navigation.y > 0.5f ? 1 : (navigation.y < -0.5f ? -1 : 0);
        if (vDir != 0)
        {
            if (vDir != lastVerticalDirection)
            {
                // Direction changed - cycle immediately
                if (vDir > 0) CycleLetterBack(selectedIndex);
                else CycleLetter(selectedIndex);
                verticalHoldTime = 0f;
                lastVerticalRepeat = 0f;
            }
            else
            {
                // Same direction held
                verticalHoldTime += Time.deltaTime;
                if (verticalHoldTime > inputRepeatDelay)
                {
                    if (Time.time - lastVerticalRepeat > inputRepeatRate)
                    {
                        if (vDir > 0) CycleLetterBack(selectedIndex);
                        else CycleLetter(selectedIndex);
                        lastVerticalRepeat = Time.time;
                    }
                }
            }
        }
        else
        {
            verticalHoldTime = 0f;
        }
        lastVerticalDirection = vDir;

        // Handle submit - advance to next letter, or submit on last letter
        if (submitPressed)
        {
            if (selectedIndex < 2)
            {
                // Move to next letter
                selectedIndex++;
                UpdateDisplay();
            }
            else
            {
                // On last letter - submit
                OnSubmit();
            }
        }
    }

    private void MoveSelection(int direction)
    {
        selectedIndex += direction;
        if (selectedIndex < 0) selectedIndex = 2;
        if (selectedIndex > 2) selectedIndex = 0;
        UpdateDisplay();
    }

    public void CycleLetter(int index)
    {
        if (index < 0 || index > 2) return;

        letters[index]++;
        if (letters[index] > 'Z') letters[index] = 'A';

        selectedIndex = index;
        UpdateDisplay();
    }

    public void CycleLetterBack(int index)
    {
        if (index < 0 || index > 2) return;

        letters[index]--;
        if (letters[index] < 'A') letters[index] = 'Z';

        selectedIndex = index;
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
        selectedIndex = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < 3; i++)
        {
            if (letterTexts[i] != null)
            {
                letterTexts[i].text = letters[i].ToString();

                // Highlight selected letter
                bool isSelected = (i == selectedIndex);
                letterTexts[i].color = isSelected ? selectedColor : normalColor;

                float scale = isSelected ? selectedScale : normalScale;
                letterTexts[i].transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
