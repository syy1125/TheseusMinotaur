using System;
using UnityEngine;

public class GameState : System.IEquatable<GameState>
{
    public readonly Vector2Int TheseusPosition;
    public readonly Vector2Int MinotaurPosition;

    public GameState(Vector2Int theseusPosition, Vector2Int minotaurPosition)
    {
        TheseusPosition = theseusPosition;
        MinotaurPosition = minotaurPosition;
    }

    public bool Equals(GameState otherState)
    {
        if (ReferenceEquals(this, otherState)) return true;
        if (otherState == null) return false;
        return this.TheseusPosition == otherState.TheseusPosition && this.MinotaurPosition == otherState.MinotaurPosition;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TheseusPosition, MinotaurPosition);
    }

    public override string ToString()
    {
        return $"T={TheseusPosition},M={MinotaurPosition}";
    }
}
