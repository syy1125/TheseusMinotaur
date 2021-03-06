using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvalidGameMoveException : Exception
{
    public InvalidGameMoveException() : base() { }
}

public class MoveResult
{
    public readonly Vector2Int TheseusStartPosition;
    public readonly Vector2Int TheseusStopPosition;
    /// <summary>
    /// The path taken by the minotaur. The first position is the initial position of the minotaur and the last position is the final position of the minotaur.
    /// The entries are deduplicated, so for example a minotaur standing still would only generate a path of length 1.
    /// </summary>
    public readonly List<Vector2Int> MinotaurPath;

    public MoveResult(Vector2Int theseusStartPosition, Vector2Int theseusStopPosition, List<Vector2Int> minotaurPath)
    {
        TheseusStartPosition = theseusStartPosition;
        TheseusStopPosition = theseusStopPosition;
        MinotaurPath = minotaurPath;
    }
}

/// <summary>
/// Handles the logic of the puzzle game. Responsible for calculating the result of moves, and undo/redo.
/// </summary>
/// <remarks>
/// A <c>GameLogic</c> object cannot switch levels once constructed. To change the level being represented, construct a new object.
/// <br/>
/// The <c>GameLogic</c> class is intentionally decoupled from Unity's object management system to allow use in other contexts, especially multi-threaded environments.
/// </remarks>
public class GameLogic
{
    private int _width;
    private int _height;
    private bool[,] _northWalls;
    private bool[,] _eastWalls;
    private Vector2Int _exitPosition;

    private List<GameState> _history;
    private int _currentStateIndex;
    private int _maxRedo;

