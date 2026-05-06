using UnityEngine;
using UnityEngine.UI;

public class RuntimeMenuBinder : MonoBehaviour
{
    public Button saveButton;
    public Button loadButton;
    public Button resumeButton;

    private SaveSystem saveSystem;
    private GameManager gameManager;

    void Start()
    {
        saveSystem = FindFirstObjectByType<SaveSystem>();
        gameManager = GameManager.Instance;

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() => 
            {
                if (saveSystem != null) saveSystem.SaveGame();
            });
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(() => 
            {
                if (saveSystem != null)
                {
                    saveSystem.LoadGame();
                    // Update UI after loading
                    if (UIManager.Instance != null && gameManager != null)
                    {
                        UIManager.Instance.UpdateScoreUI(gameManager.totalScore, gameManager.sessionTime);
                    }
                }
            });
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() => 
            {
                if (gameManager != null) gameManager.TogglePause();
            });
        }
    }
}