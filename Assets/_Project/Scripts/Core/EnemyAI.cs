using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("JSON Settings")]
    public string enemyTypeID = "bandit"; // Перезапишется спавнером при создании кодом
    public float moveSpeed = 4f;

    private EnemyStats _stats;
    private Vector2Int _currentGridPos;
    private bool _isMoving = false;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        // Ищем отрисовщик спрайтов на фишке или в ее дочерних объектах
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyTurnStartedEvent>(OnEnemyTurnStarted);
    }

    private void Start()
    {
        _stats = EnemyDatabase.GetStats(enemyTypeID);
        if (_stats != null)
        {
            Debug.Log(LocalizationManager.Get("log_spawn") + " " + _stats.EnemyName + " (" + _stats.Race + ")! HP: " + _stats.maxHp);
        }

        // 1. Считаем позицию на сетке
        _currentGridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
    
        // 2. Выравниваем физическую позицию
        transform.position = new Vector3((float)_currentGridPos.x, 0.1f, (float)_currentGridPos.y);
    
        // ЖЕЛЕЗОБЕТОННЫЙ ПОВОРОТ: принудительно кладем фишку плашмя на пол кодом при старте!
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
    // ВЫЗЫВАЕТСЯ ПРИ СТАРТЕ ИЛИ СПАВНЕ КОДОМ
    public void InitializeEnemy(string typeID)
    {
        enemyTypeID = typeID;
        _stats = EnemyDatabase.GetStats(enemyTypeID);
    
        if (_stats != null)
        {
            // Настраиваем здоровье врага из JSON параметров бандита
            Health hpComponent = GetComponent<Health>();
            if (hpComponent != null)
            {
                hpComponent.InitializeHp(_stats.maxHp);
            }

            if (_spriteRenderer != null && !string.IsNullOrEmpty(_stats.textureName))
            {
                Sprite loadedSprite = Resources.Load<Sprite>(_stats.textureName);
                if (loadedSprite != null) _spriteRenderer.sprite = loadedSprite;
            }

            Debug.Log(LocalizationManager.Get("log_spawn") + " " + _stats.EnemyName + " (" + _stats.Race + ")! HP: " + _stats.maxHp);
        }
    }
    
    
    private void OnEnemyTurnStarted(EnemyTurnStartedEvent data)
    {
        // Находим игрока на сцене через класс GridController
        GridController playerInstance = FindFirstObjectByType<GridController>();
    
        if (playerInstance != null)
        {
            // Переводим мировые координаты игрока в координаты сетки
            Vector2Int playerGridPos = new Vector2Int(
                Mathf.RoundToInt(playerInstance.transform.position.x), 
                Mathf.RoundToInt(playerInstance.transform.position.z)
            );
        
            // Передаем координаты игрока в корутину движения и атаки врага
            StartCoroutine(EnemyTurnRoutine(playerGridPos));
        }
        else
        {
            // Если игрок на сцене не найден, просто завершаем ход этого врага
            EventBus.Publish(new EnemyTurnFinishedEvent { EnemyObject = gameObject });
        }
    }


    private IEnumerator EnemyTurnRoutine(Vector2Int playerGridPos)
    {
        int apLeft = _stats.maxActionPoints;

        while (apLeft > 0)
        {
            Vector2Int nextStep = CalculateNextStepTowards(playerGridPos);

            if (nextStep == _currentGridPos || nextStep == playerGridPos)
            {
                if (nextStep == playerGridPos && _stats != null)
                {
                    Debug.Log(_stats.EnemyName + " " + LocalizationManager.Get("log_attack") + " " + _stats.damage + " " + LocalizationManager.Get("log_damage"));
    
                    // Снова находим ссылку на игрока для отправки события урона
                    GridController playerInstance = FindFirstObjectByType<GridController>();
                    if (playerInstance != null)
                    {
                        EventBus.Publish(new DamageEvent { Target = playerInstance.gameObject, Amount = _stats.damage });
                    }
                }
            }


            _isMoving = true;
            Vector3 startPos = transform.position;
            Vector3 targetWorldPos = new Vector3((float)nextStep.x, 0.1f, (float)nextStep.y);
            float elapsed = 0f;
            float duration = 1f / moveSpeed;

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetWorldPos;
            // Страховка: удерживаем фишку горизонтально после каждого шага
            transform.rotation = Quaternion.Euler(90f, 0f, 0f); 
            _currentGridPos = nextStep;
            _currentGridPos = nextStep;
            _isMoving = false;

            apLeft--;
            yield return new WaitForSeconds(0.2f);
        }

        EventBus.Publish(new EnemyTurnFinishedEvent { EnemyObject = gameObject });
    }

    private Vector2Int CalculateNextStepTowards(Vector2Int playerPos)
    {
        int stepX = 0;
        if (playerPos.x > _currentGridPos.x) stepX = 1;
        else if (playerPos.x < _currentGridPos.x) stepX = -1;

        int stepY = 0;
        if (playerPos.y > _currentGridPos.y) stepY = 1;
        else if (playerPos.y < _currentGridPos.y) stepY = -1;

        Vector2Int targetStep = _currentGridPos + new Vector2Int(stepX, stepY);

        if (GridGenerator.Instance.IsCellWalkable(targetStep)) return targetStep;

        Vector2Int altX = _currentGridPos + new Vector2Int(stepX, 0);
        if (GridGenerator.Instance.IsCellWalkable(altX)) return altX;

        Vector2Int altY = _currentGridPos + new Vector2Int(0, stepY);
        if (GridGenerator.Instance.IsCellWalkable(altY)) return altY;

        return _currentGridPos;
    }
}
