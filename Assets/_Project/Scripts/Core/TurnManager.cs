using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum TurnPhase { Player, Enemies }
    
    [Header("Current Phase")]
    public TurnPhase currentPhase = TurnPhase.Player;

    // Список врагов, которые еще ДОЛЖНЫ походить в текущую фазу
    private List<GameObject> _activeEnemies = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Слушаем отчеты врагов о завершении их хода
        EventBus.Subscribe<EnemyTurnFinishedEvent>(OnEnemyFinishedTurn);
    }

    private void Start()
    {
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        currentPhase = TurnPhase.Player;
        Debug.Log("--- НАЧАЛО ХОДА ИГРОКА ---");
        EventBus.Publish(new PlayerTurnStartedEvent());
    }

    public void EndPlayerTurn()
    {
        currentPhase = TurnPhase.Enemies;
        Debug.Log("--- ХОД ИГРОКА ЗАВЕРШЕН. ХОДЯТ ВРАГИ ---");

        // Находим абсолютно всех врагов со скриптом EnemyAI на сцене
        EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        _activeEnemies.Clear();

        // Заполняем список ожидания
        for (int i = 0; !(i >= allEnemies.Length); i++)
        {
            _activeEnemies.Add(allEnemies[i].gameObject);
        }

        // Если врагов на карте физически нет — сразу возвращаем ход игроку
        if (!(_activeEnemies.Count > 0))
        {
            Debug.Log("Врагов на карте нет. Ход сразу возвращается игроку.");
            StartPlayerTurn();
            return;
        }

        // Публикуем событие начала хода врагов в шину
        EventBus.Publish(new EnemyTurnStartedEvent());
    }

    private void OnEnemyFinishedTurn(EnemyTurnFinishedEvent data)
    {
        // Убираем отходившего врага из списка ожидания
        if (_activeEnemies.Contains(data.EnemyObject))
        {
            _activeEnemies.Remove(data.EnemyObject);
        }

        // Если список пуст — значит ВСЕ враги сделали свои шаги
        if (!(_activeEnemies.Count > 0))
        {
            Debug.Log("Все враги отходили. Передаем ход игроку.");
            StartPlayerTurn();
        }
    }
}
