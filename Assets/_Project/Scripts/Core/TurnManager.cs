using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum TurnPhase { Player, Enemies }
    
    [Header("Current Phase")]
    public TurnPhase currentPhase = TurnPhase.Player;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Начинаем игру с хода игрока
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        currentPhase = TurnPhase.Player;
        Debug.Log("--- НАЧАЛО ХОДА ИГРОКА ---");
        
        // Оповещаем все системы (включая UI и GridController), что игрок может ходить
        EventBus.Publish(new PlayerTurnStartedEvent());
    }

    public void EndPlayerTurn()
    {
        currentPhase = TurnPhase.Enemies;
        Debug.Log("--- ХОД ИГРОКА ЗАВЕРШЕН. ХОДЯТ ВРАГИ ---");

        // В будущем здесь будет запуск логики ИИ врагов.
        // А пока для теста просто возвращаем ход игроку через 1 секунду:
        Invoke(nameof(StartPlayerTurn), 1.0f);
    }
}