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

    public string[] squeezePopups;
    private int squeezePopupIndex = 0;
    private bool[] hasSqueezedTeats = {false, false, false, false};

    public string[] rhythmPopups;
    private int rhythmPopupIndex = 0;

    private float dspSongTime;
    private int numHits = 0;
    private TutorialCupConductor cupConductor;
    public int NEEDED_HITS_TO_CONTINUE = 8;
    public float beatAllowance = 0.5f;
    public float songPositionInBeats = 0f;
    //The offset to the first beat of the song in seconds
    public float firstBeatOffset = 0;
    private float songPosition = 0f;

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
    
    private AudioSource metronome;

    private Animator baristaAnimator;

    private float shutUpTime = 1.5f;


    // Start is called before the first frame update
    void Start()
    {
        metronome = GetComponent<AudioSource>();
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

        dspSongTime = (float)AudioSettings.dspTime;
        cupConductor = GetComponent<TutorialCupConductor>();
        
        introPopupIndex = 0;

        Invoke("Blink", 0.5f);        
        Invoke("Talk", 2f);

        teats[0].GetComponent<AudioSource>().Play();
        speechBubbleSound.Stop();
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
            case 2:
                if (cowPopupIndex == cowPopups.Length) {
                }
                break;
            case 3: 
                if (squeezePopupIndex >= squeezePopups.Length && (!hasSqueezedTeats[0] || !hasSqueezedTeats[1] || !hasSqueezedTeats[2] || !hasSqueezedTeats[3]) ) {
                    WaitForSqueezes();
                }

                if (hasSqueezedTeats[0] && hasSqueezedTeats[1] && hasSqueezedTeats[2] && hasSqueezedTeats[3]) {
                    Moo();
                    IncreaseStep();
                }
                break;
            case 4:
                break;
            case 5:
            if (numHits == NEEDED_HITS_TO_CONTINUE) {
                IncreaseStep();
            } else {
                songPosition = (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);
                songPositionInBeats = songPosition / CupConductor.SecPerBeat;
                cupConductor.Conduct(songPositionInBeats);
            }
                break;
            default:
                break;
        }
    }

    void IncreaseStep() {
        Debug.Log("Increase step");
        step++;
        Invoke("Talk", 1f);
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

                if (popUpIndex == popUps.Length) {
                    Invoke("IncreaseStep", shutUpTime);
                }
                break;
            case 1:
                popUps = handPopups;
                popUpIndex = handPopupIndex++;

                if (popUpIndex == popUps.Length) {
                    Invoke("EnterRightHand", shutUpTime);                    
                    Invoke("IncreaseStep", shutUpTime + 4f);
                }
                break;
            case 2: 
                popUps = cowPopups;
                popUpIndex = cowPopupIndex++;

                if (popUpIndex == popUps.Length) {
                    RightHandStartSqueezing();
                    Invoke("EnterLeftHand", shutUpTime);                    
                    Invoke("MoveLeftHand", shutUpTime + 2.5f);
                    Invoke("MoveLeftHand", shutUpTime + 4.5f);

                    Invoke("EnterRightHand", shutUpTime + 3f);
                    Invoke("MoveRightHand", shutUpTime + 3.5f);
                    Invoke("MoveRightHand", shutUpTime + 5.2f);

                    Invoke("ExitHands", shutUpTime + 8f);
                    Invoke("IncreaseStep", shutUpTime + 8f);
                }
                break;
            case 3:
                popUps = squeezePopups;
                popUpIndex = squeezePopupIndex++;
                
                break;
            case 4:
                popUps = rhythmPopups;
                popUpIndex = rhythmPopupIndex++;

                if (popUpIndex == popUps.Length) {
                    metronome.Play();
                }
                
                break;
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
        float speed = (cow.transform.position.y - cowEndingPositionY) * 15;
        if (speed < 0) {
            cowIsInView = true;
            IncreaseStep();
        } else {
            cow.transform.Translate(Vector2.down * 2 * Time.deltaTime);
        }
    }

    void Moo() {
        cow.GetComponent<AudioSource>().Play();
    }

    void WaitForSqueezes() {
        if (Input.GetKeyDown(KeyCode.Q)) {
            hasSqueezedTeats[0] = true;
            teats[0].GetComponent<AudioSource>().Play();
        }

        if (Input.GetKeyDown(KeyCode.W)) {
            hasSqueezedTeats[1] = true;
            teats[1].GetComponent<AudioSource>().Play();
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            hasSqueezedTeats[2] = true;
            teats[2].GetComponent<AudioSource>().Play();
        }

        if (Input.GetKeyDown(KeyCode.A)) {
            hasSqueezedTeats[3] = true;
            teats[3].GetComponent<AudioSource>().Play();
        }
    }

    /**
    * This is seriously getting out of hand.
    * beat stuff
    **/
    public BeatTiming IsOnBeat(int measure, float beat)
    {
        float expectedSongPosition = (measure * 4) + beat - 1;
        bool isAcceptablyEarly = songPositionInBeats > (expectedSongPosition - beatAllowance);
        bool isAcceptablyLate = songPositionInBeats < (expectedSongPosition + beatAllowance);
        Debug.Log("Expected " + expectedSongPosition + " Got: " + songPositionInBeats);
        if (isAcceptablyEarly && isAcceptablyLate)
        {
        Debug.Log("Nice!");
        return BeatTiming.OnTime;
        }

        if (!isAcceptablyLate)
        {
        Debug.Log("Too Late");
        return BeatTiming.TooLate;
        }

        Debug.Log("Too early");
        return BeatTiming.TooEarly;
    }
}
