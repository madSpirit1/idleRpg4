using UnityEngine;

[CreateAssetMenu(fileName = "NewTileConfig", menuName = "Grid/Tile Config")]
public class TileDataConfig : ScriptableObject
{
    public string tileName;        // Название (например, "Трава", "Стена", "Вода")
    public bool isWalkable;        // Можно ли ходить
    public Sprite tileSprite;      // Картинка тайла
    public Color debugColor = Color.white; // Цвет, если картинки еще нет
}