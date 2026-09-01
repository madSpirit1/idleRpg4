using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    // Используем Delegate вместо Action<object>, чтобы Git-код поддерживал точную отписку
    private static readonly Dictionary<Type, Delegate> Events = new Dictionary<Type, Delegate>();

    // Подписка
    public static void Subscribe<T>(Action<T> action)
    {
        Type type = typeof(T);
        if (!Events.ContainsKey(type))
        {
            Events[type] = null;
        }
        Events[type] = (Action<T>)Events[type] + action;
    }

    // ОТПИСКА (То, чего нам не хватало, чтобы чистить память!)
    public static void Unsubscribe<T>(Action<T> action)
    {
        Type type = typeof(T);
        if (Events.ContainsKey(type))
        {
            Events[type] = (Action<T>)Events[type] - action;
            if (Events[type] == null)
            {
                Events.Remove(type);
            }
        }
    }

    // Рассылка
    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);
        if (Events.ContainsKey(type))
        {
            Action<T> action = Events[type] as Action<T>;
            if (action != null)
            {
                action.Invoke(eventData);
            }
        }
    }
}

// Структуры твоих событий остаются строго прежними:
public struct MoveRequestEvent { public UnityEngine.Vector3 Direction; }
public struct PathRequestEvent { public UnityEngine.Vector2Int TargetGridPos; }
public struct TurnFinishedEvent { }
public struct PlayerTurnStartedEvent { }
public struct PlayerStepTakenEvent { public UnityEngine.Vector2Int Position; }
// Сигнал от TurnManager, запускающий ход врагов
public struct EnemyTurnStartedEvent { }

// Сигнал от конкретного врага, что он закончил свои шаги
public struct EnemyTurnFinishedEvent { public GameObject EnemyObject; }

// Сигнал о том, что игрок вручную нажал Пробел для завершения своего хода
public struct PlayerEndTurnRequestEvent { }