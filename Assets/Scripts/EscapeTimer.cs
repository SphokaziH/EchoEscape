
using UnityEngine;
using TMPro;

public class EscapeTimer : MonoBehaviour
{
    [Header("Timer")]
    public float startingTime = 600f;

    [Header("UI")]
    public TMP_Text timerText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverMessage;
    public GameObject winScreen;

    private float timeRemaining;
    private bool timerRunning = true;
    private bool gameFinished = false;

    void Start()
    {
        Time.timeScale = 1f;
        timeRemaining = startingTime;
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (!timerRunning || gameFinished)
            return;

        if (winScreen != null && winScreen.activeInHierarchy)
        {
            StopTimer();
            return;
        }

        if (gameOverPanel != null && gameOverPanel.activeInHierarchy)
        {
            StopTimer();
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerDisplay();
            TimeRanOut();
            return;
        }

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void TimeRanOut()
    {
        timerRunning = false;
        gameFinished = true;

        Debug.Log("GAME OVER - Time ran out.");

        if (gameOverMessage != null)
            gameOverMessage.text = "GAME OVER\nTIME RAN OUT";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    public void StopTimer()
    {
        timerRunning = false;
        gameFinished = true;
    }
}
