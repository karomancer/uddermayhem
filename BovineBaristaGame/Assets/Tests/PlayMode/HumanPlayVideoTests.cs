#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Plays the real Main scene like a person would: presses land with timing noise, some cups are missed,
/// some are squeezed too early or too late, some holds run long. Captures every frame across the song
/// into UDDER_VIDEO_DIR with a manifest of song beats so the frames can be encoded at game speed.
/// Skipped unless UDDER_VIDEO_DIR is set.
/// </summary>
public class HumanPlayVideoTests
{
    private static readonly string VideoDir = Environment.GetEnvironmentVariable("UDDER_VIDEO_DIR");
    private static readonly string LevelConfigPath = Environment.GetEnvironmentVariable("UDDER_VIDEO_LEVEL") ?? "Assets/LevelConfigs/LevelConfigMedium.asset";
    private static readonly int Seed = int.TryParse(Environment.GetEnvironmentVariable("UDDER_VIDEO_SEED"), out int s) ? s : 7;
    private static readonly float EndBeatOverride = float.TryParse(Environment.GetEnvironmentVariable("UDDER_VIDEO_END_BEAT"), out float e) ? e : -1f;
    private const float FramesPerBeat = 16f;

    private static readonly Type GameManagerType = Type.GetType("GameManager, Assembly-CSharp");
    private static readonly Type CupConductorType = Type.GetType("CupConductor, Assembly-CSharp");
    private static readonly Type TeatType = Type.GetType("TeatController, Assembly-CSharp");

    private enum Kind { Press, Release }
    private class Cue { public float beat; public TeatPosition lane; public Kind kind; public string why; }

    [UnityTest, Timeout(600000)]
    public IEnumerator HumanLikeRun_RecordsFrames()
    {
        if (string.IsNullOrEmpty(VideoDir)) { Assert.Ignore("set UDDER_VIDEO_DIR to record"); yield break; }
        System.IO.Directory.CreateDirectory(VideoDir);

        var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(LevelConfigPath);
        Assert.IsNotNull(config, $"level config not found at {LevelConfigPath}");
        var runConfig = UnityEngine.Object.Instantiate(config);
        runConfig.GetType().GetField("showTutorial").SetValue(runConfig, false);
        GameManagerType.GetField("currentLevelConfig").SetValue(null, runConfig);

        // Main.unity ships with AutoPlayController.forceAutoPlay ticked, so switch it off before Start runs
        SceneManager.sceneLoaded += DisableAutoPlay;
        yield return SceneManager.LoadSceneAsync("Main");
        SceneManager.sceneLoaded -= DisableAutoPlay;
        AudioListener.volume = 0f;
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 100;
        }

        var gameManagerObject = GameObject.Find("GameManager");
        var gameManager = gameManagerObject.GetComponent(GameManagerType);
        var conductor = gameManagerObject.GetComponent(CupConductorType);
        var notes = (IReadOnlyList<Note>)CupConductorType.GetProperty("Notes").GetValue(conductor);
        var songBeatField = GameManagerType.GetField("songPositionInBeats");
        Assert.Greater(notes.Count, 0, "no notes loaded");
        float secPerBeat = (float)CupConductorType.GetField("SecPerBeat").GetValue(null);

        var cues = PlanHuman(notes, secPerBeat, new System.Random(Seed));
        float endBeat = EndBeatOverride > 0f ? EndBeatOverride : notes.Max(n => n.endBeat) + 4f;
        Debug.Log($"[Human] {cues.Count} cues planned over {notes.Count} notes, recording to beat {endBeat:F1}");

