using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Reusable barista controller for speech bubbles, animations, and blinking.
/// Can be used by TutorialManager, EndScreenManager, or any other scene.
/// </summary>
public class BaristaController : MonoBehaviour
{
    [Header("References")]
    public GameObject baristaObject;
    public GameObject speechBubble;
    public TMP_Text messageText;

    [Header("Settings")]
    public float defaultPauseBetweenMessages = 1.5f;

    // Events
    public event Action OnFinishedSpeaking;

    private Animator baristaAnimator;
    private AudioSource speechBubbleSound;
    private float speechBubbleSoundLength;
    private Coroutine currentSpeechCoroutine;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        if (baristaObject != null)
        {
            baristaAnimator = baristaObject.GetComponent<Animator>();
        }

        if (speechBubble != null)
        {
            speechBubbleSound = speechBubble.GetComponent<AudioSource>();
            if (speechBubbleSound != null && speechBubbleSound.clip != null)
            {
                speechBubbleSoundLength = speechBubbleSound.clip.length;
            }
            speechBubble.SetActive(false);
        }
    }

    void OnEnable()
    {
        StartBlinking();
    }

    void OnDisable()
    {
        StopBlinking();
    }

    #region Public Methods

    /// <summary>
    /// Say a single message with optional callback when done.
    /// </summary>
    public void Say(string message, Action onComplete = null)
    {
        if (currentSpeechCoroutine != null)
        {
            StopCoroutine(currentSpeechCoroutine);
        }
        currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(new string[] { message }, onComplete));
    }

    /// <summary>
    /// Say multiple messages in sequence with optional callback when all done.
    /// </summary>
    public void SayMultiple(string[] messages, Action onComplete = null)
    {
        if (currentSpeechCoroutine != null)
        {
            StopCoroutine(currentSpeechCoroutine);
        }
        currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(messages, onComplete));
    }

    /// <summary>
    /// Stop speaking immediately and hide speech bubble.
    /// </summary>
    public void ShutUp()
    {
        if (currentSpeechCoroutine != null)
        {
            StopCoroutine(currentSpeechCoroutine);
            currentSpeechCoroutine = null;
        }
        StopTalkingAnimation();
        HideSpeechBubble();
    }

    /// <summary>
    /// Show or hide the barista.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (baristaObject != null)
        {
            baristaObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Start random blinking.
    /// </summary>
    public void StartBlinking()
    {
        if (blinkCoroutine == null)
        {
            blinkCoroutine = StartCoroutine(BlinkCoroutine());
        }
    }

    /// <summary>
    /// Stop blinking.
    /// </summary>
    public void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (baristaAnimator != null)
        {
            baristaAnimator.SetBool("isBlinking", false);
        }
    }

    #endregion

    #region Private Methods

    private IEnumerator SpeakCoroutine(string[] messages, Action onComplete)
    {
        StopBlinking();

        for (int i = 0; i < messages.Length; i++)
        {
            string message = messages[i];
            float talkTime = message.Length * 0.03f;

            // Show speech bubble and set text
            ShowSpeechBubble(message);
            StartTalkingAnimation();
            PlaySpeechSound(talkTime);

            // Wait for talking to finish
            yield return new WaitForSeconds(talkTime);

            StopTalkingAnimation();

            // Pause between messages (but not after the last one)
            if (i < messages.Length - 1)
            {
                yield return new WaitForSeconds(defaultPauseBetweenMessages);
            }
        }

        // Keep speech bubble visible briefly after last message
        yield return new WaitForSeconds(defaultPauseBetweenMessages);

        HideSpeechBubble();
        StartBlinking();

        currentSpeechCoroutine = null;
        onComplete?.Invoke();
        OnFinishedSpeaking?.Invoke();
    }

    private IEnumerator BlinkCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 6f));

            if (baristaAnimator != null)
            {
                baristaAnimator.SetBool("isBlinking", true);
                yield return new WaitForSeconds(0.15f);
                baristaAnimator.SetBool("isBlinking", false);
            }
        }
    }

    private void ShowSpeechBubble(string message)
    {
        if (speechBubble != null)
        {
            speechBubble.SetActive(true);
        }
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private void HideSpeechBubble()
    {
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    private void StartTalkingAnimation()
    {
        if (baristaAnimator != null)
        {
            baristaAnimator.SetBool("isTalking", true);
        }
    }

    private void StopTalkingAnimation()
    {
        if (baristaAnimator != null)
        {
            baristaAnimator.SetBool("isTalking", false);
        }
        if (speechBubbleSound != null)
        {
            speechBubbleSound.Stop();
        }
    }

    private void PlaySpeechSound(float duration)
    {
        if (speechBubbleSound != null && speechBubbleSound.clip != null)
        {
            float maxStartTime = Mathf.Max(0, speechBubbleSoundLength - duration);
            speechBubbleSound.time = UnityEngine.Random.Range(0f, maxStartTime);
            speechBubbleSound.Play();
        }
    }

    #endregion
}
