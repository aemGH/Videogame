using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameOver = false;
    public bool isGamePaused = false;
    
    [Header("Session Data")]
    public float sessionTime = 0f;
    public int totalScore = 0;
    
    // Pour la diversité des objets collectés
    public List<string> collectedItemTypes = new List<string>();

    private void Awake()
    {
        // Singleton pattern pour s'assurer qu'il n'y a qu'un seul GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optionnel si plusieurs scènes
        }
        else
        {
            Destroy(gameObject);
            return; // Arrêter l'exécution pour ce duplicata
        }

        // IMPORTANT : Réinitialiser les valeurs au démarrage (Crucial si Domain Reload est désactivé dans Unity 6)
        isGameOver = false;
        isGamePaused = false;
        sessionTime = 0f;
        totalScore = 0;
        if (collectedItemTypes == null) collectedItemTypes = new List<string>();
        collectedItemTypes.Clear();
        Time.timeScale = 1f; // S'assurer que le temps tourne
    }

    private void Update()
    {
        if (!isGameOver && !isGamePaused)
        {
            // Augmenter le temps de survie
            sessionTime += Time.deltaTime;

            // Mettre à jour l'UI du temps chaque frame
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimeUI(sessionTime);
            }
        }

        // Pause avec la touche Echap
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void AddScore(int amount, string itemType)
    {
        totalScore += amount;

        // Gérer la diversité des objets (un des critères du thème)
        if (!collectedItemTypes.Contains(itemType))
        {
            collectedItemTypes.Add(itemType);
            totalScore += 50; // Bonus pour la découverte d'un nouveau type d'objet
        }

        // Mettre à jour l'UI (sera appelé plus tard)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScoreUI(totalScore, sessionTime);
        }
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f; // Arrêter le temps
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverPanel(sessionTime, totalScore);
        }
    }

    public void Victory()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictoryPanel(sessionTime, totalScore);
        }
    }

    public void TogglePause()
    {
        if (isGameOver) return;

        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePauseMenu(isGamePaused);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        isGamePaused = false;
        sessionTime = 0f;
        totalScore = 0;
        collectedItemTypes.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
