using UnityEngine;

public class PathDot : MonoBehaviour
{
    public Vector2Int GridPosition { get; set; }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerStepTakenEvent>(OnStepTaken);
    }

    // ВАЖНО: Когда точка удаляется, она аккуратно стирает себя из памяти EventBus!
    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerStepTakenEvent>(OnStepTaken);
    }

    private void OnStepTaken(PlayerStepTakenEvent data)
    {
        // Проверяем: если эта точка создана, жива и координаты совпали — стираем её
        if (this != null && data.Position == GridPosition)
        {
            Destroy(gameObject);
        }
    }
}