        var setSqueezing = TeatType.GetMethod("SetSqueezing");
        var forLane = TeatType.GetMethod("ForLane");
        var manifest = new List<string>();
        int cueIndex = 0;
        float lastShot = -1f;
        float beat = 0f;
        float deadline = Time.realtimeSinceStartup + endBeat * secPerBeat + 60f;
        while (beat < endBeat)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, $"song only reached beat {beat:F1}; is audio advancing?");
            beat = (float)songBeatField.GetValue(gameManager);

            while (cueIndex < cues.Count && cues[cueIndex].beat <= beat)
            {
                Cue cue = cues[cueIndex++];
                object teat = forLane.Invoke(null, new object[] { cue.lane });
                if (teat != null) setSqueezing.Invoke(teat, new object[] { cue.kind == Kind.Press });
                Debug.Log($"[Human] {cue.kind} {cue.lane} at beat {beat:F2} ({cue.why})");
            }

            if (beat > 0f && beat - lastShot >= 1f / FramesPerBeat)
            {
                lastShot = beat;
                string file = System.IO.Path.Combine(VideoDir, $"frame_{manifest.Count:D5}.png");
                yield return AutoplaySmokeTests.Capture(file);
                float shotBeat = (float)songBeatField.GetValue(gameManager);
                int score = (int)GameManagerType.GetProperty("CurrentScore").GetValue(gameManager);
                int streak = (int)GameManagerType.GetProperty("CurrentStreak").GetValue(gameManager);
                manifest.Add($"{System.IO.Path.GetFileName(file)}\t{shotBeat:F4}\t{score}\t{streak}");
            }
            yield return null;
        }

        System.IO.File.WriteAllLines(System.IO.Path.Combine(VideoDir, "manifest.tsv"), manifest);
        System.IO.File.WriteAllLines(System.IO.Path.Combine(VideoDir, "notes.tsv"),
            notes.Select(n => $"{n.lane}\t{n.startBeat:F3}\t{n.endBeat:F3}\t{n.state}\t{n.pressBeat:F3}\t{n.pressJudgment}\t{n.releaseBeat:F3}\t{n.releaseJudgment}"));
        System.IO.File.WriteAllLines(System.IO.Path.Combine(VideoDir, "cues.tsv"), cues.Select(c => $"{c.kind}\t{c.lane}\t{c.beat:F3}\t{c.why}"));
        int perfectPress = notes.Count(n => n.pressJudgment == BeatTiming.OnTime);
        int tipped = notes.Count(n => n.pressJudgment.HasValue && n.pressJudgment != BeatTiming.OnTime);
        int missed = notes.Count(n => n.state == NoteState.Missed);
        int lateRelease = notes.Count(n => n.releaseJudgment == BeatTiming.TooLate);
        int earlyRelease = notes.Count(n => n.releaseJudgment == BeatTiming.TooEarly);
        int finalScore = (int)GameManagerType.GetProperty("CurrentScore").GetValue(gameManager);
        Debug.Log($"[Human] done: {manifest.Count} frames, {perfectPress} perfect presses, {tipped} tipped, {missed} missed, {earlyRelease} early / {lateRelease} late releases, score {finalScore}, secPerBeat {secPerBeat:F4}");
        Assert.Greater(manifest.Count, 0, "no frames captured");
    }

    // A believable player: mostly on time with ~70 ms jitter, the odd flub, a few complete misses,
    // holds that run long, and a fumble now and then on the dense stretches
    private static List<Cue> PlanHuman(IReadOnlyList<Note> notes, float secPerBeat, System.Random random)
    {
        var cues = new List<Cue>();
        var byLane = notes.GroupBy(n => n.lane);
        foreach (var lane in byLane)
        {
            Note[] laneNotes = lane.OrderBy(n => n.startBeat).ToArray();
            for (int i = 0; i < laneNotes.Length; i++)
            {
                Note note = laneNotes[i];
                float nextStart = i + 1 < laneNotes.Length ? laneNotes[i + 1].startBeat : float.MaxValue;
                double roll = random.NextDouble();
                float pressSeconds, releaseSeconds;
                string why;
                if (roll < 0.05) { continue; }                                              // never squeezed: a miss
                else if (roll < 0.11) { pressSeconds = -(0.30f + 0.20f * (float)random.NextDouble()); why = "jumped the gun"; }
                else if (roll < 0.17) { pressSeconds = 0.28f + 0.12f * (float)random.NextDouble(); why = "late grab"; }
                else { pressSeconds = Gaussian(random, 0f, 0.07f, 0.22f); why = "ok press"; }

                double releaseRoll = random.NextDouble();
                if (releaseRoll < 0.12) { releaseSeconds = 0.30f + 0.20f * (float)random.NextDouble(); why += ", held too long"; }
                else if (releaseRoll < 0.20) { releaseSeconds = -(0.28f + 0.12f * (float)random.NextDouble()); why += ", let go early"; }
                else { releaseSeconds = Gaussian(random, 0f, 0.08f, 0.22f); }

                float pressBeat = note.startBeat + pressSeconds / secPerBeat;
                float releaseBeat = note.endBeat + releaseSeconds / secPerBeat;
                if (Mathf.Abs(pressSeconds) > 0.25f) releaseBeat = pressBeat + 0.25f / secPerBeat;      // a tipped cup: let go right away
                releaseBeat = Mathf.Max(releaseBeat, pressBeat + 0.1f);
                releaseBeat = Mathf.Min(releaseBeat, nextStart - 0.15f);                                    // hands off before the next cup in this lane
                if (releaseBeat <= pressBeat) continue;
                cues.Add(new Cue { beat = pressBeat, lane = note.lane, kind = Kind.Press, why = why });
                cues.Add(new Cue { beat = releaseBeat, lane = note.lane, kind = Kind.Release, why = why });
            }
        }
        return cues.OrderBy(c => c.beat).ToList();
    }

    private static void DisableAutoPlay(Scene scene, LoadSceneMode mode)
    {
        var autoPlayType = Type.GetType("AutoPlayController, Assembly-CSharp");
        var autoPlay = UnityEngine.Object.FindObjectOfType(autoPlayType);
        Assert.IsNotNull(autoPlay, "AutoPlayController not found in Main scene");
        autoPlayType.GetField("forceAutoPlay").SetValue(autoPlay, false);
    }

    private static float Gaussian(System.Random random, float mean, float sigma, float clamp)
    {
        double u1 = 1.0 - random.NextDouble(), u2 = random.NextDouble();
        float value = mean + sigma * (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        return Mathf.Clamp(value, -clamp, clamp);
    }
}
#endif
