using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

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

    [Header("Game Settings")]
    [SerializeField] private string gameOverScene;

    private int score = 0;

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

        // Clean up destroyed objects from active list
        activeObjects.RemoveAll(obj => obj == null);
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

        float randomValue = Random.value * totalWeight;
        float cumulativeWeight = 0f;

        foreach (var obj in objectPrefabs)
        {
            cumulativeWeight += obj.spawnWeight;
            if (randomValue <= cumulativeWeight)
                return obj;
        }

        return objectPrefabs[0];
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
        foreach (var prefab in objectPrefabs)
        {
            if (obj.name.Contains(prefab.prefab.name))
            {
                ReturnToPool(obj, prefab.prefab);
                return;
            }
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