    public GameLogic(GameLevel level)
    {
        _width = level.Width;
        _height = level.Height;
        _northWalls = new bool[level.Width, level.Height];
        _eastWalls = new bool[level.Width, level.Height];

        foreach (Wall wall in level.Walls)
        {
            switch (wall.Side)
            {
                case 0: // North
                    _northWalls[wall.X, wall.Y] = true;
                    break;
                case 1: // East
                    _eastWalls[wall.X, wall.Y] = true;
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        _exitPosition = new Vector2Int(level.ExitX, level.ExitY);

        _history = new List<GameState>
        {
            new GameState(new Vector2Int(level.TheseusX, level.TheseusY), new Vector2Int(level.MinotaurX, level.MinotaurY))
        };
        _currentStateIndex = 0;
        _maxRedo = 0;
    }

    #region Win and Lose

    public bool IsWin()
    {
        return GetCurrentState().TheseusPosition == _exitPosition;
    }

    public bool IsLoss()
    {
        return GetCurrentState().MinotaurPosition == GetCurrentState().TheseusPosition;
    }

    #endregion

    #region Game Moves

    public bool CanMoveNorth(Vector2Int position)
    {
        return position.y < _height - 1 ? !_northWalls[position.x, position.y] : new Vector2Int(position.x, position.y + 1) == _exitPosition;
    }

    public bool CanMoveSouth(Vector2Int position)
    {
        return position.y > 0 ? !_northWalls[position.x, position.y - 1] : new Vector2Int(position.x, position.y - 1) == _exitPosition;
    }

    public bool CanMoveEast(Vector2Int position)
    {
        return position.x < _width - 1 ? !_eastWalls[position.x, position.y] : new Vector2Int(position.x + 1, position.y) == _exitPosition;
    }

    public bool CanMoveWest(Vector2Int position)
    {
        return position.x > 0 ? !_eastWalls[position.x - 1, position.y] : new Vector2Int(position.x - 1, position.y) == _exitPosition;
    }

    /// <summary>
    /// Represents executing a full turn in the game.
    /// The input is the direction that Theseus moves in. It can be any one of the four unit vectors or the zero vector.
    /// This method moves Theseus and then moves the minotaur. Returns an object describing the result of the game move.
    /// </summary>
    public MoveResult MakeMove(Vector2Int moveDirection)
    {
        if (IsWin() || IsLoss())
        {
            throw new InvalidGameMoveException();
        }

        Vector2Int theseusPosition = GetCurrentState().TheseusPosition;

        if (moveDirection == Vector2Int.zero)
        {
            // No-op
        }
        else if (moveDirection == Vector2Int.up)
        {
            if (!CanMoveNorth(theseusPosition)) throw new InvalidGameMoveException();
        }
        else if (moveDirection == Vector2Int.down)
        {
            if (!CanMoveSouth(theseusPosition)) throw new InvalidGameMoveException();
        }
        else if (moveDirection == Vector2Int.right)
        {
            if (!CanMoveEast(theseusPosition)) throw new InvalidGameMoveException();
        }
        else if (moveDirection == Vector2Int.left)
        {
            if (!CanMoveWest(theseusPosition)) throw new InvalidGameMoveException();
        }
        else
        {
            throw new InvalidGameMoveException();
        }

        // At this point, we've ascertained that the move is valid.
        return ForceMove(moveDirection);
    }

    /// <summary>
    /// Force an execution of a move, assuming that it is valid.
    /// Intended for use in game solver, where performace is important.
    /// For a safer method that does the same thing, use `MakeMove`.
    /// </summary>
    public MoveResult ForceMove(Vector2Int moveDirection)
    {
        Vector2Int theseusStartPosition = GetCurrentState().TheseusPosition;
        Vector2Int theseusPosition = theseusStartPosition + moveDirection;
        List<Vector2Int> minotaurPath = GetMinotaurPath(theseusPosition, GetCurrentState().MinotaurPosition);

        _currentStateIndex++;
        // If there's redo history, they are no longer valid.
        _maxRedo = _currentStateIndex;

        GameState state = new GameState(theseusPosition, minotaurPath[minotaurPath.Count - 1]);
        if (_currentStateIndex < _history.Count)
        {
            _history[_currentStateIndex] = state;
        }
        else
        {
            _history.Add(state);
        }

        return new MoveResult(theseusStartPosition, theseusPosition, minotaurPath);

    }

    // Compute the path taken by the minotaur given the current positions of the actors.
    // Minotaur moves up to 2 steps each turn.
    // The return value is in the same format as `MoveResult.MinotaurPath`.
    private List<Vector2Int> GetMinotaurPath(Vector2Int theseusPosition, Vector2Int minotaurPosition)
    {
        Vector2Int firstStep = GetMinotaurStep(theseusPosition, minotaurPosition);
        Vector2Int secondStep = GetMinotaurStep(theseusPosition, firstStep);

        List<Vector2Int> path = new List<Vector2Int> { minotaurPosition };
        if (firstStep != minotaurPosition) path.Add(firstStep);
        if (secondStep != firstStep) path.Add(secondStep);

        return path;
    }

    private Vector2Int GetMinotaurStep(Vector2Int theseusPosition, Vector2Int minotaurPosition)
    {
        // According to the rules, minotaur always picks a horizontal move first if possible.
        if (theseusPosition.x > minotaurPosition.x && CanMoveEast(minotaurPosition))
        {
            return minotaurPosition + Vector2Int.right;
        }
        else if (theseusPosition.x < minotaurPosition.x && CanMoveWest(minotaurPosition))
        {
            return minotaurPosition + Vector2Int.left;
        }
        else if (theseusPosition.y > minotaurPosition.y && CanMoveNorth(minotaurPosition))
        {
            return minotaurPosition + Vector2Int.up;
        }
        else if (theseusPosition.y < minotaurPosition.y && CanMoveSouth(minotaurPosition))
        {
            return minotaurPosition + Vector2Int.down;
        }
        else
        {
            return minotaurPosition;
        }
    }

    #endregion

    #region Undo/Redo

    public bool CanUndo()
    {
        return _currentStateIndex > 0;
    }

    public bool CanRedo()
    {
        return _currentStateIndex < _maxRedo;
    }

    public void Undo()
    {
        if (!CanUndo()) throw new InvalidGameMoveException();
        _currentStateIndex--;
    }

    public void Redo()
    {
        if (!CanRedo()) throw new InvalidGameMoveException();
        _currentStateIndex++;
    }

    #endregion

    #region State Management

    public GameState GetCurrentState() => _history[_currentStateIndex];

    /// <summary>
    /// Loads a given game state, discarding all history.
    /// Intended for use in game solver.
    /// </summary>
    public void LoadState(GameState state)
    {
        _history.Clear();
        _history.Add(state);
        _currentStateIndex = 0;
    }

    #endregion
}
