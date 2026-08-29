using UnityEngine;
using System.Collections;

public class GridController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 8f;        // Скорость скольжения кубика
    public float cellSize = 1f;         // Размер одной клетки сетки

    private bool _isMoving = false;     // Блокировка ввода во время движения
    private Vector2Int _currentGridPos = Vector2Int.zero; // Текущая координата игрока на сетке

    private void OnEnable()
    {
        // Подписываемся на событие запроса хода из PlayerInput
        EventBus.Subscribe<MoveRequestEvent>(OnMoveRequested);
    }

    private void Start()
    {
        // При старте выравниваем положение кубика под размер клеток
        transform.position = new Vector3(_currentGridPos.x * cellSize, 0.5f, _currentGridPos.y * cellSize);
    }

    private void OnMoveRequested(MoveRequestEvent data)
    {
        // Если кубик уже скользит, новый ввод игнорируем
        if (_isMoving) return;

        // Переводим направление из 3D (Vector3) в плоскость сетки (Vector2Int)
        Vector2Int direction2D = new Vector2Int((int)data.Direction.x, (int)data.Direction.z);
        
        // Считаем координату клетки, в которую игрок хочет наступить
        Vector2Int targetGridPos = _currentGridPos + direction2D;

        // Переводим координаты сетки в реальные 3D координаты сцены
        Vector3 targetWorldPos = new Vector3(targetGridPos.x * cellSize, 0.5f, targetGridPos.y * cellSize);

        // Запускаем плавное перемещение
        StartCoroutine(MoveRoutine(targetWorldPos, targetGridPos));
    }

    private IEnumerator MoveRoutine(Vector3 targetWorldPos, Vector2Int targetGridPos)
    {
        _isMoving = true;
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float duration = 1f / moveSpeed;

        // Плавно двигаем кубик кадр за кадром
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Фиксируем персонажа ровно в центре целевой клетки
        transform.position = targetWorldPos;
        
        // Перезаписываем текущую координату сетки на новую
        _currentGridPos = targetGridPos;
        _isMoving = false;

        // Публикуем АБСОЛЮТНО ПУСТОЕ событие, строго как в твоем репозитории
        EventBus.Publish(new TurnFinishedEvent());
    }
}