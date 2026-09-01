using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float cellSize = 1f;
    public GameObject pathDotPrefab; 

    [Header("Turn Settings")]
    public int maxActionPoints = 4; // Сколько клеток можно пройти за 1 ход
    private int _currentActionPoints;
    private bool _isMyTurn = false;

    private bool _isMoving = false;
    private Vector2Int _currentGridPos = Vector2Int.zero;
    private List<GameObject> _activeDots = new List<GameObject>();
    
    private List<Vector2Int> _remainingPath = new List<Vector2Int>(); // Сюда запоминаем остаток пути


    private void OnEnable()
    {
        EventBus.Subscribe<PathRequestEvent>(OnPathRequested);
        EventBus.Subscribe<PlayerTurnStartedEvent>(OnTurnStarted);
    }

    private void Start()
    {
        transform.position = new Vector3(_currentGridPos.x * cellSize, 0.1f, _currentGridPos.y * cellSize);
    }

    private void OnTurnStarted(PlayerTurnStartedEvent data)
    {
        _currentActionPoints = maxActionPoints;
        _isMyTurn = true;
        Debug.Log("Очки действий восстановлены: " + _currentActionPoints);

        if (_remainingPath != null && _remainingPath.Count > 0)
        {
            List<Vector2Int> nextSegment = new List<Vector2Int>();
        
            for (int i = 0; !(i >= _remainingPath.Count) && !(i >= _currentActionPoints); i++)
            {
                nextSegment.Add(_remainingPath[i]);
            }

            _remainingPath.RemoveRange(0, nextSegment.Count);

            // ВАЖНО: Больше НЕ вызываем ClearPathDots() и SpawnPathDots() здесь,
            // чтобы не стирать точки, которые уже стоят на всей линии пути впереди.
            StartCoroutine(MoveAlongPathRoutine(nextSegment));
        }
    }

    private void OnPathRequested(PathRequestEvent data)
    {
        if (!_isMyTurn || _isMoving) return;

        ClearPathDots();
        _remainingPath.Clear(); // Очищаем остатки с прошлых кликов

        // Вычисляем полную траекторию от начала до самого конца клика
        List<Vector2Int> fullPath = CalculatePath(_currentGridPos, data.TargetGridPos);

        if (fullPath.Count > 0)
        {
            List<Vector2Int> currentSegment = new List<Vector2Int>();

            // Распределяем полный путь на текущий ход и будущие остатки
            for (int i = 0; !(i >= fullPath.Count); i++)
            {
                if (i >= _currentActionPoints)
                {
                    _remainingPath.Add(fullPath[i]); // Все, что выходит за рамки AP — на будущее
                }
                else
                {
                    currentSegment.Add(fullPath[i]); // То, что успеем пройти за этот ход
                }
            }

            // ВАЖНО: Рисуем точки СРАЗУ на весь длинный путь, а не на огрызок
            SpawnPathDots(fullPath);

            if (currentSegment.Count > 0)
            {
                StartCoroutine(MoveAlongPathRoutine(currentSegment));
            }
        }
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
        
            // НОВЫЙ КОД: Добавляем компонент PathDot на созданную точку и даем ей координату
            PathDot dotComponent = dot.AddComponent<PathDot>();
            if (dotComponent != null)
            {
                dotComponent.GridPosition = path[i];
            }

            _activeDots.Add(dot);
        }
    }

    private void ClearPathDots()
    {
        // Этот метод теперь нужен только для полной очистки поля при новом клике
        for (int i = 0; !(i >= _activeDots.Count); i++)
        {
            if (_activeDots[i] != null) 
            {
                Destroy(_activeDots[i]);
            }
        }
        _activeDots.Clear();
    }
    private IEnumerator MoveAlongPathRoutine(List<Vector2Int> path)
    {
        _isMoving = true;

        // СНАЧАЛА: Персонаж полностью проходит весь доступный на этот ход отрезков пути
        for (int i = 0; !(i >= path.Count); i++)
        {
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

            // Тратим 1 AP за шаг
            _currentActionPoints--;
            Debug.Log("Сделан шаг. Осталось Очков Действия (AP): " + _currentActionPoints);

            // Публикуем событие шага — точка на этой клетке стирается
            EventBus.Publish(new PlayerStepTakenEvent { Position = _currentGridPos });
        } // <--- КОНЕЦ ЦИКЛА ДВИЖЕНИЯ! Игрок закончил текущую пробежку.

        _isMoving = false;

        // ТОЛЬКО ЗДЕСЬ (вне цикла): Проверяем, закончился ли ход глобально
        // ПРОВЕРКА ОКОНЧАНИЯ ХОДА (вне цикла движения)
        if (!(_currentActionPoints > 0))
        {
            _isMyTurn = false;
            ClearPathDots();

            // Передаем ход менеджеру. Он сам проверит наличие врагов на сцене
            TurnManager.Instance.EndPlayerTurn();
        }
    
        EventBus.Publish(new TurnFinishedEvent());
    }
}
