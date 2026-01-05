using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeatController : MonoBehaviour
{
    public KeyCode keyPress;

    private bool isSqueezing = false;

    private Animator animator;

    private new BoxCollider2D collider;
    private Vector3 defaultColliderSize;

    private GameManager gameManager;

    // Track song position at press/release for accurate timing
    public float songPositionAtPress { get; private set; }
    public float songPositionAtRelease { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        collider = gameObject.GetComponent<BoxCollider2D>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        defaultColliderSize = collider.size;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keyPress)) {
            isSqueezing = true;
            songPositionAtPress = gameManager.songPositionInBeats;
            collider.size = new Vector3(defaultColliderSize.x * 1.5f, defaultColliderSize.y * 2f, 0.0f);
        }

        if (Input.GetKeyUp(keyPress)) {
            isSqueezing = false;
            songPositionAtRelease = gameManager.songPositionInBeats;
            collider.size = defaultColliderSize;
        }

        animator.SetBool("isSqueezing", isSqueezing);
    }

    /// <summary>
    /// Programmatically set squeezing state (used by AutoPlayController during attract mode)
    /// </summary>
    public void SetSqueezing(bool squeezing)
    {
        if (squeezing && !isSqueezing)
        {
            // Simulate key down
            isSqueezing = true;
            songPositionAtPress = gameManager.songPositionInBeats;
            collider.size = new Vector3(defaultColliderSize.x * 1.5f, defaultColliderSize.y * 2f, 0.0f);
        }
        else if (!squeezing && isSqueezing)
        {
            // Simulate key up
            isSqueezing = false;
            songPositionAtRelease = gameManager.songPositionInBeats;
            collider.size = defaultColliderSize;
        }

        animator.SetBool("isSqueezing", isSqueezing);
    }
}
