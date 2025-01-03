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
        // No need to manage scheduled presses here
    }

    // Example: call this method when setting up each note in CupConductor
    public void ScheduleTeatPress(string teatType, float timeUntilPress, float squeezeDuration)
    {
        if (teatMap.TryGetValue(teatType, out TeatController teat))
        {
            double pressTime = AudioSettings.dspTime + timeUntilPress - perfectAdvanceTime;
            double releaseTime = pressTime + squeezeDuration;
            teat.SetSqueezing(pressTime, releaseTime);
            Debug.Log($"Scheduled teat press for {teatType} at {pressTime} with release at {releaseTime}");
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