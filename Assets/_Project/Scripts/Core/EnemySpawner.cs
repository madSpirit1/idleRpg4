using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Настройки спавна")]
    public GameObject baseEnemyPrefab; // Сюда перетаскиваем наш универсальный префаб
    public int enemiesToSpawn = 3;

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

    // Метод стал PUBLIC. Теперь его контролирует генератор сетки!
    public void SpawnEnemiesOnReadyGrid()
    {
        if (baseEnemyPrefab == null)
        {
            Debug.LogError("Base Enemy Prefab is not assigned in EnemySpawner!");
            return;
        }

        int mapWidth = GridGenerator.Instance.width;
        int mapHeight = GridGenerator.Instance.height;

        int spawnedCount = 0;
        int safetyNet = 0;

        while (!(spawnedCount >= enemiesToSpawn) && !(safetyNet >= 200))
        {
            safetyNet++;

            int randomX = Random.Range(0, mapWidth);
            int randomZ = Random.Range(0, mapHeight);
            Vector2Int potentialPos = new Vector2Int(randomX, randomZ);

            if (GridGenerator.Instance.IsCellWalkable(potentialPos))
            {
                if (potentialPos != Vector2Int.zero)
                {
                    Vector3 spawnWorldPos = new Vector3((float)randomX, 0.1f, (float)randomZ);
                    
                    GameObject newEnemy = Instantiate(baseEnemyPrefab, spawnWorldPos, Quaternion.identity);
                    newEnemy.name = "Enemy_Instance_" + spawnedCount;

                    EnemyAI enemyAI = newEnemy.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.InitializeEnemy("bandit");
                    }

                    spawnedCount++;
                }
            }
        }
        
        Debug.Log("Менеджер спавна успешно создал врагов: " + spawnedCount);
    }
}
