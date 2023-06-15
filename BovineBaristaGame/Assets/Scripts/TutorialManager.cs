using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    private int step = 0;
    public string[] introPopups;
    private int introPopupIndex = 0;

    public string[] cowPopups;
    private int cowPopupIndex = 0;

    public GameObject cow;
    public float cowEndingPositionY;
    private bool cowIsInView = false;

    public GameObject speechBubble;
    private AudioSource speechBubbleSound;
    private float speechBubbleSoundLength;

    public TMP_Text message;

    public GameObject barista;
    
    private AudioSource metronome;

    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        metronome = GetComponent<AudioSource>();
        animator = barista.GetComponent<Animator>();  
        speechBubbleSound = speechBubble.GetComponent<AudioSource>();
        speechBubbleSoundLength = speechBubbleSound.clip.length;
        
        speechBubble.SetActive(false);
        introPopupIndex = 0;

        Invoke("Blink", 0.5f);        
        Invoke("Talk", 2f);

        speechBubbleSound.Stop();
    }

    // Update is called once per frame
    void Update()
    {        
        if (step == 1) {
            if (!cowIsInView) {
                EnterCow();
            } else {

            }
        }        
    }

    /**
     * Barista methods
     **/
    void Talk() {        
        StopBlinking();

        string[] popUps; int popUpIndex; bool needsKey;

        switch(step) {
            case 0:
                popUps = introPopups;
                popUpIndex = introPopupIndex++;
                needsKey = false;
                break;
            case 1:
                popUps = cowPopups;
                popUpIndex = cowPopupIndex++;
                needsKey = false;
                break;
            default: 
                return;
        }

        if (popUpIndex == popUps.Length) {
            step++;
            Invoke("ShutUp", 1.5f);
            Invoke("Talk", 3f);
            return;
        }        

        string newMessage = popUps[popUpIndex];
        float talkTime = newMessage.Length * 0.025f;

        speechBubbleSound.time = Random.Range(0f, speechBubbleSoundLength - talkTime);
        speechBubbleSound.Play();

        speechBubble.SetActive(true);
        message.text = newMessage;
        animator.SetBool("isTalking", true);

        Invoke("StopTalking", talkTime);

        if (popUpIndex < popUps.Length && !needsKey) {
            Invoke("Talk", talkTime + 3f);
        }
    }

    void Blink() {
        animator.SetBool("isBlinking", true);
        Invoke("StopBlinking", 1f);
        Invoke("Blink", Random.Range(0, 15f));
    }

    void StopTalking() {
        animator.SetBool("isTalking", false);
        speechBubbleSound.Stop();
    }

    void StopBlinking() {
        animator.SetBool("isBlinking", false);
    }

    void ShutUp() {
        StopTalking();
        speechBubble.SetActive(false);
        message.text = "";
    }

    /**
     * Cow methods
     **/
    void EnterCow() {
        float speed = (cow.transform.position.y - cowEndingPositionY) * 15;
        Debug.Log(cow.transform.position.y);
        if (speed < 0) {
            cowIsInView = true;
        } else {
            cow.transform.Translate(Vector2.down * 2 * Time.deltaTime);
        }
    }
}
