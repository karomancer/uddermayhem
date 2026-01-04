using UnityEngine;
using UnityEngine.SceneManagement;

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
        LoadScene(levelSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (transitionManager != null)
        {
            transitionManager.TransitionToScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
