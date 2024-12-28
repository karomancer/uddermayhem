using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simulates perfect squeezes for each TeatController
public class AutoTeatManager : MonoBehaviour
{
    public TeatController[] teats; // Assign in inspector
    public float perfectAdvanceTime = 0.1f; // How early to press before the perfect beat
    private Dictionary<string, TeatController> teatMap;
    private AudioSource bellAudioSource;

    private class ScheduledTeatPress
    {
        public TeatController teat;
        public double pressTime;
        public double releaseTime;
        public bool isSqueezing; // Flag to track squeeze state

        public ScheduledTeatPress(TeatController teat, double pressTime, double releaseTime)
        {
            this.teat = teat;
            this.pressTime = pressTime;
            this.releaseTime = releaseTime;
            this.isSqueezing = false; // Initialize flag to false
        }
    }

    private List<ScheduledTeatPress> scheduledTeatPresses = new List<ScheduledTeatPress>();

    void Start()
    {
        teatMap = new Dictionary<string, TeatController>();
        foreach (TeatController teat in teats)
        {
            string teatType = teat.gameObject.name.Split("_")[1];
            teatMap[teatType] = teat;
        }
        bellAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        double currentTime = AudioSettings.dspTime;
        for (int i = scheduledTeatPresses.Count - 1; i >= 0; i--)
        {
            ScheduledTeatPress press = scheduledTeatPresses[i];
            if (currentTime >= press.pressTime && currentTime < press.releaseTime)
            {
                if (!press.isSqueezing)
                {
                    press.teat.SetSqueezing(true);
                    PlayBellSound();
                    press.isSqueezing = true; // Set flag to true
                }
            }
            else if (currentTime >= press.releaseTime)
            {
                if (press.isSqueezing)
                {
                    press.teat.SetSqueezing(false);
                    press.isSqueezing = false; // Reset flag
                }
                scheduledTeatPresses.RemoveAt(i);
            }
        }
    }

    // Example: call this method when setting up each note in CupConductor
    public void ScheduleTeatPress(string teatType, float timeUntilPress, float squeezeDuration)
    {
        if (teatMap.TryGetValue(teatType, out TeatController teat))
        {
            double pressTime = AudioSettings.dspTime + timeUntilPress - perfectAdvanceTime;
            double releaseTime = pressTime + squeezeDuration;
            scheduledTeatPresses.Add(new ScheduledTeatPress(teat, pressTime, releaseTime));
        }
        else
        {
            Debug.LogWarning($"Teat type {teatType} not found in teatMap.");
        }
    }

    private void PlayBellSound()
    {
        if (bellAudioSource != null)
        {
            bellAudioSource.Play();
        }
    }
}