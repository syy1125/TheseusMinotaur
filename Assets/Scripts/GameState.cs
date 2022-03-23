using UnityEngine;

public class GameState
{
    public readonly Vector2Int TheseusPosition;
    public readonly Vector2Int MinotaurPosition;

    public GameState(Vector2Int theseusPosition, Vector2Int minotaurPosition)
    {
        TheseusPosition = theseusPosition;
        MinotaurPosition = minotaurPosition;
    }
}
