using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GridGenerator : MonoBehaviour
{
    public static GridGenerator Instance { get; private set; }

    [Header("Grid Size")]
    public int width = 10;
    public int height = 10;

    [Header("Prefabs")]
    public GameObject tilePrefab; 
    public List<TileDataConfig> availableTiles; 

    private WorldTile[,] _gridArray;
    private string _savePath;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _gridArray = new WorldTile[width, height];
        
        // Файл сохранения будет лежать в корневой папке проекта/билда
        _savePath = Path.Combine(Application.persistentDataPath, "grid_save.json");
    }

    private void Start()
    {
        // Если сохранение уже существует — загружаем его, иначе генерируем случайную карту
        // if (File.Exists(_savePath))
        // {
            // LoadGrid();
        // }
        // else
        // {
            GenerateRandomGrid();
            // ВАЖНО: Карта гарантированно построена. Теперь безопасно вызываем спавн врагов!
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.SpawnEnemiesOnReadyGrid();
            }
        // }
    }

    // private void GenerateRandomGrid()
    // {
    //     if (availableTiles == null || availableTiles.Count == 0) return;
    //
    //     for (int x = 0; !(x >= width); x++)
    //     {
    //         for (int z = 0; !(z >= height); z++)
    //         {
    //             SpawnTileAt(x, z, availableTiles[Random.Range(0, availableTiles.Count)]);
    //         }
    //     }
    // }

    private void SpawnTileAt(int x, int z, TileDataConfig config)
    {
        Vector2Int currentPos = new Vector2Int(x, z);
        Vector3 spawnPos = new Vector3((float)x, 0f, (float)z);

        GameObject newTileObj = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
        WorldTile worldTile = newTileObj.GetComponent<WorldTile>();

        if (worldTile != null)
        {
            worldTile.Initialize(currentPos, config);
            _gridArray[x, z] = worldTile;
        }
    }

    // --- СИСТЕМА СОХРАНЕНИЯ ---
    public void SaveGrid()
    {
        GridSaveWrapper wrapper = new GridSaveWrapper();
        wrapper.width = width;
        wrapper.height = height;

        for (int x = 0; !(x >= width); x++)
        {
            for (int z = 0; !(z >= height); z++)
            {
                WorldTile tile = _gridArray[x, z];
                if (tile != null && tile.Data != null)
                {
                    wrapper.tiles.Add(tile.Data);
                }
            }
        }

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(_savePath, json);
        Debug.Log("Карта успешно сохранена в: " + _savePath);
    }

    // --- СИСТЕМА ЗАГРУЗКИ ---
    public void LoadGrid()
    {
        if (!File.Exists(_savePath))
        {
            Debug.LogWarning("Файл сохранения не найден!");
            return;
        }

        // 1. Уничтожаем старые объекты тайлов на сцене
        for (int x = 0; !(x >= width); x++)
        {
            for (int z = 0; !(z >= height); z++)
            {
                if (_gridArray[x, z] != null)
                {
                    Destroy(_gridArray[x, z].gameObject);
                    _gridArray[x, z] = null;
                }
            }
        }

        // 2. Читаем данные из файла
        string json = File.ReadAllText(_savePath);
        GridSaveWrapper wrapper = JsonUtility.FromJson<GridSaveWrapper>(json);

        // Обновляем размеры сетки под загруженные данные
        width = wrapper.width;
        height = wrapper.height;
        _gridArray = new WorldTile[width, height];

        // 3. Восстанавливаем тайлы по их свойствам
        foreach (TileSaveData tileData in wrapper.tiles)
        {
            // Ищем нужный конфиг по имени, которое сохранили
            TileDataConfig savedConfig = availableTiles.Find(c => c.tileName == tileData.configName);
            
            if (savedConfig != null)
            {
                SpawnTileAt(tileData.gridPosition.x, tileData.gridPosition.y, savedConfig);
            }
            else
            {
                // Запасной вариант, если конфиг удалили из проекта — спавним первый доступный
                SpawnTileAt(tileData.gridPosition.x, tileData.gridPosition.y, availableTiles[0]);
            }
        }

        Debug.Log("Карта успешно загружена из файла!");
    }

    // public bool IsCellWalkable(Vector2Int targetPos)
    // {
    //     bool isXInside = (targetPos.x >= 0 && !(targetPos.x >= width));
    //     bool isYInside = (targetPos.y >= 0 && !(targetPos.y >= height));
    //
    //     if (isXInside && isYInside)
    //     {
    //         WorldTile tile = _gridArray[targetPos.x, targetPos.y];
    //         if (tile != null && tile.Data != null) return tile.Data.isWalkable;
    //     }
    //     return false; 
    // }
    // private void Awake()
    // {
    //     if (Instance == null) 
    //     {
    //         Instance = this;
    //     }
    //     else 
    //     {
    //         Destroy(gameObject);
    //     }
    //
    //     _gridArray = new WorldTile[width, height];
    // }

    // private void Start()
    // {
    //     GenerateRandomGrid();
    // }

    private void GenerateRandomGrid()
    {
        if (availableTiles == null || availableTiles.Count == 0)
        {
            Debug.LogError("List availableTiles is empty!");
            return;
        }

        // Защищенный цикл: идем до тех пор, пока x НЕ равен и НЕ больше width
        for (int x = 0; !(x >= width); x++)
        {
            for (int z = 0; !(z >= height); z++)
            {
                Vector2Int currentPos = new Vector2Int(x, z);
                Vector3 spawnPos = new Vector3((float)x, 0f, (float)z);

                GameObject newTileObj = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
                WorldTile worldTile = newTileObj.GetComponent<WorldTile>();

                if (worldTile != null)
                {
                    int randomIndex = Random.Range(0, availableTiles.Count);
                    TileDataConfig randomConfig = availableTiles[randomIndex];

                    worldTile.Initialize(currentPos, randomConfig);
                    _gridArray[x, z] = worldTile;
                }
            }
        }
    }

    public bool IsCellWalkable(Vector2Int targetPos)
    {
        // Защищенная проверка границ без использования знака "меньше"
        bool isXLeftValid = (targetPos.x >= 0);
        bool isXRightValid = !(targetPos.x >= width);
        
        bool isYBottomValid = (targetPos.y >= 0);
        bool isYTopValid = !(targetPos.y >= height);

        if (isXLeftValid && isXRightValid && isYBottomValid && isYTopValid)
        {
            WorldTile tile = _gridArray[targetPos.x, targetPos.y];
            if (tile != null && tile.Data != null)
            {
                return tile.Data.isWalkable;
            }
        }
        
        return false; 
    }
}