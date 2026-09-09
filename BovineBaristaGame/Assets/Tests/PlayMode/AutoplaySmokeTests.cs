#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Plays the real Main scene under autoplay for the densest stretch of the Hard chart and
/// checks the judge, conductor, and cup views agree: every note judged so far is Perfect.
/// Game types live in Assembly-CSharp, which test assemblies cannot reference, so they are reached by name.
/// </summary>
public class AutoplaySmokeTests
{
    private const string LevelConfigPath = "Assets/LevelConfigs/LevelConfigHard.asset";
    private const float TargetBeat = 60f;
    private const float RealtimeBudgetSeconds = 60f;

    private static readonly Type GameManagerType = Type.GetType("GameManager, Assembly-CSharp");
    private static readonly Type CupConductorType = Type.GetType("CupConductor, Assembly-CSharp");
    private static readonly Type AutoPlayType = Type.GetType("AutoPlayController, Assembly-CSharp");
    private static readonly Type CupViewType = Type.GetType("CoffeeController, Assembly-CSharp");

    [UnityTest, Timeout(180000)]
    public IEnumerator HardChart_AutoplayJudgesEveryNotePerfect()
    {
        var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(LevelConfigPath);
        Assert.IsNotNull(config, "Hard level config not found");
        var runConfig = UnityEngine.Object.Instantiate(config);
        runConfig.GetType().GetField("showTutorial").SetValue(runConfig, false);
        GameManagerType.GetField("currentLevelConfig").SetValue(null, runConfig);

        SceneManager.sceneLoaded += ForceAutoPlay;
        yield return SceneManager.LoadSceneAsync("Main");
        SceneManager.sceneLoaded -= ForceAutoPlay;
        AudioListener.volume = 0f;

        var gameManagerObject = GameObject.Find("GameManager");
        var gameManager = gameManagerObject.GetComponent(GameManagerType);
        var conductor = gameManagerObject.GetComponent(CupConductorType);
        var notes = (IReadOnlyList<Note>)CupConductorType.GetProperty("Notes").GetValue(conductor);
        var songBeatField = GameManagerType.GetField("songPositionInBeats");
        Assert.Greater(notes.Count, 0, "no notes loaded");

        int maxCupsSeen = 0;
        float deadline = Time.realtimeSinceStartup + RealtimeBudgetSeconds;
        float beat = 0f;
        while (beat < TargetBeat)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline,
                $"song only reached beat {beat:F1} within {RealtimeBudgetSeconds}s; is audio advancing?");
            beat = (float)songBeatField.GetValue(gameManager);
            maxCupsSeen = Math.Max(maxCupsSeen, UnityEngine.Object.FindObjectsOfType(CupViewType).Length);
            yield return null;
        }

        var settled = notes.Where(n => n.endBeat + 1f < beat).ToList();
        Assert.Greater(settled.Count, 40, "expected the dense Hard opening to be behind us");
        Assert.Greater(maxCupsSeen, 0, "no cups were ever spawned");

        var notPerfect = settled.Where(n => n.state != NoteState.Done ||
                                            n.pressJudgment != BeatTiming.OnTime ||
                                            n.releaseJudgment != BeatTiming.OnTime).ToList();
        Assert.IsEmpty(notPerfect.Select(n => $"{n.lane} @ {n.startBeat} ({n.state}, press {n.pressJudgment}, release {n.releaseJudgment})"),
            "autoplay should judge every settled note as Perfect");

        Assert.AreEqual(0, notes.Count(n => n.state == NoteState.Missed), "no note should be missed under autoplay");

        int streak = (int)GameManagerType.GetProperty("CurrentStreak").GetValue(gameManager);
        int doneCount = notes.Count(n => n.state == NoteState.Done);
        Assert.AreEqual(doneCount, streak, "streak should equal the number of completed notes");

        Debug.Log($"[Smoke] beat {beat:F1}: {doneCount} notes done, streak {streak}, max concurrent cups {maxCupsSeen}");
    }

    private static void ForceAutoPlay(Scene scene, LoadSceneMode mode)
    {
        var autoPlay = UnityEngine.Object.FindObjectOfType(AutoPlayType);
        Assert.IsNotNull(autoPlay, "AutoPlayController not found in Main scene");
        AutoPlayType.GetField("forceAutoPlay").SetValue(autoPlay, true);
    }
}
#endif
