using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    // Event fired when enter transition completes
    public event Action OnEnterTransitionComplete;

    [Header("References")]
    [Tooltip("UI Image that covers the screen for fade effect")]
    public Image fadeOverlay;
    [Tooltip("Camera to rotate/zoom (leave empty to find Main Camera)")]
    public Camera targetCamera;

    [Header("Transition Settings")]
    public float transitionDuration = 0.8f;
    public float rotationAmount = 180f; // Degrees to rotate
    public float zoomAmount = 5f; // How much to zoom in (added to ortho size)

    [Header("Enter Transition")]
    [Tooltip("Play enter transition when scene starts")]
    public bool playEnterTransition = true;

    private float originalOrthoSize;
    private float originalRotation;
    private bool isTransitioning = false;

    public bool IsTransitioning => isTransitioning;

    void Start()
    {
        // Find camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Store original values
        if (targetCamera != null)
        {
            originalOrthoSize = targetCamera.orthographicSize;
            originalRotation = targetCamera.transform.eulerAngles.z;
        }

        // Ensure fade overlay exists and is set up
        if (fadeOverlay != null)
        {
            // Start fully black if we're going to play enter transition
            if (playEnterTransition)
            {
                fadeOverlay.color = new Color(0, 0, 0, 1);
                fadeOverlay.raycastTarget = true;

                // Set camera to "zoomed in and rotated" state
                if (targetCamera != null)
                {
                    targetCamera.orthographicSize = originalOrthoSize - zoomAmount;
                    targetCamera.transform.rotation = Quaternion.Euler(0, 0, originalRotation + rotationAmount);
                }

                StartCoroutine(PlayEnterTransition());
            }
            else
            {
                fadeOverlay.color = new Color(0, 0, 0, 0);
                fadeOverlay.raycastTarget = false;
                // No transition - fire event immediately
                OnEnterTransitionComplete?.Invoke();
            }
        }
        else if (!playEnterTransition)
        {
            // No fade overlay and no transition - fire event immediately
            OnEnterTransitionComplete?.Invoke();
        }
    }

    public void TransitionToScene(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(PlayExitTransition(sceneName));
        }
    }

    private IEnumerator PlayEnterTransition()
    {
        isTransitioning = true;

        float elapsed = 0f;
        float startOrtho = targetCamera.orthographicSize;
        float startRotation = targetCamera.transform.eulerAngles.z;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // Smooth step easing
            float smoothT = t * t * (3f - 2f * t);

            // Fade from black to transparent
            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(0, 0, 0, 1f - smoothT);
            }

            // Zoom out and rotate back to normal
            if (targetCamera != null)
            {
                targetCamera.orthographicSize = Mathf.Lerp(startOrtho, originalOrthoSize, smoothT);

                float currentRotation = Mathf.LerpAngle(startRotation, originalRotation, smoothT);
                targetCamera.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            }

            yield return null;
        }

        // Ensure final values
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.raycastTarget = false;
        }

        if (targetCamera != null)
        {
            targetCamera.orthographicSize = originalOrthoSize;
            targetCamera.transform.rotation = Quaternion.Euler(0, 0, originalRotation);
        }

        isTransitioning = false;

        // Notify listeners that enter transition is complete
        OnEnterTransitionComplete?.Invoke();
    }

    private IEnumerator PlayExitTransition(string sceneName)
    {
        isTransitioning = true;

        // Enable raycast blocking during transition
        if (fadeOverlay != null)
        {
            fadeOverlay.raycastTarget = true;
        }

        float elapsed = 0f;
        float startOrtho = targetCamera != null ? targetCamera.orthographicSize : originalOrthoSize;
        float startRotation = targetCamera != null ? targetCamera.transform.eulerAngles.z : originalRotation;

        float targetOrtho = originalOrthoSize - zoomAmount;
        float targetRotation = originalRotation + rotationAmount;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // Smooth step easing
            float smoothT = t * t * (3f - 2f * t);

            // Fade to black
            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(0, 0, 0, smoothT);
            }

            // Zoom in and rotate
            if (targetCamera != null)
            {
                targetCamera.orthographicSize = Mathf.Lerp(startOrtho, targetOrtho, smoothT);

                float currentRotation = Mathf.LerpAngle(startRotation, targetRotation, smoothT);
                targetCamera.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            }

            yield return null;
        }

        // Load the new scene
        SceneManager.LoadScene(sceneName);
    }

    // Manual trigger for enter transition (useful if playEnterTransition is false)
    public void PlayEnter()
    {
        if (!isTransitioning)
        {
            StartCoroutine(PlayEnterTransition());
        }
    }
}
