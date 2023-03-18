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

    // Start is called before the first frame update
    void Start()
    {
        animator = gameObject.GetComponent<Animator>();   
        collider = gameObject.GetComponent<BoxCollider2D>();

        defaultColliderSize = collider.size;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keyPress)) {
            isSqueezing = true;
            collider.size = new Vector3(defaultColliderSize.x * 1.5f, defaultColliderSize.y * 2f, 0.0f);
        }

        if (Input.GetKeyUp(keyPress)) {
            isSqueezing = false;
            collider.size = defaultColliderSize;
        }

        animator.SetBool("isSqueezing", isSqueezing);
    }

    public void SetSqueezing(bool squeezing) {
        isSqueezing = squeezing;
    }
}
