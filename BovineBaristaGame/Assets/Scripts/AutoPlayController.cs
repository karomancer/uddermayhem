using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles automatic note hitting during attract mode gameplay.
/// Watches for cups and triggers the corresponding teats at the right timing.
/// </summary>
public class AutoPlayController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Enable autoplay regardless of attract mode")]
    public bool forceAutoPlay = false;

    [Header("Timing")]
    public float fadeOutTime = 25f;  // Seconds into song before fading out

    private GameManager gameManager;
    private Dictionary<string, TeatController> teatControllers;
    private HashSet<int> processedCups = new HashSet<int>();
    private bool isActive = false;
    private float songStartTime;

    void Start()
    {
        // Only active during attract mode tutorial phase, unless forceAutoPlay is enabled
        bool inAttractTutorial = AttractModeManager.IsAttractModeActive &&
            AttractModeManager.CurrentPhase == AttractModeManager.AttractPhase.Tutorial;

        if (!forceAutoPlay && !inAttractTutorial)
        {
            enabled = false;
            return;
        }

        isActive = true;

        // Get GameManager reference
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj != null)
        {
            gameManager = gmObj.GetComponent<GameManager>();
        }

        // Build teat controller lookup
        teatControllers = new Dictionary<string, TeatController>();
        TeatController[] teats = FindObjectsOfType<TeatController>();
        foreach (var teat in teats)
        {
            // Extract teat type from name (e.g., "Teat_BackLeft" -> "BackLeft")
            string teatName = teat.gameObject.name;
            if (teatName.Contains("_"))
            {
                string teatType = teatName.Split('_')[1];
                teatControllers[teatType] = teat;
            }
        }

        Debug.Log($"AutoPlayController: Found {teatControllers.Count} teat controllers");

        // Start fade-out timer only if in attract mode (not for forceAutoPlay)
        if (AttractModeManager.IsAttractModeActive)
        {
            StartCoroutine(FadeOutAfterDelay(fadeOutTime));
        }
    }

    void Update()
    {
        if (!isActive || gameManager == null) return;

        float currentBeat = gameManager.songPositionInBeats;
        AutoHitNotesInWindow(currentBeat);
    }

    private void AutoHitNotesInWindow(float currentBeat)
    {
        // Find all active cups in the scene
        CoffeeController[] activeCups = FindObjectsOfType<CoffeeController>();

        foreach (var cup in activeCups)
        {
            // Skip if we've already handled this cup
            int cupId = cup.GetInstanceID();
            if (processedCups.Contains(cupId)) continue;

            // Calculate the target beat for this cup
            float targetBeat = (cup.measure * 4) + cup.beatInMeasure - 1;

            // Check if we're at the right time to hit (within a small window)
            float timingWindow = 0.15f;  // Slightly early to ensure we catch it
            if (currentBeat >= targetBeat - timingWindow && currentBeat <= targetBeat + timingWindow)
            {
                processedCups.Add(cupId);

                // Find and activate the correct teat
                string cupType = cup.CupTagName;
                if (cupType != null && teatControllers.ContainsKey(cupType))
                {
                    TeatController teat = teatControllers[cupType];
                    StartCoroutine(SimulateTeatPress(teat, cup.duration));
                }
            }
        }
    }

    private IEnumerator SimulateTeatPress(TeatController teat, float noteDuration)
    {
        // Press the teat
        teat.SetSqueezing(true);

        // Hold for duration minus small buffer (release after cup enters but before it exits)
        float holdTime = noteDuration * CupConductor.SecPerBeat * 0.8f;
        yield return new WaitForSeconds(holdTime);

        // Release the teat
        teat.SetSqueezing(false);
    }

    private IEnumerator FadeOutAfterDelay(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            // Check if attract mode was cancelled by input
            if (!AttractModeManager.IsAttractModeActive)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Time's up - end attract mode and return to title
        Debug.Log("AutoPlayController: Fade out timer expired, ending attract mode");
        if (AttractModeManager.IsAttractModeActive && AttractModeManager.Instance != null)
        {
            AttractModeManager.Instance.EndAttractMode();
        }
    }

    void OnDestroy()
    {
        // Clean up any pending coroutines
        StopAllCoroutines();
    }
}
