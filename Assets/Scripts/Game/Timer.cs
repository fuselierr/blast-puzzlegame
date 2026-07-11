using System;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private float startTime = 60f;
    [SerializeField] private bool isCountingDown = true;

    private float timeRemaining;
    private bool isPaused = false;
    private bool finishedFired = false;

    // Raised once when a count-down timer hits zero (Classic mode loss).
    public event Action OnTimerFinished;

    private void Start()
    {
        timeRemaining = startTime;
    }

    private void Update()
    {
        if (isPaused)
        {
            return;
        }
        if (isCountingDown)
        {
            if (timeRemaining <= 0f)
            {
                return;
            }

            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f && !finishedFired)
            {
                finishedFired = true;
                timeRemaining = 0f;
                OnTimerFinished?.Invoke();
            }
        }
        else
        {
            timeRemaining += Time.deltaTime;
        }
    }

    // Lets a mode set duration/direction at runtime instead of relying on
    // whatever was left in the inspector for a previous mode.
    public void Configure(float time, bool countDown)
    {
        startTime = time;
        isCountingDown = countDown;
        timeRemaining = time;
        finishedFired = false;
    }

    public string GetFormattedTime()
    {
        if (isCountingDown && timeRemaining <= 0f)
        {
            return "0:00";
        }

        float displayTime = Mathf.Max(0f, timeRemaining);
        int minutes = Mathf.FloorToInt(displayTime / 60f);
        int seconds = Mathf.FloorToInt(displayTime % 60f);
        return $"{minutes}:{seconds:00}";
    }

    public bool IsCountdownTimer()
    {
        return isCountingDown;
    }

    // Raw seconds — for a count-up timer (Freeplay) this IS the elapsed
    // time, useful as a score. For a count-down timer it's time left.
    public float TimeValue => timeRemaining;

    public bool IsTimerFinished()
    {
        return timeRemaining <= 0f;
    }

    public void ResetTimer()
    {
        timeRemaining = startTime;
        finishedFired = false;
    }

    public void PauseTimer()
    {
        isPaused = true;
    }

    public void ResumeTimer()
    {
        isPaused = false;
    }
}