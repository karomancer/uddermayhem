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
    // Defaults cover the dense Hard opening; UDDER_SMOKE_LEVEL / UDDER_SMOKE_TARGET_BEAT pick another chart or stretch
    private static readonly string LevelConfigPath = Environment.GetEnvironmentVariable("UDDER_SMOKE_LEVEL") ?? "Assets/LevelConfigs/LevelConfigHard.asset";
    private static readonly float TargetBeat = float.TryParse(Environment.GetEnvironmentVariable("UDDER_SMOKE_TARGET_BEAT"), out float b) ? b : 74f;
    private static readonly float RealtimeBudgetSeconds = TargetBeat + 30f;
    private const int CapturesPerCupSize = 2;

    private static readonly Type GameManagerType = Type.GetType("GameManager, Assembly-CSharp");
    private static readonly Type CupConductorType = Type.GetType("CupConductor, Assembly-CSharp");
    private static readonly Type AutoPlayType = Type.GetType("AutoPlayController, Assembly-CSharp");
    private static readonly Type CupViewType = Type.GetType("CoffeeController, Assembly-CSharp");
    private static readonly Type TeatType = Type.GetType("TeatController, Assembly-CSharp");

    [UnityTest, Timeout(180000)]
    public IEnumerator HardChart_AutoplayJudgesEveryNotePerfect()
    {
        var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(LevelConfigPath);
        Assert.IsNotNull(config, $"level config not found at {LevelConfigPath}");
        var runConfig = UnityEngine.Object.Instantiate(config);
        runConfig.GetType().GetField("showTutorial").SetValue(runConfig, false);
        GameManagerType.GetField("currentLevelConfig").SetValue(null, runConfig);

        SceneManager.sceneLoaded += ForceAutoPlay;
        yield return SceneManager.LoadSceneAsync("Main");
        SceneManager.sceneLoaded -= ForceAutoPlay;
        AudioListener.volume = 0f;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UDDER_SMOKE_SHOTS")))
        {
            // Overlay canvases are invisible to Camera.Render; move them in front of the camera so captures include the HUD
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 1f;
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = 100;
            }
        }

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
        var shotPush = new HashSet<(Note, float)>();
        var shotStasis = new HashSet<Note>();
        var shotPickup = new HashSet<Note>();
        var shotHalf = new HashSet<Note>();
        var shotDone = new HashSet<Note>();
        var shotExit = new HashSet<Note>();
        bool emptySqueezeShot = false;
        bool hudShot = false;
        bool danceOnBeatShot = false, danceMidBeatShot = false, dance2xShot = false;
        var capturedBySize = new Dictionary<float, List<Note>>();
        bool entranceShot = false;
        while (beat < TargetBeat)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline,
                $"song only reached beat {beat:F1} within {RealtimeBudgetSeconds}s; is audio advancing?");
            beat = (float)songBeatField.GetValue(gameManager);
            maxCupsSeen = Math.Max(maxCupsSeen, UnityEngine.Object.FindObjectsOfType(CupViewType).Length);
            if (!string.IsNullOrEmpty(screenshotDir))
            {
                if (beat > 0f && beat < 0.8f && !entranceShot)
                {
                    entranceShot = true;
                    Debug.Log($"[HUD] jar entrance capture at beat {beat:F2}");
                    yield return Capture(System.IO.Path.Combine(screenshotDir, "jar_entrance.png"));
                }
                int multiplier = (int)GameManagerType.GetProperty("CurrentMultiplier").GetValue(gameManager);
                float phase = beat - Mathf.Floor(beat);
                if (multiplier == 2 && !dance2xShot && phase < 0.06f)
                {
                    dance2xShot = true;
                    yield return Capture(System.IO.Path.Combine(screenshotDir, "jar_dance_2x_onbeat.png"));
                    LogJarRig("2x on-beat", (float)songBeatField.GetValue(gameManager));
                }
                if (multiplier == 4 && !danceOnBeatShot && phase < 0.06f)
                {
                    danceOnBeatShot = true;
                    yield return Capture(System.IO.Path.Combine(screenshotDir, "jar_dance_4x_onbeat.png"));
                    LogJarRig("4x on-beat", (float)songBeatField.GetValue(gameManager));
                }
                if (multiplier == 4 && !danceMidBeatShot && Mathf.Abs(phase - 0.3f) < 0.05f)
                {
                    danceMidBeatShot = true;
                    yield return Capture(System.IO.Path.Combine(screenshotDir, "jar_dance_4x_stretch.png"));
                    LogJarRig("4x stretch", (float)songBeatField.GetValue(gameManager));
                }
                if (beat >= 9f && !hudShot)
                {
                    yield return Capture(System.IO.Path.Combine(screenshotDir, "hud.png"));
                    hudShot = true;
                    var scoreText = (Component)GameManagerType.GetField("ScoreText").GetValue(gameManager);
                    string scoreString = (string)scoreText.GetType().GetProperty("text").GetValue(scoreText);
                    var rt = scoreText.GetComponent<RectTransform>(); var c = new Vector3[4]; rt.GetWorldCorners(c);
                    var canvas = rt.GetComponentInParent<Canvas>();
                    Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    Debug.Log($"[HUD] screen {Screen.width}x{Screen.height} canvas mode {canvas.renderMode} scale {canvas.scaleFactor} text '{scoreString}' rect {rt.rect.size} lossyScale {rt.lossyScale} corners(screen) BL {RectTransformUtility.WorldToScreenPoint(uiCam, c[0])} TL {RectTransformUtility.WorldToScreenPoint(uiCam, c[1])} TR {RectTransformUtility.WorldToScreenPoint(uiCam, c[2])}");
                    var jar = GameObject.Find("TipJar");
                    Debug.Log($"[HUD] jar pos {jar?.transform.position} scale {jar?.transform.localScale} cam {Camera.main.transform.position} ortho {Camera.main.orthographicSize} pixelRect {Camera.main.pixelRect}");
                }

                // Squeeze an empty lane once so the no-cup stream can be seen (BackRight has no cup before beat 13)
                if (beat >= 10f && beat < 10.6f && !emptySqueezeShot)
                {
                    var teat = UnityEngine.Object.FindObjectsOfType(TeatType).Cast<Component>()
                        .First(c => (int)TeatType.GetField("teatPosition").GetValue(c) == 3);
                    TeatType.GetMethod("SetSqueezing").Invoke(teat, new object[] { true });
                    if (beat >= 10.3f)
                    {
                        yield return Capture(System.IO.Path.Combine(screenshotDir, "empty_lane_squeeze.png"));
                        TeatType.GetMethod("SetSqueezing").Invoke(teat, new object[] { false });
                        emptySqueezeShot = true;
                    }
                }

                // Quarter-note cups only: mid-hold, then just after the release
                foreach (Note n in notes)
                {
                    if (!capturedBySize.TryGetValue(n.HoldBeats, out var captured)) capturedBySize[n.HoldBeats] = captured = new List<Note>();
                    if (!captured.Contains(n)) { if (captured.Count >= CapturesPerCupSize) continue; captured.Add(n); }
                    if (n.HoldBeats >= 4f)
                    {
                        foreach (float ahead in new[] { 0.85f, 0.7f, 0.55f, 0.4f })
                        {
                            if (n.state != NoteState.Pending || beat < n.startBeat - ahead || !shotPush.Add((n, ahead))) continue;
                            var teat = UnityEngine.Object.FindObjectsOfType(TeatType).Cast<Component>()
                                .First(c => (int)TeatType.GetField("teatPosition").GetValue(c) == (int)n.lane);
                            Debug.Log($"[Push] beat {beat:F2} ({n.lane} cup at {n.startBeat}) teat swing {TeatType.GetProperty("SwingAngle").GetValue(teat):F1} deg");
                            yield return Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_push{ahead:0.00}.png"));
                        }
                    }
                    if (n.state == NoteState.Pending && beat >= n.startBeat - 1.0f && shotEnter.Add(n))
                        yield return Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_enter.png"));
                    if (n.state == NoteState.Pending && beat >= n.startBeat - 0.3f && shotStasis.Add(n))
                        yield return Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_stasis.png"));
                    if (n.state == NoteState.Pending && beat >= n.startBeat - 0.12f && shotPickup.Add(n))
                        yield return Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_pickup.png"));
                    if (n.state == NoteState.Holding && beat >= n.pressBeat + n.HoldBeats * 0.5f && shotHalf.Add(n))
                        yield return Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_half.png"));
                    if (n.state == NoteState.Done && beat >= n.endBeat + 0.2f && shotDone.Add(n))
                        yield return Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_done.png"));
                    if (n.state == NoteState.Done && beat >= n.endBeat + 0.7f && shotExit.Add(n))
                        yield return Capture(System.IO.Path.Combine(screenshotDir, $"beat{n.startBeat:000}_{n.lane}_exit.png"));
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

        int maxScore = (int)GameManagerType.GetMethod("MaxScoreForChart").Invoke(gameManager, null);
        Assert.AreEqual(Grading.MaxScore(notes.Count, 24, new[] { 0, 10, 25, 50 }, new[] { 1, 2, 3, 4 }), maxScore, "ceiling must follow the chart's note count and the scene's tiers");
        if (notes.Count == 164) Assert.AreEqual(27480, maxScore, "the Hard chart's ceiling");
        int score = (int)GameManagerType.GetProperty("CurrentScore").GetValue(gameManager);
        Assert.Greater(score, 0, "autoplay should have scored");
        Debug.Log($"[Smoke] beat {beat:F1}: {doneCount} notes done, streak {streak}, max concurrent cups {maxCupsSeen}, score {score}/{maxScore}");
    }

    private static void LogJarRig(string label, float beat)
    {
        var jar = GameObject.Find("TipJar");
        var baseBone = jar != null ? jar.transform.Find("base") : null;
        var belly = baseBone != null ? baseBone.Find("belly") : null;
        var front = jar != null ? jar.transform.Find("JarFront") : null;
        var skin = front != null ? front.GetComponent("UnityEngine.U2D.Animation.SpriteSkin") : null;
        object valid = skin != null ? skin.GetType().GetProperty("isValid", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(skin) : null;
        Debug.Log($"[Dance] {label} beat {beat:F2} base scale {baseBone?.localScale} belly scale {belly?.localScale} skin valid {valid}");
    }

    // Captures at every size in UDDER_SMOKE_SIZE ("1280x720,1692x772"), else at the batchmode screen size.
    // The camera renders to a texture of that size for one frame first, so the UI lays itself out for it.
    private static IEnumerator Capture(string path)
    {
        var sizes = new List<Vector2Int>();
        string spec = Environment.GetEnvironmentVariable("UDDER_SMOKE_SIZE");
        if (!string.IsNullOrEmpty(spec))
            foreach (string s in spec.Split(','))
            {
                string[] wh = s.Trim().Split('x');
                sizes.Add(new Vector2Int(int.Parse(wh[0]), int.Parse(wh[1])));
            }
        if (sizes.Count == 0) sizes.Add(new Vector2Int(Screen.width, Screen.height));

        foreach (Vector2Int size in sizes)
        {
            string file = sizes.Count > 1 ? path.Replace(".png", $"_{size.x}x{size.y}.png") : path;
            yield return CaptureAt(file, size.x, size.y);
        }
    }

    private static IEnumerator CaptureAt(string path, int width, int height)
    {
        Camera camera = Camera.main;
        var target = new RenderTexture(width, height, 24);
        RenderTexture previousTarget = camera.targetTexture;
        camera.targetTexture = target;
        // Frame 1: CanvasScaler.Update picks up the new size; frame 2: LateUpdate placement (TipJar) uses it
        yield return null;
        yield return null;
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
