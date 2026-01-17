using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    private int step = 0;
    public string[] introPopups;
    private int introPopupIndex = 0;

    [Header("Udder/Keyboard Tutorial")]
    public string[] handPopups;
    private int handPopupIndex = 0;

    [Header("Gamepad Tutorial")]
    [Tooltip("Messages shown to gamepad users instead of hand demo")]
    public string[] gamepadPopups;
    private int gamepadPopupIndex = 0;

    [Header("Button Prompt Overlays")]
    [Tooltip("Text overlays for gamepad button prompts on teats (BackLeft, BackRight, FrontLeft, FrontRight)")]
    public GameObject[] buttonPrompts; // Should match teats array order
    [Tooltip("How long to show button prompts before starting the game")]
    public float buttonPromptDisplayTime = 4f;

    public string[] cowPopups;
    private int cowPopupIndex = 0;

    public string[] rhythmPopups;
    private int rhythmPopupIndex = 0;

    // Track which input type we're showing tutorial for
    private bool isGamepadTutorial = false;

    public GameObject cow;
    public GameObject[] teats; // backleft, backright, frontleft, frontright
    public float cowEndingPositionY;
    private bool cowIsInView = false;

    public GameObject speechBubble;
    private AudioSource speechBubbleSound;
    private float speechBubbleSoundLength;

    public GameObject rightHand;
    private Animator rightHandAnimator;
    private SpriteRenderer rightHandSprite;
    public GameObject leftHand;
    private Animator leftHandAnimator;
    private SpriteRenderer leftHandSprite;

    public TMP_Text message;

    public GameObject barista;
    public GameObject skipButton;

    private AudioSource metronome;

    private Animator baristaAnimator;

    private float shutUpTime = 1.5f;

    private GameManager gameManager;
    private CupConductor cupConductor;

    [Header("Tutorial Timing")]
    [Tooltip("Minimum seconds between tutorial end and first cup arrival")]
    public float bufferBeforeFirstCup = 2f;

    // Start is called before the first frame update
    void Start()
    {
        metronome = GetComponent<AudioSource>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        cupConductor = gameManager.GetComponent<CupConductor>();

        // Check if tutorial should be skipped based on level config
        if (!ShouldShowTutorial())
        {
            SkipTutorialImmediate();
            return;
        }

        baristaAnimator = barista.GetComponent<Animator>();

        // Detect input type for tutorial fork
        if (InputManager.Instance != null)
        {
            isGamepadTutorial = InputManager.Instance.IsUsingGamepad();
            Debug.Log($"Tutorial mode: {(isGamepadTutorial ? "Gamepad" : "Udder/Keyboard")}");
        }

        speechBubbleSound = speechBubble.GetComponent<AudioSource>();
        speechBubbleSoundLength = speechBubbleSound.clip.length;
        speechBubble.SetActive(false);

        rightHandAnimator = rightHand.GetComponent<Animator>();
        rightHandSprite = rightHand.GetComponent<SpriteRenderer>();
        leftHandAnimator = leftHand.GetComponent<Animator>();
        leftHandSprite = leftHand.GetComponent<SpriteRenderer>();
        rightHand.SetActive(false);
        leftHand.SetActive(false);

        // Hide button prompts at start
        HideButtonPrompts();

        introPopupIndex = 0;

        // Hide skip button during attract mode
        if (AttractModeManager.IsAttractModeActive && skipButton != null)
        {
            skipButton.SetActive(false);
        }

        Invoke("Blink", 0.5f);
        Invoke("Talk", 1f);

        teats[0].GetComponent<AudioSource>().Play();
        speechBubbleSound.Stop();
    }

    private bool ShouldShowTutorial()
    {
        // If no config is set, default to showing tutorial
        if (GameManager.currentLevelConfig == null)
        {
            return true;
        }
        return GameManager.currentLevelConfig.showTutorial;
    }

    // Skip tutorial without any animation - for non-tutorial levels
    private void SkipTutorialImmediate()
    {
        Debug.Log("SkipTutorialImmediate called - skipping tutorial");

        // Hide tutorial UI elements
        if (speechBubble != null) speechBubble.SetActive(false);
        if (rightHand != null) rightHand.SetActive(false);
        if (leftHand != null) leftHand.SetActive(false);
        if (skipButton != null) skipButton.SetActive(false);
        if (barista != null) barista.SetActive(false);
        if (message != null) message.text = "";

        // Ensure all teats are active
        for (int i = 0; i < teats.Length; i++)
        {
            if (teats[i] != null) teats[i].SetActive(true);
        }

        // Position cow in view
        if (cow != null)
        {
            cow.transform.position = new Vector3(
                cow.transform.position.x,
                cowEndingPositionY,
                cow.transform.position.z
            );
        }

        // Start the game immediately
        Debug.Log("Starting song from SkipTutorialImmediate");
        gameManager.ShowScore();
        gameManager.StartSong();
    }

    // Update is called once per frame
    void Update()
    {
        // Check for skip input (Cancel/B button/Escape)
        if (skipButton != null && skipButton.activeSelf)
        {
            bool skipPressed = false;
            if (InputManager.Instance != null)
            {
                skipPressed = InputManager.Instance.Cancel.WasPressedThisFrame();
            }
            else
            {
                skipPressed = Input.GetKeyDown(KeyCode.Escape);
            }

            if (skipPressed)
            {
                SkipTutorial();
                return;
            }
        }

        switch(step) {
            case 0:
                if (introPopupIndex == introPopups.Length && !cowIsInView) {
                    EnterCow();
                }
                break;
            case 1:
                break;
            default:
                break;
        }
    }

    void IncreaseStep() {
        Debug.Log("Increase step");
        step++;
        Talk();
    }

    void StartSong() {
        gameManager.StartSong();
    }

    void HideBarista() {
        barista.SetActive(false);
        if (skipButton != null) skipButton.SetActive(false);
        HideButtonPrompts(); // Hide button prompts when barista hides
        gameManager.ShowScore();

        // Start song with calculated delay so first cup arrives after buffer
        float secondsUntilFirstCup = cupConductor.GetSecondsUntilFirstCup();
        if (secondsUntilFirstCup < bufferBeforeFirstCup)
        {
            // First cup would arrive too soon - delay the song start
            // by playing from a negative position (Unity handles this as silence)
            Debug.Log($"First cup at {secondsUntilFirstCup}s, adding delay for {bufferBeforeFirstCup}s buffer");
        }
        gameManager.StartSong();
    }

    /**
     * Barista methods
     **/
    void Talk() {
        StopBlinking();

        string[] popUps; int popUpIndex;

        switch(step) {
            case 0:
                popUps = introPopups;
                popUpIndex = introPopupIndex++;
                break;
            case 1:
                if (isGamepadTutorial)
                {
                    // Gamepad: same flow as udder - gamepadPopups then cowPopups
                    popUps = gamepadPopups;
                    popUpIndex = gamepadPopupIndex++;

                    if (popUpIndex == popUps.Length) {
                        // Show buttons, hide before cowPopups start
                        Invoke("ShowButtonPrompts", shutUpTime);
                        Invoke("HideButtonPrompts", shutUpTime + 5.5f);
                        Invoke("IncreaseStep", shutUpTime + 6f);
                        // Song starts in HideBarista after cowPopups
                    }
                }
                else
                {
                    // Udder/keyboard: use hand demo messages
                    popUps = handPopups;
                    popUpIndex = handPopupIndex++;

                    if (popUpIndex == popUps.Length) {
                        Invoke("EnterRightHand", shutUpTime);
                        Invoke("EnterLeftHand", shutUpTime + 0.5f);
                        Invoke("MoveLeftHand", shutUpTime + 2.5f);
                        Invoke("MoveLeftHand", shutUpTime + 4.5f);

                        Invoke("MoveRightHand", shutUpTime + 3.5f);
                        Invoke("MoveRightHand", shutUpTime + 5.2f);

                        Invoke("ExitHands", shutUpTime + 8f);
                        Invoke("IncreaseStep", shutUpTime + 6f);
                        // Song starts in HideBarista after cowPopups
                    }
                }
                break;
            case 2:
                popUps = cowPopups;
                popUpIndex = cowPopupIndex++;
                if (popUpIndex == popUps.Length) {
                    Invoke("HideBarista", shutUpTime);
                }
                break;
            // case 3:
            //     popUps = rhythmPopups;
            //     popUpIndex = rhythmPopupIndex;

            //     break;
            default:
                return;
        }

        if (popUpIndex == popUps.Length) {
            Invoke("ShutUp", shutUpTime);
            return;
        }

        string newMessage = popUps[popUpIndex];
        float talkTime = newMessage.Length * 0.03f;

        speechBubbleSound.time = Random.Range(0f, speechBubbleSoundLength - talkTime);
        speechBubbleSound.Play();

        speechBubble.SetActive(true);
        message.text = newMessage;
        baristaAnimator.SetBool("isTalking", true);

        Invoke("StopTalking", talkTime);

        if (popUpIndex < popUps.Length) {
            Invoke("Talk", talkTime + shutUpTime);
        }
    }

    void Blink() {
        baristaAnimator.SetBool("isBlinking", true);
        Invoke("StopBlinking", 1f);
        Invoke("Blink", Random.Range(0, 10f));
    }

    void StopTalking() {
        baristaAnimator.SetBool("isTalking", false);
        speechBubbleSound.Stop();
    }

    void StopBlinking() {
        baristaAnimator.SetBool("isBlinking", false);
    }

    void ShutUp() {
        StopTalking();
        speechBubble.SetActive(false);
        message.text = "";
    }

    /**
     * Button prompt methods (for gamepad tutorial)
     **/
    private Coroutine blinkCoroutine;

    void ShowButtonPrompts() {
        if (buttonPrompts == null) return;
        foreach (var prompt in buttonPrompts) {
            if (prompt != null) prompt.SetActive(true);
        }
        // Start blinking
        blinkCoroutine = StartCoroutine(BlinkButtonPrompts());
    }

    void HideButtonPrompts() {
        // Stop blinking
        if (blinkCoroutine != null) {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (buttonPrompts == null) return;
        foreach (var prompt in buttonPrompts) {
            if (prompt != null) prompt.SetActive(false);
        }
    }

    IEnumerator BlinkButtonPrompts() {
        float blinkInterval = 0.5f;
        bool visible = true;

        while (true) {
            yield return new WaitForSeconds(blinkInterval);
            visible = !visible;
            foreach (var prompt in buttonPrompts) {
                if (prompt != null) prompt.SetActive(visible);
            }
        }
    }

    /**
     * Hand methods
     **/
    void EnterRightHand() {
        rightHand.SetActive(true);
        teats[3].SetActive(false);
        Invoke("RightHandStartSqueezing", 1f);
        Invoke("RightHandStopSqueezing", 3.5f);
    }

    void EnterLeftHand() {
        leftHand.SetActive(true);
        teats[2].SetActive(false);
        Invoke("LeftHandStartSqueezing", 1f);
        Invoke("LeftHandStopSqueezing", 3.5f);
    }

    void ExitHands() {
        rightHand.SetActive(false);
        leftHand.SetActive(false);
        for (int i = 0; i < teats.Length; i++) {
            teats[i].SetActive(true);
        }
    }

    void MoveRightHand() {
        if (teats[1].activeSelf) { // if back right
            teats[3].SetActive(true);
            teats[1].SetActive(false);
            rightHandSprite.sortingLayerName = "Cow";
            rightHand.transform.position = new Vector2(1.5f, 3.2f);
        } else {
            teats[3].SetActive(false);
            teats[1].SetActive(true);
            rightHandSprite.sortingLayerName = "UI";
            rightHand.transform.position = new Vector2(2.772604f, 2.26f);
        }

    }

    void MoveLeftHand() {
        if (teats[0].activeSelf) { // if back right
            teats[0].SetActive(false);
            teats[2].SetActive(true);
            leftHandSprite.sortingLayerName = "Cow";
            leftHand.transform.position = new Vector2(-4f, 3.1f);
        } else {
            teats[0].SetActive(true);
            teats[2].SetActive(false);
            leftHandSprite.sortingLayerName = "UI";
            leftHand.transform.position = new Vector2(-3.136723f, 2.21f);
        }
    }

    void RightHandStartSqueezing() {
        rightHandAnimator.SetBool("isSqueezingTeat", true);
    }

    void RightHandStopSqueezing() {
        rightHandAnimator.SetBool("isSqueezingTeat", false);
    }

    void LeftHandStartSqueezing() {
        leftHandAnimator.SetBool("isSqueezingTeat", true);
    }

    void LeftHandStopSqueezing() {
        leftHandAnimator.SetBool("isSqueezingTeat", false);
    }

    /**
     * Cow methods
     **/
    void EnterCow() {
        if (cow.transform.position.y <= cowEndingPositionY && !cowIsInView) {
            cowIsInView = true;
            Moo();
            Invoke("IncreaseStep", shutUpTime *2);
        } else {
            cow.transform.Translate(Vector2.down * 5 * Time.deltaTime);
        }
    }

    void Moo() {
        cow.GetComponent<AudioSource>().Play();
    }

    public void SkipTutorial() {
        // Cancel all pending tutorial sequences
        CancelInvoke();

        // Hide tutorial UI elements
        speechBubble.SetActive(false);
        StopTalking();
        rightHand.SetActive(false);
        leftHand.SetActive(false);
        HideButtonPrompts();
        if (skipButton != null) skipButton.SetActive(false);
        message.text = "";

        // Ensure all teats are active
        for (int i = 0; i < teats.Length; i++) {
            teats[i].SetActive(true);
        }

        // Position cow if not already in view
        if (!cowIsInView) {
            cow.transform.position = new Vector3(
                cow.transform.position.x,
                cowEndingPositionY,
                cow.transform.position.z
            );
            cowIsInView = true;
        }

        // Start the game
        HideBarista();
        StartSong();
    }
}
