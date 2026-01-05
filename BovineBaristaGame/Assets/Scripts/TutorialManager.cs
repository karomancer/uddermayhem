using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    private int step = 0;
    public string[] introPopups;
    private int introPopupIndex = 0;

    public string[] handPopups;
    private int handPopupIndex = 0;

    public string[] cowPopups;
    private int cowPopupIndex = 0;

    public string[] rhythmPopups;
    private int rhythmPopupIndex = 0;

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

    // Start is called before the first frame update
    void Start()
    {
        metronome = GetComponent<AudioSource>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        // Check if tutorial should be skipped based on level config
        if (!ShouldShowTutorial())
        {
            SkipTutorialImmediate();
            return;
        }

        baristaAnimator = barista.GetComponent<Animator>();

        speechBubbleSound = speechBubble.GetComponent<AudioSource>();
        speechBubbleSoundLength = speechBubbleSound.clip.length;
        speechBubble.SetActive(false);

        rightHandAnimator = rightHand.GetComponent<Animator>();
        rightHandSprite = rightHand.GetComponent<SpriteRenderer>();
        leftHandAnimator = leftHand.GetComponent<Animator>();
        leftHandSprite = leftHand.GetComponent<SpriteRenderer>();
        rightHand.SetActive(false);
        leftHand.SetActive(false);

        introPopupIndex = 0;

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
        gameManager.StartSong((float)AudioSettings.dspTime);
    }

    // Update is called once per frame
    void Update()
    {
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
        gameManager.StartSong((float)AudioSettings.dspTime);
    }

    void HideBarista() {
        barista.SetActive(false);
        gameManager.ShowScore();
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

                    Invoke("StartSong", shutUpTime + 6f);
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
