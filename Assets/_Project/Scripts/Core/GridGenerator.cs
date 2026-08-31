using UnityEngine;
using System.Collections.Generic;

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

        _gridArray = new WorldTile[width, height];
    }

    private void Start()
    {
        GenerateRandomGrid();
    }

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