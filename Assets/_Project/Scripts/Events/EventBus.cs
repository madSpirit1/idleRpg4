using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, Action<object>> Events = new();

    public static void Subscribe<T>(Action<T> action)
    {
        Type type = typeof(T);
        if (!Events.ContainsKey(type)) Events[type] = null;
        Events[type] += (obj) => action((T)obj);
    }

    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);
        if (Events.ContainsKey(type)) Events[type]?.Invoke(eventData);
    }
}

// Запрос на поиск пути к конкретной координате сетки
public struct PathRequestEvent { public Vector2Int TargetGridPos; }

