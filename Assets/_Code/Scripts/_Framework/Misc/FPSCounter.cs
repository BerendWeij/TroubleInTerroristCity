using System;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public float updateInterval = 0.5f; // Update interval in seconds.

    private float accum; // FPS accumulated over the interval.
    private int frames; // Frames drawn over the interval.
    private float timeLeft; // Left time for current interval.
    
    public TMP_Text display_Text;

    private void Start()
    {
        timeLeft = updateInterval;
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        // Interval ended - update GUI text and start new interval.
        if (timeLeft <= 0.0)
        {
            // Display FPS in the console.
            float fps = accum / frames;
            string format = String.Format("{0:F2} FPS", fps);
            display_Text.text = format;

            // Reset values.
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}