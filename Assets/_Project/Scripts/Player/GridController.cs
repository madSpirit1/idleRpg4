using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;
    public float cellSize = 1f;
    public GameObject pathDotPrefab; // Сюда перетащим префаб круглой точки

    private bool _isMoving = false;
    private Vector2Int _currentGridPos = Vector2Int.zero;
    private List<GameObject> _activeDots = new List<GameObject>();

    private void OnEnable()
    {
        EventBus.Subscribe<PathRequestEvent>(OnPathRequested);
    }

    private void Start()
    {
        // Начальное выравнивание фишки игрока
        transform.position = new Vector3(_currentGridPos.x * cellSize, 0.1f, _currentGridPos.y * cellSize);
    }

    private void OnPathRequested(PathRequestEvent data)
    {
        if (_isMoving) return;

        ClearPathDots();

        // Строим список координат от текущей позиции до цели
        List<Vector2Int> path = CalculatePath(_currentGridPos, data.TargetGridPos);

        if (path.Count > 0)
        {
            // Визуализируем точки на пути
            SpawnPathDots(path);

            // Запускаем последовательное перемещение по клеткам
            StartCoroutine(MoveAlongPathRoutine(path));
        }
    }

   private List<Vector2Int> CalculatePath(Vector2Int start, Vector2Int end)
{
    List<Vector2Int> path = new List<Vector2Int>();

    // Если конечная клетка непроходима или мы кликнули на себя — сразу выходим
    if (start == end || !GridGenerator.Instance.IsCellWalkable(end))
    {
        return path;
    }

    // Очередь для сканирования клеток (волна)
    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    // Словарь, чтобы запомнить, из какой клетки мы пришли в текущую (для восстановления пути)
    Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

    queue.Enqueue(start);
    cameFrom.Add(start, start);

    bool pathFound = false;

    // Списки направлений: 4 по прямой + 4 по диагонали
    Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Вверх
        new Vector2Int(0, -1),  // Вниз
        new Vector2Int(1, 0),   // Вправо
        new Vector2Int(-1, 0),  // Влево
        new Vector2Int(1, 1),   // Вверх-Вправо
        new Vector2Int(-1, 1),  // Вверх-Влево
        new Vector2Int(1, -1),  // Вниз-Вправо
        new Vector2Int(-1, -1)  // Вниз-Влево
    };

    // Запускаем волну по сетке
    while (queue.Count > 0)
    {
        Vector2Int current = queue.Dequeue();

        if (current == end)
        {
            pathFound = true;
            break;
        }

        // Проверяем всех соседей текущей клетки
        for (int i = 0; !(i >= directions.Length); i++)
        {
            Vector2Int nextStep = current + directions[i];

            // Если сосед в границах карты и он проходим, и мы там еще не были
            if (GridGenerator.Instance.IsCellWalkable(nextStep) && !cameFrom.ContainsKey(nextStep))
            {
                queue.Enqueue(nextStep);
                cameFrom.Add(nextStep, current);
            }
        }
    }

    // Если волна дошла до цели, собираем путь обратно от конца к старту
    if (pathFound)
    {
        Vector2Int currentTile = end;
        while (currentTile != start)
        {
            path.Add(currentTile);
            currentTile = cameFrom[currentTile];
        }
        // Переворачиваем список, чтобы он шел от стартовой точки к финишу
        path.Reverse();
    }

    return path;
}


    private void SpawnPathDots(List<Vector2Int> path)
    {
        // Спавним точки абсолютно на всех шагах пути
        for (int i = 0; !(i >= path.Count); i++)
        {
            Vector3 dotPos = new Vector3(path[i].x * cellSize, 0.08f, path[i].y * cellSize);
            // Quaternion.Euler(90f, 0f, 0f) кладет точку плашмя на пол
            GameObject dot = Instantiate(pathDotPrefab, dotPos, Quaternion.Euler(90f, 0f, 0f));
            _activeDots.Add(dot);
        }
    }

    private void ClearPathDots()
    {
        foreach (var dot in _activeDots)
        {
            if (dot != null) Destroy(dot);
        }
        _activeDots.Clear();
    }

    private IEnumerator MoveAlongPathRoutine(List<Vector2Int> path)
    {
        _isMoving = true;

        for (int i = 0; !(i >= path.Count); i++)
        {
            Vector2Int nextCell = path[i];
            Vector3 startPos = transform.position;
            Vector3 targetWorldPos = new Vector3(nextCell.x * cellSize, 0.1f, nextCell.y * cellSize);
        
            float elapsed = 0f;
            float duration = 1f / moveSpeed;

            // Плавное перемещение к следующей клетке
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetWorldPos;
            _currentGridPos = nextCell;

            // Удаляем точку, на которую только что успешно наступили
            if (_activeDots.Count > 0)
            {
                Destroy(_activeDots[0]);
                _activeDots.RemoveAt(0);
            }
        }

        _isMoving = false;
        EventBus.Publish(new TurnFinishedEvent());
    }
}