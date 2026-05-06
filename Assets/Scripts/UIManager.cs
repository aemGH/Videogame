using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI messageText;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject pausePanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void UpdateScoreUI(int score, float time)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
        UpdateTimeUI(time);
    }

    public void UpdateTimeUI(float time)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + Mathf.RoundToInt(currentHealth) + " / " + maxHealth;
        }
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            CancelInvoke("HideMessage");
            Invoke("HideMessage", duration);
        }
    }

    private void HideMessage()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    public void ShowGameOverPanel(float finalTime, int finalScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void ShowVictoryPanel(float finalTime, int finalScore)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }

    public void TogglePauseMenu(bool isPaused)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
    }
}
