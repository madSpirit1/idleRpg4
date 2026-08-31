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
        Vector2Int current = start;
    
        // Безопасный счетчик, чтобы игра не зависла
        int maxSteps = 50; 
        int steps = 0;

        // Сразу проверяем: если финальная клетка вообще непроходима, то даже не начинаем путь
        if (!GridGenerator.Instance.IsCellWalkable(end))
        {
            return path; // Возвращаем пустой путь
        }

        // Пошагово строим линию до цели
        while (current != end && !(steps >= maxSteps))
        {
            steps++;
        
            // Определяем направление шага по каждой оси (-1, 0 или 1)
            int stepX = 0;
            if (end.x > current.x) stepX = 1;
            else if (end.x < current.x) stepX = -1;

            int stepY = 0;
            if (end.y > current.y) stepY = 1;
            else if (end.y < current.y) stepY = -1;

            Vector2Int nextStep = current + new Vector2Int(stepX, stepY);

            // Проверяем проходимость следующего шага
            if (GridGenerator.Instance.IsCellWalkable(nextStep))
            {
                current = nextStep;
                path.Add(current);
            }
            else
            {
                // Если наткнулись на препятствие посреди пути — останавливаемся
                break;
            }
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