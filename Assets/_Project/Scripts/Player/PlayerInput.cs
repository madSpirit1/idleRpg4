using UnityEngine;
using UnityEngine.InputSystem; // Добавляем пространство имен новой системы

public class PlayerInput : MonoBehaviour
{
    void Update()
    {
        // В новой системе мы проверяем нажатия напрямую через клавиатуру
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector3 direction = Vector3.zero;

        // Проверяем клавиши
        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            direction = new Vector3(0, 0, 1);
        else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            direction = new Vector3(0, 0, -1);
        else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            direction = new Vector3(-1, 0, 0);
        else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            direction = new Vector3(1, 0, 0);

        // Если направление выбрано, отправляем событие
        if (direction != Vector3.zero)
        {
            EventBus.Publish(new MoveRequestEvent { Direction = direction });
        }
    }
}