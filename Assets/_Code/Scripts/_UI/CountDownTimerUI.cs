using TMPro;
using UnityEngine;

public class CountdownTimerUI : MonoBehaviour
{
    private TMP_Text _countdownText;
    private float _currentTime;

    private void Start()
    {
        _countdownText = GetComponent<TMP_Text>();

    }

    public void StartTimer(float countdownDuration)
    {
        _currentTime = countdownDuration;
        UpdateTimerDisplay();
    }

    private void Update()
    {
        if (_currentTime > 0f)
        {
            _currentTime -= Time.deltaTime;
            UpdateTimerDisplay();
        }
        else
        {
            _currentTime = 0f; // Prevent displaying negative time
            // Handle timer completion (e.g., end the game, trigger an event)
        }
    }

    private void UpdateTimerDisplay()
    {
        // Display the timer value in minutes and seconds format
        int minutes = Mathf.FloorToInt(_currentTime / 60f);
        int seconds = Mathf.FloorToInt(_currentTime % 60f);
        _countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}