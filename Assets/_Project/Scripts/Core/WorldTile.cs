using UnityEngine;

public class WorldTile : MonoBehaviour
{
    public TileSaveData Data { get; private set; }
    
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Инициализация тайла его свойствами
    public void Initialize(Vector2Int position, TileDataConfig config)
    {
        Data = new TileSaveData
        {
            gridPosition = position,
            configName = config.tileName,
            isWalkable = config.isWalkable
        };

        // Настраиваем визуал
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = config.tileSprite;
            // Если спрайта нет, подкрасим для теста debug-цветом
            if (config.tileSprite == null)
            {
                _spriteRenderer.color = config.debugColor;
            }
        }
    }
}