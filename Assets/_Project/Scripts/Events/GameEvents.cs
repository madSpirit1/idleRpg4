using UnityEngine;

// Это просто контейнер данных, который летит по шине
public struct MoveRequestEvent 
{
    public Vector3 Direction; // Куда хочет пойти игрок
}

public struct TurnFinishedEvent 
{
    public Vector3 FinalPosition; // Где игрок оказался
}