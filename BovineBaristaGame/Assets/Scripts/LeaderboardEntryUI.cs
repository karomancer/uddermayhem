using UnityEngine;
using TMPro;

/// <summary>
/// Simple component to hold references to the 3 text columns in a leaderboard row.
/// </summary>
public class LeaderboardEntryUI : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text initialsText;
    public TMP_Text tipsText;

    public void SetEntry(int rank, string initials, double tips, Color color)
    {
        if (rankText != null) rankText.text = $"{rank}.";
        if (initialsText != null) initialsText.text = initials;
        if (tipsText != null) tipsText.text = $"${tips:F2}";

        // Apply color to all columns
        if (rankText != null) rankText.color = color;
        if (initialsText != null) initialsText.color = color;
        if (tipsText != null) tipsText.color = color;
    }

    public void SetEmpty(int rank, Color color)
    {
        if (rankText != null) rankText.text = $"{rank}.";
        if (initialsText != null) initialsText.text = "---";
        if (tipsText != null) tipsText.text = "$0.00";

        if (rankText != null) rankText.color = color;
        if (initialsText != null) initialsText.color = color;
        if (tipsText != null) tipsText.color = color;
    }
}
