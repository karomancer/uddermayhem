using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Configs")]
    public LevelConfig easyConfig;
    public LevelConfig mediumConfig;
    public LevelConfig hardConfig;

    [Header("Scene Names")]
    public string levelSceneName = "Main";      // Single level scene for all difficulties
    public string titleSceneName = "Title";

    [Header("Transition")]
    public SceneTransitionManager transitionManager;

    [Header("Background Music")]
    public AudioSource backgroundMusic;

    [Header("Selection Sound Effects")]
    public AudioSource sfxAudioSource;
    public AudioClip staticSelectSound;
    [Range(0f, 1f)] public float staticSoundVolume = 1f;
    public AudioClip[] randomSelectSounds;
    [Range(0f, 1f)] public float randomSoundVolume = 1f;

    void Start()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }

    void Update()
    {
        // Check for Cancel action (B button / Escape) to go back
        bool cancelPressed = false;
        if (InputManager.Instance != null)
        {
            cancelPressed = InputManager.Instance.Cancel.WasPressedThisFrame();
        }
        else
        {
            cancelPressed = Input.GetKeyDown(KeyCode.Escape);
        }

        if (cancelPressed)
        {
            GoBack();
        }
    }

    // Called by Easy button OnClick
    public void SelectEasy()
    {
        LoadLevelWithConfig(easyConfig);
    }

    // Called by Medium button OnClick
    public void SelectMedium()
    {
        LoadLevelWithConfig(mediumConfig);
    }

    // Called by Hard button OnClick
    public void SelectHard()
    {
        LoadLevelWithConfig(hardConfig);
    }

    // Called by Back button OnClick
    public void GoBack()
    {
        LoadScene(titleSceneName);
    }

    private void LoadLevelWithConfig(LevelConfig config)
    {
        if (config != null)
        {
            GameManager.currentLevelConfig = config;
            GameManager.currentDifficulty = config.difficultyName;
        }

        // Stop music immediately
        StopMusic();

        // Play SFX and start transition together
        float sfxDuration = PlaySelectSounds();
        float transitionDuration = transitionManager != null ? transitionManager.transitionDuration : 0f;

        // Calculate extra delay needed after fade completes (if SFX is longer than transition)
        float delayAfterFade = Mathf.Max(0f, sfxDuration - transitionDuration);

        // Start transition (will wait for SFX if needed)
        if (transitionManager != null)
        {
            transitionManager.TransitionToScene(levelSceneName, delayAfterFade);
        }
        else
        {
            SceneManager.LoadScene(levelSceneName);
        }
    }

    private float PlaySelectSounds()
    {
        float maxDuration = 0f;

        if (sfxAudioSource != null)
        {
            // Play static sound
            if (staticSelectSound != null)
            {
                sfxAudioSource.PlayOneShot(staticSelectSound, staticSoundVolume);
                maxDuration = Mathf.Max(maxDuration, staticSelectSound.length);
            }

            // Play random sound
            if (randomSelectSounds != null && randomSelectSounds.Length > 0)
            {
                int randomIndex = Random.Range(0, randomSelectSounds.Length);
                AudioClip randomClip = randomSelectSounds[randomIndex];
                if (randomClip != null)
                {
                    sfxAudioSource.PlayOneShot(randomClip, randomSoundVolume);
                    maxDuration = Mathf.Max(maxDuration, randomClip.length);
                }
            }
        }

        return maxDuration;
    }

    private void LoadScene(string sceneName)
    {
        StopMusic();

        if (transitionManager != null)
        {
            transitionManager.TransitionToScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void StopMusic()
    {
        if (backgroundMusic != null && backgroundMusic.isPlaying)
        {
            backgroundMusic.Stop();
        }
    }
}
