using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;

[System.Serializable]
public class SpawnableObject
{
    public GameObject prefab;
    [Range(0f, 1f)] public float spawnWeight = 0.5f; // 0-1 for weighted randomness
    public float minPadding = 0.5f;
}

public class GameManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private SpawnableObject[] objectPrefabs;
    [SerializeField] private int maxActiveObjects = 50; // Prevents performance issues
    
    [SerializeField] private float initialSpawnRate = 1.5f;
    [SerializeField] private float minSpawnRate = 0.5f;
    [SerializeField] private float difficultyScale = 0.02f;
    [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Camera mainCam;
    private float currentSpawnRate;
    private float nextSpawnTime;
    private float minX, maxX;
    private float spawnY;
    private int totalSpawned = 0;

    // Object pooling
    private Dictionary<GameObject, Queue<GameObject>> objectPools = new();
    private List<GameObject> activeObjects = new();
    private GameObject lastSpawnedPrefab = null; // Track last spawn to vary spawns

    [Header("Game Settings")]
    [SerializeField] private string gameOverScene;

    public int score = 0;
    private int lives = 3; // Player starts with 3 lives

    [Header("UI references")]
    [SerializeField] private TextMeshProUGUI scoreText;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        InitializeObjectPools();
        CalculateSpawnArea();
        currentSpawnRate = initialSpawnRate;
    }

    void Update()
    {
        // Don't spawn if we've hit the max active objects
        if (activeObjects.Count >= maxActiveObjects)
            return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnObject();
            
            // Difficulty progression with curve for better scaling
            float progressionFactor = Mathf.Clamp01((totalSpawned * difficultyScale) / 10f);
            float curvedDifficulty = difficultyCurve.Evaluate(progressionFactor);
            currentSpawnRate = Mathf.Lerp(initialSpawnRate, minSpawnRate, curvedDifficulty);
            
            nextSpawnTime = Time.time + currentSpawnRate;
            totalSpawned++;
        }

        // Clean up destroyed or deactivated objects from active list
        activeObjects.RemoveAll(obj => obj == null || !obj.activeSelf);
        UpdateScoreUI();
    }

    private void InitializeObjectPools()
    {
        foreach (var spawnObj in objectPrefabs)
        {
            if (!objectPools.ContainsKey(spawnObj.prefab))
            {
                objectPools[spawnObj.prefab] = new Queue<GameObject>();
            }
        }
    }

    private GameObject GetPooledObject(GameObject prefab)
    {
        if (objectPools[prefab].Count > 0)
        {
            GameObject pooledObj = objectPools[prefab].Dequeue();
            pooledObj.SetActive(true);
            return pooledObj;
        }

        // Create new object if pool is empty
        return Instantiate(prefab);
    }

    private void ReturnToPool(GameObject obj, GameObject prefab)
    {
        obj.SetActive(false);
        objectPools[prefab].Enqueue(obj);
    }

    private void SpawnObject()
    {
        if (objectPrefabs.Length == 0) return;

        // Weighted randomness
        SpawnableObject spawnObj = GetWeightedRandomObject();
        GameObject prefab = spawnObj.prefab;

        float randomX = Random.Range(minX + spawnObj.minPadding, maxX - spawnObj.minPadding);
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0);

        GameObject obj = GetPooledObject(prefab);
        obj.transform.position = spawnPos;
        activeObjects.Add(obj);
    }

    private SpawnableObject GetWeightedRandomObject()
    {
        float totalWeight = 0f;
        foreach (var obj in objectPrefabs)
            totalWeight += obj.spawnWeight;

        // Try to pick something different from last spawn
        SpawnableObject selected = null;
        int attempts = 0;
        
        do
        {
            float randomValue = Random.value * totalWeight;
            float cumulativeWeight = 0f;

            foreach (var obj in objectPrefabs)
            {
                cumulativeWeight += obj.spawnWeight;
                if (randomValue <= cumulativeWeight)
                {
                    selected = obj;
                    break;
                }
            }
            
            attempts++;
        } while (selected != null && lastSpawnedPrefab == selected.prefab && attempts < 3 && objectPrefabs.Length > 1);

        if (selected != null)
            lastSpawnedPrefab = selected.prefab;

        return selected ?? objectPrefabs[0];
    }

    private void CalculateSpawnArea()
    {
        float distance = 10f;
        float screenLeft = mainCam.ViewportToWorldPoint(new Vector3(0, 0, distance)).x;
        float screenRight = mainCam.ViewportToWorldPoint(new Vector3(1, 0, distance)).x;
        spawnY = mainCam.ViewportToWorldPoint(new Vector3(0, 1.1f, distance)).y;

        minX = screenLeft;
        maxX = screenRight;
    }

    // Call this when an object is caught/destroyed
    public void OnObjectRemoved(GameObject obj)
    {
        if (activeObjects.Contains(obj))
            activeObjects.Remove(obj);

        // Return to pool instead of destroying
        bool pooled = false;
        foreach (var prefab in objectPrefabs)
        {
            if (obj.name.Contains(prefab.prefab.name))
            {
                ReturnToPool(obj, prefab.prefab);
                pooled = true;
                break;
            }
        }
        
        // If not pooled, deactivate the object to ensure it's hidden
        if (!pooled)
        {
            obj.SetActive(false);
        }
    }

    // Increase the score
    public void AddScore(int points)
    {
        score += points;
        Debug.Log("Score: " + score);
    }

    public int GetScore()
    {
        return score;
    }
    private void UpdateScoreUI(){
        if(scoreText != null)
        {
            scoreText.text = $"Momo: {score}";
        }
    }

    // Lives management
    public int GetLives()
    {
        return lives;
    }

    public void LoseLife()
    {
        lives--;
        Debug.Log("Lives remaining: " + lives);

        // Check if game over (when lives reach 0)
        if (lives <= 0)
        {
            EndGame();
        }
    }

    public void EndGame()
    {
        StartCoroutine(GameOverSequence());
    }

    private System.Collections.IEnumerator GameOverSequence()
    {
        // Disable spawning immediately
        enabled = false;

        // Pause game smoothly
        Time.timeScale = 0f;

        // Optional: Add a short delay for polish (feels less jarring)
        yield return new WaitForSecondsRealtime(0.5f);

        // Load game over scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverScene);
    }
}