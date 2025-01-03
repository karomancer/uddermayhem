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

  private double squeezeStartTime;
  private double squeezeEndTime;
  private bool autoSqueezing = false;

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
    if (autoSqueezing)
    {
      double currentTime = AudioSettings.dspTime;
      if (currentTime >= squeezeStartTime)
      {
        SetSqueezing(true);
      }
      if (currentTime >= squeezeEndTime)
      {
        SetSqueezing(false);
        autoSqueezing = false;
      }
    }
    if (Input.GetKeyDown(keyPress)) {
      SetSqueezing(true);
    }

    if (Input.GetKeyUp(keyPress)) {
      SetSqueezing(false);
    }

    animator.SetBool("isSqueezing", isSqueezing);
  }

  public void SetSqueezing(bool squeezing) {
    isSqueezing = squeezing;
    collider.size = squeezing ? new Vector3(defaultColliderSize.x * 1.5f, defaultColliderSize.y * 2f, 0.0f) : defaultColliderSize;
    animator.SetBool("isSqueezing", squeezing);

    // Ensure the collider is updated to trigger collision events
    collider.enabled = false;
    collider.enabled = true;
  }

  public void SetSqueezing(double startTime, double endTime)
  {
    squeezeStartTime = startTime;
    squeezeEndTime = endTime;
    autoSqueezing = true;
  }
}
