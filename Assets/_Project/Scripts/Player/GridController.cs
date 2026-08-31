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
    
        // Ограничение безопасности, чтобы цикл не завис, если пути нет
        int maxSteps = 100; 
        int steps = 0;

        while (current != end && steps < maxSteps)
        {
            steps++;
            int stepX = Mathf.Clamp(end.x - current.x, -1, 1);
            int stepY = Mathf.Clamp(end.y - current.y, -1, 1);

            Vector2Int nextStep = current + new Vector2Int(stepX, stepY);

            // ВАЖНО: Спрашиваем у генератора, проходима ли СЛЕДУЮЩАЯ клетка
            if (GridGenerator.Instance.IsCellWalkable(nextStep))
            {
                current = nextStep;
                path.Add(current);
            }
            else
            {
                // Если на пути стена по диагонали или прямой, пытаемся обойти (простейший обход)
                // Пытаемся сделать шаг только по X
                Vector2Int altStepX = current + new Vector2Int(stepX, 0);
                // Пытаемся сделать шаг только по Y
                Vector2Int altStepY = current + new Vector2Int(0, stepY);

                if (GridGenerator.Instance.IsCellWalkable(altStepX))
                {
                    current = altStepX;
                    path.Add(current);
                }
                else if (GridGenerator.Instance.IsCellWalkable(altStepY))
                {
                    current = altStepY;
                    path.Add(current);
                }
                else
                {
                    // Если зажали в тупик — прекращаем строить путь
                    break;
                }
            }
        }

        return path;
    }


    private void SpawnPathDots(List<Vector2Int> path)
    {
        // Спавним точки во всех клетках пути, кроме последней (где встанет игрок)
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 dotPos = new Vector3(path[i].x * cellSize, 0.08f, path[i].y * cellSize);
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

        foreach (Vector2Int nextCell in path)
        {
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

            // Удаляем первую точку из визуального пути, так как мы на неё уже наступили
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