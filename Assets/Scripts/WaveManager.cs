using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave";
        public int zombieCount = 5;
        public float spawnRate = 2f;
        public GameObject zombiePrefab;
    }

    [Header("Wave Configuration")]
    public Transform[] spawnPoints;
    public Wave[] waves;
    public int currentWaveIndex = 0;

    [Header("State")]
    public int zombiesAlive = 0;
    private bool isSpawning = false;
    private bool gameWon = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (waves != null && waves.Length > 0 && spawnPoints != null && spawnPoints.Length > 0)
        {
            StartCoroutine(StartWave());
        }
        else
        {
            Debug.LogWarning("WaveManager is missing waves or spawn points!");
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;
        
        if (gameWon) return;

        // Si la vague est terminée (plus de zombies à spawn et tous les zombies sont morts)
        if (!isSpawning && zombiesAlive <= 0)
        {
            currentWaveIndex++;
            
            if (currentWaveIndex < waves.Length)
            {
                StartCoroutine(StartWave());
            }
            else
            {
                gameWon = true;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Victory();
                }
            }
        }
    }

    IEnumerator StartWave()
    {
        isSpawning = true;
        Wave currentWave = waves[currentWaveIndex];
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage("Wave " + (currentWaveIndex + 1) + ": " + currentWave.waveName, 3f);
        }

        // Auto-Save au début de chaque vague
        SaveSystem saveSys = Object.FindFirstObjectByType<SaveSystem>();
        if (saveSys != null)
        {
            saveSys.SaveGame();
        }

        yield return new WaitForSeconds(3f); // Temps de répit avant que les zombies n'arrivent

        for (int i = 0; i < currentWave.zombieCount; i++)
        {
            SpawnZombie(currentWave.zombiePrefab);
            yield return new WaitForSeconds(currentWave.spawnRate);
        }

        isSpawning = false;
    }

    void SpawnZombie(GameObject prefab)
    {
        if (prefab == null) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject zombie = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        
        zombiesAlive++;
    }

    // Appelé par le script ZombieHealth quand un zombie meurt
    public void OnZombieDied()
    {
        zombiesAlive--;
        if (zombiesAlive < 0) zombiesAlive = 0;
    }
}
