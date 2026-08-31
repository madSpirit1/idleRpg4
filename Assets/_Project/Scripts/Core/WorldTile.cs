using UnityEngine;

public class WorldTile : MonoBehaviour
{
    public TileSaveData Data { get; private set; }
    
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Vector2Int position, TileDataConfig config)
    {
        Data = new TileSaveData();
        Data.gridPosition = position;
        Data.configName = config.tileName;
        Data.isWalkable = config.isWalkable;

        if (_spriteRenderer != null)
        {
            // Берем картинку из конфига. Если там null (нет картинки), 
            // SpriteRenderer сохранит тот белый квадрат, который мы укажем в префабе!
            if (config.tileSprite != null)
            {
                _spriteRenderer.sprite = config.tileSprite;
            }

            // Задаем цвет и принудительно выставляем непрозрачность
            Color finalColor = config.debugColor;
            finalColor.a = 1f;
            _spriteRenderer.color = finalColor;
        }
    }
}