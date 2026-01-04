using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string easySceneName = "Easy";
    public string mediumSceneName = "Main";
    public string hardSceneName = "Hard";
    public string titleSceneName = "Title";

    [Header("Transition")]
    public SceneTransitionManager transitionManager;

    // Called by Easy button OnClick
    public void SelectEasy()
    {
        GameManager.currentDifficulty = "Easy";
        LoadLevel(easySceneName);
    }

    // Called by Medium button OnClick
    public void SelectMedium()
    {
        GameManager.currentDifficulty = "Medium";
        LoadLevel(mediumSceneName);
    }

    // Called by Hard button OnClick
    public void SelectHard()
    {
        GameManager.currentDifficulty = "Hard";
        LoadLevel(hardSceneName);
    }

    // Called by Back button OnClick
    public void GoBack()
    {
        LoadLevel(titleSceneName);
    }

    private void LoadLevel(string sceneName)
    {
        if (transitionManager != null)
        {
            transitionManager.TransitionToScene(sceneName);
        }
        else
        {
            // Fallback: load immediately without transition
            SceneManager.LoadScene(sceneName);
        }
    }
}
