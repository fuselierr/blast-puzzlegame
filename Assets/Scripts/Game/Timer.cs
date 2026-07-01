using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private float startTime = 60f;
    [SerializeField] private bool isCountingDown = true;

    private float timeRemaining;

    private void Start()
    {
        timeRemaining = startTime;
    }

    private void Update()
    {
        if (isCountingDown)
        {
            if (timeRemaining <= 0f)
            {
                return;
            }

            timeRemaining -= Time.deltaTime;
        }
        else
        {
            timeRemaining += Time.deltaTime;
        }
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

    public bool IsTimerFinished()
    {
        return timeRemaining <= 0f;
    }
}
