using System;
using UnityEngine;

[Serializable]
public class TileSaveData
{
    public Vector2Int gridPosition;
    public string configName; 
    public bool isWalkable;
}

// Обертка, так как Unity не умеет напрямую сериализовать массивы или списки на верхнем уровне JSON
[Serializable]
public class GridSaveWrapper
{
    public int width;
    public int height;
    public System.Collections.Generic.List<TileSaveData> tiles = new System.Collections.Generic.List<TileSaveData>();
}