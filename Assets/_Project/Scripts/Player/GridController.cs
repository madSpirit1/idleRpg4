using UnityEngine;
using System.Collections;

public class GridController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;     // Скорость движения между клетками
    public float cellSize = 1f;      // Размер клетки (совпадает с полом)

    private bool _isMoving = false;  // Флаг, чтобы не ходить во время движения

    private void OnEnable()
    {
        // Подписываемся на событие запроса хода из PlayerInput
        EventBus.Subscribe<MoveRequestEvent>(OnMoveRequested);
    }

    private void OnMoveRequested(MoveRequestEvent data)
    {
        // Если мы уже в процессе движения, игнорируем нажатия
        if (_isMoving) return;

        // Рассчитываем целевую позицию
        Vector3 targetPos = transform.position + (data.Direction * cellSize);

        // Запускаем плавное перемещение (Корутину)
        StartCoroutine(MoveRoutine(targetPos));
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        _isMoving = true;
        Vector3 startPosition = transform.position;
        float elapsed = 0f;
        float duration = 1f / moveSpeed;

        while (elapsed < duration)
        {
            // Плавно двигаем объект от старта к цели
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null; // Ждем следующего кадра
        }

        // В конце фиксируем точную позицию
        transform.position = targetPosition;
        _isMoving = false;

        // Сообщаем всем системам, что ход успешно завершен
        EventBus.Publish(new TurnFinishedEvent { FinalPosition = targetPosition });
    }
}