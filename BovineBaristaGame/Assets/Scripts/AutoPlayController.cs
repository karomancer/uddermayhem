using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles automatic note hitting during attract mode gameplay.
/// Squeezes each lane's teat exactly for the duration of its notes.
/// </summary>
public class AutoPlayController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Enable autoplay regardless of attract mode")]
    public bool forceAutoPlay = false;

    [Header("Timing")]
    public float fadeOutTime = 25f;  // Seconds into song before fading out

    private GameManager gameManager;
    private CupConductor cupConductor;
    private readonly TeatController[] teatsByLane = new TeatController[4];
    private readonly List<Note> dueNotes = new List<Note>();
    private int nextNoteIndex = 0;
    private bool isActive = false;

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

        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj != null)
        {
            gameManager = gmObj.GetComponent<GameManager>();
            cupConductor = gmObj.GetComponent<CupConductor>();
        }

        foreach (TeatController teat in FindObjectsOfType<TeatController>())
        {
            teatsByLane[(int)teat.teatPosition] = teat;
        }

        // Start fade-out timer only if in attract mode (not for forceAutoPlay)
        if (AttractModeManager.IsAttractModeActive)
        {
            StartCoroutine(FadeOutAfterDelay(fadeOutTime));
        }
    }

    void Update()
    {
        if (!isActive || gameManager == null || gameManager.Judge == null) return;

        float currentBeat = gameManager.songPositionInBeats;
        IReadOnlyList<Note> notes = cupConductor.Notes;

        while (nextNoteIndex < notes.Count && notes[nextNoteIndex].startBeat <= currentBeat)
        {
            dueNotes.Add(notes[nextNoteIndex]);
            nextNoteIndex++;
        }

        // A lane still releasing its previous note retries next frame instead of skipping the note
        for (int i = dueNotes.Count - 1; i >= 0; i--)
        {
            Note note = dueNotes[i];
            if (note.state != NoteState.Pending)
            {
                dueNotes.RemoveAt(i);
                continue;
            }

            TeatController teat = teatsByLane[(int)note.lane];
            if (teat == null || teat.IsSqueezing) continue;

            StartCoroutine(HoldNote(teat, note));
            dueNotes.RemoveAt(i);
        }
    }

    private IEnumerator HoldNote(TeatController teat, Note note)
    {
        teat.SetSqueezing(true);

        while (gameManager.songPositionInBeats < note.endBeat)
        {
            yield return null;
        }

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
