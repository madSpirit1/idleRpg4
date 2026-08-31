using System;
using UnityEngine;

[Serializable]
public class TileSaveData
{
    public Vector2Int gridPosition;
    public string configName; // По имени конфига мы поймем, что это за тайл при загрузке
    public bool isWalkable;
}