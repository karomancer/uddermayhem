using NUnit.Framework;

public class GradingTests
{
    private static readonly int[] Tiers = { 0, 10, 25, 50 };
    private static readonly int[] Multipliers = { 1, 2, 3, 4 };

    [Test]
    public void MultiplierFollowsTiers()
    {
        Assert.AreEqual(1, Grading.MultiplierFor(0, Tiers, Multipliers));
        Assert.AreEqual(1, Grading.MultiplierFor(9, Tiers, Multipliers));
        Assert.AreEqual(2, Grading.MultiplierFor(10, Tiers, Multipliers));
        Assert.AreEqual(3, Grading.MultiplierFor(25, Tiers, Multipliers));
        Assert.AreEqual(4, Grading.MultiplierFor(50, Tiers, Multipliers));
        Assert.AreEqual(4, Grading.MultiplierFor(500, Tiers, Multipliers));
    }

    [Test]
    public void MaxScoreCountsPressAtCurrentStreakAndReleaseAfterIncrement()
    {
        // One note: press at streak 0 (1x) + release at streak 1 (1x)
        Assert.AreEqual(48, Grading.MaxScore(1, 24, Tiers, Multipliers));
        // Ten notes: the tenth release lands on streak 10 and gets 2x
        Assert.AreEqual(24 * (10 + 11), Grading.MaxScore(10, 24, Tiers, Multipliers));
    }

    [Test]
    public void MaxScoreForTheShippedCharts()
    {
        Assert.AreEqual(16728, Grading.MaxScore(108, 24, Tiers, Multipliers), "Medium");
        Assert.AreEqual(27480, Grading.MaxScore(164, 24, Tiers, Multipliers), "Hard");
        Assert.AreEqual(1848, Grading.MaxScore(24, 24, Tiers, Multipliers), "Easy");
    }

    [Test]
    public void GradeBands()
    {
        const int max = 10000;
        Assert.AreEqual(Grade.Failed, Grading.GradeFor(0, max, 0.6f, 0.8f));
        Assert.AreEqual(Grade.Failed, Grading.GradeFor(5999, max, 0.6f, 0.8f));
        Assert.AreEqual(Grade.Good, Grading.GradeFor(6000, max, 0.6f, 0.8f));
        Assert.AreEqual(Grade.Good, Grading.GradeFor(7999, max, 0.6f, 0.8f));
        Assert.AreEqual(Grade.Superb, Grading.GradeFor(8000, max, 0.6f, 0.8f));
        Assert.AreEqual(Grade.Superb, Grading.GradeFor(9999, max, 0.6f, 0.8f));
        Assert.AreEqual(Grade.Perfect, Grading.GradeFor(10000, max, 0.6f, 0.8f));
    }

    [Test]
    public void EmptyChartNeverGradesPerfect()
    {
        Assert.AreEqual(Grade.Failed, Grading.GradeFor(0, 0, 0.6f, 0.8f));
        Assert.AreEqual(0f, Grading.Accuracy(0, 0));
    }
}
