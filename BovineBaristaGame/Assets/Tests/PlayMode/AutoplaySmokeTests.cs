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
    private const float TargetBeat = 74f;
    private const float RealtimeBudgetSeconds = 60f;

    private static readonly Type GameManagerType = Type.GetType("GameManager, Assembly-CSharp");
    private static readonly Type CupConductorType = Type.GetType("CupConductor, Assembly-CSharp");
    private static readonly Type AutoPlayType = Type.GetType("AutoPlayController, Assembly-CSharp");
    private static readonly Type CupViewType = Type.GetType("CoffeeController, Assembly-CSharp");
    private static readonly Type TeatType = Type.GetType("TeatController, Assembly-CSharp");

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
        string screenshotDir = Environment.GetEnvironmentVariable("UDDER_SMOKE_SHOTS");
        var shotEnter = new HashSet<Note>();
        var shotStasis = new HashSet<Note>();
        var shotPickup = new HashSet<Note>();
        var shotHalf = new HashSet<Note>();
        var shotDone = new HashSet<Note>();
        var shotExit = new HashSet<Note>();
        bool emptySqueezeShot = false;
        while (beat < TargetBeat)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline,
                $"song only reached beat {beat:F1} within {RealtimeBudgetSeconds}s; is audio advancing?");
            beat = (float)songBeatField.GetValue(gameManager);
            maxCupsSeen = Math.Max(maxCupsSeen, UnityEngine.Object.FindObjectsOfType(CupViewType).Length);
            if (!string.IsNullOrEmpty(screenshotDir))
            {
                // Squeeze an empty lane once so the no-cup stream can be seen (BackRight has no cup before beat 13)
                if (beat >= 10f && beat < 10.6f && !emptySqueezeShot)
                {
                    var teat = UnityEngine.Object.FindObjectsOfType(TeatType).Cast<Component>()
                        .First(c => (int)TeatType.GetField("teatPosition").GetValue(c) == 3);
                    TeatType.GetMethod("SetSqueezing").Invoke(teat, new object[] { true });
                    if (beat >= 10.3f)
                    {
                        Capture(System.IO.Path.Combine(screenshotDir, "empty_lane_squeeze.png"));
                        TeatType.GetMethod("SetSqueezing").Invoke(teat, new object[] { false });
                        emptySqueezeShot = true;
                    }
                }

                // Quarter-note cups only: mid-hold, then just after the release
                foreach (Note n in notes)
                {
                    if (n.HoldBeats < 1f) continue;
                    if (n.state == NoteState.Pending && beat >= n.startBeat - 1.0f && shotEnter.Add(n))
                        Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_enter.png"));
                    if (n.state == NoteState.Pending && beat >= n.startBeat - 0.3f && shotStasis.Add(n))
                        Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_stasis.png"));
                    if (n.state == NoteState.Pending && beat >= n.startBeat - 0.12f && shotPickup.Add(n))
                        Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_pickup.png"));
                    if (n.state == NoteState.Holding && beat >= n.pressBeat + n.HoldBeats * 0.5f && shotHalf.Add(n))
                        Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_half.png"));
                    if (n.state == NoteState.Done && beat >= n.endBeat + 0.2f && shotDone.Add(n))
                        Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_done.png"));
                    if (n.state == NoteState.Done && beat >= n.endBeat + 0.7f && shotExit.Add(n))
                        Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_exit.png"));
                }
            }
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

    private static void Capture(string path)
    {
        Camera camera = Camera.main;
        const int width = 1280, height = 720;
        var target = new RenderTexture(width, height, 24);
        RenderTexture previousTarget = camera.targetTexture;
        camera.targetTexture = target;
        camera.Render();
        camera.targetTexture = previousTarget;

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = previousActive;

        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.Destroy(texture);
        target.Release();
        UnityEngine.Object.Destroy(target);
    }

    private static void ForceAutoPlay(Scene scene, LoadSceneMode mode)
    {
        var autoPlay = UnityEngine.Object.FindObjectOfType(AutoPlayType);
        Assert.IsNotNull(autoPlay, "AutoPlayController not found in Main scene");
        AutoPlayType.GetField("forceAutoPlay").SetValue(autoPlay, true);
    }
}
#endif
