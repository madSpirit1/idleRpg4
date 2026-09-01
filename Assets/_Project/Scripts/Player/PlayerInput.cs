using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // --- СОХРАНЕНИЕ НА F5 ---
        if (keyboard.f5Key.wasPressedThisFrame)
        {
            GridGenerator.Instance.SaveGrid();
        }

        // --- ЗАГРУЗКА НА F6 ---
        if (keyboard.f6Key.wasPressedThisFrame)
        {
            GridGenerator.Instance.LoadGrid();
        }
        // --- НОВЫЙ КОД: ПРОПУСК ХОДА НА ПРОБЕЛ ---
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Нажат Пробел. Запрос на ручное завершение хода...");
            EventBus.Publish(new PlayerEndTurnRequestEvent());
        }

        // Проверяем нажатие левой кнопки мыши (в новой Input System)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Округляем мировые координаты точки удара, чтобы получить индекс клетки
                int x = Mathf.RoundToInt(hit.point.x);
                int z = Mathf.RoundToInt(hit.point.z);

                Vector2Int clickedCell = new Vector2Int(x, z);

                // Отправляем запрос на построение пути к этой клетке
                EventBus.Publish(new PathRequestEvent { TargetGridPos = clickedCell });
            }
        }
    }
}