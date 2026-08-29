using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Размеры поля")]
    public int width = 10;
    public int height = 10;

    [Header("Префаб ячейки")]
    public GameObject tilePrefab; 

    private void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Спавним клетки на плоской поверхности XZ
                Vector3 spawnPos = new Vector3(x, 0f, z);
                Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
            }
        }
    }
}