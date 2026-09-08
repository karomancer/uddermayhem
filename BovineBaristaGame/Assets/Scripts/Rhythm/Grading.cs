using System;

/// <summary>
/// Turns a final score into a Grade. The ceiling is what a flawless run of the chart earns under the
/// game's rules: every note is pressed and released, both judged Perfect; the press scores at the
/// current streak multiplier, the release bumps the streak first and then scores.
/// </summary>
public static class Grading
{
    public const int EventsPerNote = 2;

    public static int MaxScore(int noteCount, int perfectPoints, int[] streakTiers, int[] multipliers)
    {
        int total = 0;
        for (int i = 1; i <= noteCount; i++)
        {
            total += perfectPoints * MultiplierFor(i - 1, streakTiers, multipliers);
            total += perfectPoints * MultiplierFor(i, streakTiers, multipliers);
        }
        return total;
    }

    public static int MultiplierFor(int streak, int[] streakTiers, int[] multipliers)
    {
        if (streakTiers == null || multipliers == null || streakTiers.Length == 0 || streakTiers.Length != multipliers.Length) return 1;
        int multiplier = multipliers[0];
        for (int i = streakTiers.Length - 1; i >= 0; i--)
        {
            if (streak >= streakTiers[i]) { multiplier = multipliers[i]; break; }
        }
        return multiplier;
    }

    /// <summary>Share of the ceiling earned, 0..1.</summary>
    public static float Accuracy(int score, int maxScore)
    {
        return maxScore <= 0 ? 0f : Math.Min(1f, Math.Max(0f, (float)score / maxScore));
    }

    /// <summary>Perfect only when every event was judged Perfect, which is the only way to reach the ceiling.</summary>
    public static Grade GradeFor(int score, int maxScore, float goodThreshold, float superbThreshold)
    {
        if (maxScore > 0 && score >= maxScore) return Grade.Perfect;
        float accuracy = Accuracy(score, maxScore);
        if (accuracy >= superbThreshold) return Grade.Superb;
        if (accuracy >= goodThreshold) return Grade.Good;
        return Grade.Failed;
    }
}
