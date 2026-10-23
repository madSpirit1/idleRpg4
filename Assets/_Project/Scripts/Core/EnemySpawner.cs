using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Настройки спавна")]
    public GameObject baseEnemyPrefab; // Сюда перетаскиваем наш универсальный префаб
    public int enemiesToSpawn = 3;

    private int _spawnCounter;

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

    // Сбрасываем статическую ссылку, иначе после перезагрузки сцены Instance
    // указывает на уничтоженный объект и новый спавнер убивает сам себя в Awake.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

        // Клетки, занятые игроком и уже созданными врагами
        HashSet<Vector2Int> occupied = CollectOccupiedCells();

        int spawnedCount = 0;
        int safetyNet = 0;

        while (spawnedCount < enemiesToSpawn && safetyNet < 200)
        {
            safetyNet++;

            int randomX = Random.Range(0, mapWidth);
            int randomZ = Random.Range(0, mapHeight);
            Vector2Int potentialPos = new Vector2Int(randomX, randomZ);

            if (!GridGenerator.Instance.IsCellWalkable(potentialPos)) continue;
            if (occupied.Contains(potentialPos)) continue;

            if (SpawnEnemyAt("bandit", potentialPos) == null) continue;

            occupied.Add(potentialPos);
            spawnedCount++;
        }

        Debug.Log("Менеджер спавна успешно создал врагов: " + spawnedCount);

        if (spawnedCount < enemiesToSpawn)
        {
            Debug.LogWarning("Спавн: создано " + spawnedCount + " из " + enemiesToSpawn + " — свободных клеток не хватило.");
        }
    }

    // --- СПАВН ПО ID (используется консолью) ---

    // Создать врага с указанным id на конкретной клетке.
    // Возвращает null, если id неизвестен, префаб не задан или клетка непроходима.
    public GameObject SpawnEnemyAt(string enemyId, Vector2Int cell)
    {
        if (baseEnemyPrefab == null)
        {
            Debug.LogError("Base Enemy Prefab is not assigned in EnemySpawner!");
            return null;
        }

        if (GridGenerator.Instance == null || !GridGenerator.Instance.IsCellWalkable(cell))
        {
            return null;
        }

        if (!EnemyDatabase.Contains(enemyId))
        {
            Debug.LogWarning("EnemySpawner: неизвестный id врага '" + enemyId + "'");
            return null;
        }

        Vector3 spawnWorldPos = new Vector3((float)cell.x, 0.1f, (float)cell.y);

        GameObject newEnemy = Instantiate(baseEnemyPrefab, spawnWorldPos, Quaternion.identity);
        newEnemy.name = "Enemy_" + enemyId + "_" + _spawnCounter;
        _spawnCounter++;

        EnemyAI enemyAI = newEnemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.InitializeEnemy(enemyId);
            // Start() выполнится позже, поэтому клетку проставляем руками прямо сейчас
            enemyAI.PlaceOnCell(cell);
        }

        return newEnemy;
    }

    // Создать врага рядом с игроком. Клетка выбирается случайно среди достижимых
    // (BFS по проходимым тайлам), свободных от других фишек.
    public GameObject SpawnEnemyNearPlayer(string enemyId, int maxRadius, out Vector2Int spawnedCell)
    {
        spawnedCell = Vector2Int.zero;

        if (GridGenerator.Instance == null)
        {
            Debug.LogError("EnemySpawner: на сцене нет GridGenerator.");
            return null;
        }

        GridController player = FindAnyObjectByType<GridController>();
        if (player == null)
        {
            Debug.LogError("EnemySpawner: на сцене нет GridController (игрока).");
            return null;
        }

        List<Vector2Int> candidates = FindFreeCellsAround(player.GridPosition, Mathf.Max(1, maxRadius));
        if (candidates.Count == 0) return null;

        Vector2Int cell = candidates[Random.Range(0, candidates.Count)];

        GameObject spawned = SpawnEnemyAt(enemyId, cell);
        if (spawned == null) return null;

        spawnedCell = cell;
        return spawned;
    }

    // Все достижимые проходимые клетки в радиусе maxRadius (по сетке), кроме занятых.
    // BFS гарантирует, что враг появится в той же области карты, что и игрок, а не за стеной.
    private List<Vector2Int> FindFreeCellsAround(Vector2Int center, int maxRadius)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        GridGenerator grid = GridGenerator.Instance;
        HashSet<Vector2Int> occupied = CollectOccupiedCells();

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        visited.Add(center);
        queue.Enqueue(center);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            int distX = Mathf.Abs(current.x - center.x);
            int distY = Mathf.Abs(current.y - center.y);
            int distance = Mathf.Max(distX, distY);

            // dist == 0 — это клетка самого игрока, туда спавнить нельзя
            if (distance > 0 && !occupied.Contains(current))
            {
                result.Add(current);
            }

            if (distance >= maxRadius) continue;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int nextStep = current + directions[i];
                if (!grid.IsCellWalkable(nextStep)) continue;
                if (!visited.Add(nextStep)) continue;
                queue.Enqueue(nextStep);
            }
        }

        return result;
    }

    // Клетки, занятые игроком и всеми врагами на сцене
    private HashSet<Vector2Int> CollectOccupiedCells()
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        GridController player = FindAnyObjectByType<GridController>();
        if (player != null) occupied.Add(player.GridPosition);

        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null) occupied.Add(enemies[i].GridPosition);
        }

        return occupied;
    }
}
