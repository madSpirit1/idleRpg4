using System;
using System.Collections.Generic;

public static class EventBus
{
    // Словарь, хранящий списки действий для каждого типа события
    private static readonly Dictionary<Type, Action<object>> Events = new();

    // Подписка
    public static void Subscribe<T>(Action<T> action)
    {
        Type type = typeof(T);
        if (!Events.ContainsKey(type)) Events[type] = null;
        Events[type] += (obj) => action((T)obj);
    }

    // Рассылка события
    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);
        if (Events.ContainsKey(type))
        {
            Events[type]?.Invoke(eventData);
            ;
        }
    }
}