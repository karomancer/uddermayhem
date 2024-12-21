using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simulates perfect squeezes for each TeatController
public class AutoTeatManager : MonoBehaviour
{
  public TeatController[] teats; // Assign in inspector
  public float perfectAdvanceTime = 0.1f; // How early to press before the perfect beat
  private Dictionary<string, TeatController> teatMap;

  void Start()
  {
    teatMap = new Dictionary<string, TeatController>();
    foreach (TeatController teat in teats)
    {
      string teatType = teat.gameObject.name.Split("_")[1];
      teatMap[teatType] = teat;
    }
  }

  // Example: call this method when setting up each note in CupConductor
  public void ScheduleTeatPress(string teatType, float timeUntilPress, float squeezeDuration)
  {
    if (teatMap.TryGetValue(teatType, out TeatController teat))
    {
      StartCoroutine(PressTeat(teat, timeUntilPress - perfectAdvanceTime, squeezeDuration));
    }
    else
    {
      Debug.LogWarning($"Teat type {teatType} not found in teatMap.");
    }
  }

  private IEnumerator PressTeat(TeatController teat, float offset, float squeezeDuration)
  {
    yield return new WaitForSeconds(offset);
    teat.SetSqueezing(true);
    yield return new WaitForSeconds(squeezeDuration);
    teat.SetSqueezing(false);
  }
}