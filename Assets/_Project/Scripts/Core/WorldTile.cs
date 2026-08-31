using UnityEngine;

public class WorldTile : MonoBehaviour
{
    // Публичное свойство (Property) для чтения данных тайла
    public TileSaveData Data { get; private set; }
    
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        // Ищем компонент отображения 2D спрайтов
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Vector2Int position, TileDataConfig config)
    {
        // Заполняем базовую структуру данных
        Data = new TileSaveData();
        Data.gridPosition = position;
        Data.configName = config.tileName;
        Data.isWalkable = config.isWalkable;

        // Если компонент для картинок найден на объекте
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = config.tileSprite;
            
            // Запасной вариант: если вы забыли прикрепить картинку в конфиг,
            // мы принудительно красим плитку в ее дебаг-цвет, чтобы она не была прозрачной
            if (config.tileSprite == null)
            {
                _spriteRenderer.color = config.debugColor;
            }
        }
        else
        {
            // ЕСЛИ ВЫ ИСПОЛЬЗУЕТЕ СТАНДАРТНЫЙ 3D КУБ В КАЧЕСТВЕ ПРЕФАБА:
            // Этот кусок кода сработает автоматически, найдет MeshRenderer куба и покрасит его
            MeshRenderer meshRender = GetComponent<MeshRenderer>();
            if (meshRender != null)
            {
                meshRender.material.color = config.debugColor;
            }
        }
    }
}