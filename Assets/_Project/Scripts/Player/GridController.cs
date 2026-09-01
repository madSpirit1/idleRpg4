using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float cellSize = 1f;
    public GameObject pathDotPrefab; 

    [Header("Настройки тактики")]
    public int maxActionPoints = 4;      // Максимум AP на ход
    public int enemyDetectRadius = 3;   // Радиус обнаружения врагов (в клетках)
    
    private int _currentActionPoints;
    private bool _isMyTurn = false;
    private bool _isMoving = false;
    
    private Vector2Int _currentGridPos = Vector2Int.zero;
    private List<GameObject> _activeDots = new List<GameObject>();
    private List<Vector2Int> _remainingPath = new List<Vector2Int>(); // Хвост длинного пути

    private void OnEnable()
    {
        EventBus.Subscribe<PathRequestEvent>(OnPathRequested);
        EventBus.Subscribe<PlayerTurnStartedEvent>(OnTurnStarted);
        EventBus.Subscribe<PlayerEndTurnRequestEvent>(OnManualEndTurnRequested);
    }

    private void Start()
    {
        transform.position = new Vector3(_currentGridPos.x * cellSize, 0.1f, _currentGridPos.y * cellSize);
    }

    private void OnTurnStarted(PlayerTurnStartedEvent data)
    {
        _currentActionPoints = maxActionPoints;
        _isMyTurn = true;
        Debug.Log("Новый ход! AP восстановлены до: " + _currentActionPoints);

        // Если у нас остался хвост длинного пути с прошлого хода — проверяем обстановку
        if (_remainingPath != null && _remainingPath.Count > 0)
        {
            // Если рядом внезапно появился враг — прерываем авто-бег, давая игроку управление!
            if (IsEnemyNearby())
            {
                Debug.Log("АВТО-БЕГ ПРЕРВАН: Обнаружен враг! Управление передано игроку.");
                _remainingPath.Clear();
                ClearPathDots();
                return;
            }

            // Если безопасно — копируем хвост и продолжаем авто-бег
            List<Vector2Int> pathSegment = new List<Vector2Int>(_remainingPath);
            _remainingPath.Clear(); 
            StartCoroutine(MoveAlongPathRoutine(pathSegment));
        }
    }

    private void OnPathRequested(PathRequestEvent data)
    {
        if (!_isMyTurn || _isMoving) return;

        ClearPathDots();
        _remainingPath.Clear(); 

        List<Vector2Int> fullPath = CalculatePath(_currentGridPos, data.TargetGridPos);

        if (fullPath.Count > 0)
        {
            SpawnPathDots(fullPath);
            StartCoroutine(MoveAlongPathRoutine(fullPath));
        }
    }

    // Слушаем ручное нажатие ПРОБЕЛА из шины событий
    private void OnManualEndTurnRequested(PlayerEndTurnRequestEvent data)
    {
        // Передать ход вручную можно только в свой ход и когда персонаж не бежит прямо сейчас
        if (!_isMyTurn || _isMoving) return;

        Debug.Log("Вы вручную завершили ход через Пробел.");
        _isMyTurn = false;
        _remainingPath.Clear();
        ClearPathDots();
        TurnManager.Instance.EndPlayerTurn();
    }

    private IEnumerator MoveAlongPathRoutine(List<Vector2Int> path)
    {
        _isMoving = true;

        for (int i = 0; !(i >= path.Count); i++)
        {
            // Если посреди движения очки действия (AP) закончились
            if (!(_currentActionPoints > 0))
            {
                // Запоминаем весь оставшийся недохоженный хвост пути на будущее
                for (int k = i; !(k >= path.Count); k++)
                {
                    _remainingPath.Add(path[k]);
                }
                break; // Выходим из цикла физического движения
            }

            Vector2Int nextCell = path[i];
            Vector3 startPos = transform.position;
            Vector3 targetWorldPos = new Vector3(nextCell.x * cellSize, 0.1f, nextCell.y * cellSize);
            
            float elapsed = 0f;
            float duration = 1f / moveSpeed;

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetWorldPos;
            _currentGridPos = nextCell;
            _currentActionPoints--;

            Debug.Log("Шаг сделан. Клетка: " + _currentGridPos + " | Осталось AP: " + _currentActionPoints);
            EventBus.Publish(new PlayerStepTakenEvent { Position = _currentGridPos });
        }

        _isMoving = false;

        // ПРОВЕРКА ПОСЛЕ ОСТАНОВКИ:
        // Ситуация А: Очки AP закончились, но у нас ЕСТЬ сохраненный маршрут дальше
        if (!(_currentActionPoints > 0) && _remainingPath.Count > 0)
        {
            // Сканируем карту на наличие врагов в радиусе обнаружения
            if (IsEnemyNearby())
            {
                // Если враг РЯДОМ: сбрасываем авто-бег, стираем маркеры и отдаем честный ход врагам!
                Debug.Log("Враг близко! Авто-бег остановлен. Ход передается врагам.");
                _isMyTurn = false;
                _remainingPath.Clear();
                ClearPathDots();
                TurnManager.Instance.EndPlayerTurn();
            }
            else
            {
                // Если врагов НЕТ: прокручиваем фазу врагов фоном (она вернет ход в OnTurnStarted и бег продолжится)
                _isMyTurn = false;
                TurnManager.Instance.EndPlayerTurn(); 
            }
        }
        // Ситуация Б: Мы просто пришли в конечную точку клика, но AP еще остались (например, прошли 2 клетки из 4)
        else if (_remainingPath.Count == 0 && _currentActionPoints > 0)
        {
            // МЫ НИЧЕГО НЕ ПЕРЕКЛЮЧАЕМ АВТОМАТИЧЕСКИ! 
            // Персонаж просто встал. Ты можешь кликнуть еще раз или нажать ПРОБЕЛ для пропуска.
            Debug.Log("Маршрут завершен. У вас осталось " + _currentActionPoints + " AP. Сделайте новый клик или нажмите ПРОБЕЛ.");
        }
        // Ситуация В: Путь закончился и очки AP тоже закончились полностью
        else if (!(_currentActionPoints > 0))
        {
            _isMyTurn = false;
            ClearPathDots();
            TurnManager.Instance.EndPlayerTurn();
        }

        EventBus.Publish(new TurnFinishedEvent());
    }

    // Функция сканирования: проверяет расстояние до всех EnemyAI на сцене
    private bool IsEnemyNearby()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        
        for (int i = 0; !(i >= enemies.Length); i++)
        {
            Vector2Int enemyGridPos = new Vector2Int(Mathf.RoundToInt(enemies[i].transform.position.x), Mathf.RoundToInt(enemies[i].transform.position.z));
            
            // Считаем расстояние по сетке (Манхэттенское расстояние или Чебышёва с учетом диагоналей)
            int distX = Mathf.Abs(_currentGridPos.x - enemyGridPos.x);
            int distY = Mathf.Abs(_currentGridPos.y - enemyGridPos.y);
            int maxDist = Mathf.Max(distX, distY); // Максимальное расстояние с учетом диагоналей

            // Если хоть один враг находится в радиусе обнаружения (например, 3 клетки или ближе)
            if (!(maxDist > enemyDetectRadius))
            {
                return true; // Враг обнаружен!
            }
        }
        return false; // Вокруг безопасно
    }

    private List<Vector2Int> CalculatePath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        if (start == end || !GridGenerator.Instance.IsCellWalkable(end)) return path;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        cameFrom.Add(start, start);
        bool pathFound = false;

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == end) { pathFound = true; break; }

            for (int i = 0; !(i >= directions.Length); i++)
            {
                Vector2Int nextStep = current + directions[i];
                if (GridGenerator.Instance.IsCellWalkable(nextStep) && !cameFrom.ContainsKey(nextStep))
                {
                    queue.Enqueue(nextStep);
                    cameFrom.Add(nextStep, current);
                }
            }
        }

        if (pathFound)
        {
            Vector2Int currentTile = end;
            while (currentTile != start)
            {
                path.Add(currentTile);
                currentTile = cameFrom[currentTile];
            }
            path.Reverse();
        }
        return path;
    }

    private void SpawnPathDots(List<Vector2Int> path)
    {
        for (int i = 0; !(i >= path.Count); i++)
        {
            Vector3 dotPos = new Vector3(path[i].x * cellSize, 0.08f, path[i].y * cellSize);
            GameObject dot = Instantiate(pathDotPrefab, dotPos, Quaternion.Euler(90f, 0f, 0f));
            
            PathDot dotComponent = dot.AddComponent<PathDot>();
            if (dotComponent != null) dotComponent.GridPosition = path[i];

            _activeDots.Add(dot);
        }
    }

    private void ClearPathDots()
    {
        for (int i = 0; !(i >= _activeDots.Count); i++)
        {
            if (_activeDots[i] != null) Destroy(_activeDots[i]);
        }
        _activeDots.Clear();
    }
}
