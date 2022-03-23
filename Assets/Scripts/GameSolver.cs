using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BFS solver to find a solution to the game.
/// </summary>
public class GameSolver
{
    // Each game state corresponds to a search node. A search node can be one of three "types": nonexistant, discovered, explored.
    // Nonexistant search nodes do not exist in the state tree at all.
    // Discovered search nodes have been discovered, but their children have not been resolved yet.
    // Explored search nodes have their children resolved. Here we might also know whether they are a winning state or a losing state.
    private class SearchNode
    {
        // The parent node from which this game state was first discovered. Does not change once assigned.
        public GameState Parent;
        public Vector2Int PrevMove;
        public List<GameState> Children;
        public bool IsWin;
        public bool IsLoss;
    }

    // For the first theee fields, readonly modifier is just to prevent accidental modification. It does not indicate whether the fields are mutable.
    private readonly GameLogic _gameLogic;
    private readonly Dictionary<GameState, SearchNode> _nodes;
    private readonly Queue<GameState> _exploreQueue;

    // `_rootState` is actually immutable.
    private readonly GameState _rootState;

    public GameSolver(GameLevel level)
    {
        _gameLogic = new GameLogic(level);
        _nodes = new Dictionary<GameState, SearchNode>();
        _exploreQueue = new Queue<GameState>();
        _rootState = _gameLogic.GetCurrentState();
    }

    #region Tree Search

    public void RunSearch()
    {
        _nodes.Clear();
        _exploreQueue.Clear();

        _nodes.Add(_rootState, new SearchNode
        {
            Parent = null,
            PrevMove = Vector2Int.zero,
            Children = null,
            IsWin = false,
            IsLoss = false
        });
        _exploreQueue.Enqueue(_rootState);

        while (_exploreQueue.Count > 0)
        {
            GameState exploreState = _exploreQueue.Dequeue();

            _gameLogic.LoadState(exploreState);

            if (_gameLogic.IsWin())
            {
                // Create an empty children array to indicate that the node has been fully explored.
                _nodes[exploreState].Children = new List<GameState>();
                PropagateWin(exploreState);

                // We could go on and explore the entire state tree. We could also say "good enough, I know how to win".
                // I need to compare how long it takes to run each of the two options.
                // return;
            }
            else if (_gameLogic.IsLoss())
            {
                // Create an empty children array to indicate that the node has been fully explored.
                _nodes[exploreState].Children = new List<GameState>();
                PropagateLoss(exploreState);
            }
            else
            {
                SearchNode currentNode = _nodes[exploreState];
                List<Tuple<Vector2Int, GameState>> childStates = GetCurrentChildren(exploreState);
                currentNode.Children = new List<GameState>();

                // If some or all children are fully explored, we might already know the win/loss situation.
                bool isWin = false, isLoss = true;
                foreach (Tuple<Vector2Int, GameState> childState in childStates)
                {
                    currentNode.Children.Add(childState.Item2);

                    if (_nodes.TryGetValue(childState.Item2, out SearchNode childNode))
                    {
                        // Child already discovered by another node
                        if (childNode.IsWin)
                        {
                            isWin = true;
                        }
                        if (!childNode.IsLoss)
                        {
                            isLoss = false;
                        }
                    }
                    else
                    {
                        // Child not yet discovered, add it to the state tree and queue it up for exploration.
                        _nodes.Add(childState.Item2, new SearchNode
                        {
                            Parent = exploreState,
                            PrevMove = childState.Item1
                        });

                        // Since there are unexplored children, we definitely don't know if this is a loss state.
                        isLoss = false;
                    }
                }

                currentNode.IsWin = isWin;
                currentNode.IsLoss = isLoss;
            }
        }
    }

    private void PropagateWin(GameState winState)
    {
        GameState currentState = winState;
        while (currentState != null)
        {
            SearchNode node = _nodes[currentState];
            if (node.IsWin) return; // Already know it's a winning state
            node.IsWin = true;
            currentState = node.Parent;
        }
    }

    private void PropagateLoss(GameState lossState)
    {
        _nodes[lossState].IsLoss = true;
        GameState currentState = _nodes[lossState].Parent;

        while (currentState != null)
        {
            SearchNode node = _nodes[currentState];

            // We already know this is a losing state. We also already went through its parent states at one point, we aren't adding any new information here.
            // Early return.
            if (node.IsLoss) return;

            foreach (GameState child in node.Children)
            {
                if (!_nodes[child].IsLoss)
                {
                    // Propagation stops here. We can exit out of the function entirely.
                    return;
                }
            }

            // If the function reaches here, then no early return triggered and the node is actually a loss state.
            // Keep propagating upward.
            node.IsLoss = true;
            currentState = node.Parent;
        }
    }

    private List<Tuple<Vector2Int, GameState>> GetCurrentChildren(GameState parent)
    {
        List<Tuple<Vector2Int, GameState>> children = new List<Tuple<Vector2Int, GameState>>();

        GameState waitState = GetCurrentChild(Vector2Int.zero);
        if (!waitState.Equals(parent))
        {
            children.Add(Tuple.Create(Vector2Int.zero, waitState));
        }

        if (_gameLogic.CanMoveNorth(parent.TheseusPosition))
        {
            children.Add(Tuple.Create(Vector2Int.up, GetCurrentChild(Vector2Int.up)));
        }
        if (_gameLogic.CanMoveSouth(parent.TheseusPosition))
        {
            children.Add(Tuple.Create(Vector2Int.down, GetCurrentChild(Vector2Int.down)));
        }
        if (_gameLogic.CanMoveEast(parent.TheseusPosition))
        {
            children.Add(Tuple.Create(Vector2Int.right, GetCurrentChild(Vector2Int.right)));
        }
        if (_gameLogic.CanMoveWest(parent.TheseusPosition))
        {
            children.Add(Tuple.Create(Vector2Int.left, GetCurrentChild(Vector2Int.left)));
        }

        return children;
    }

    // Assuming that the parent state is already loaded into `_gameLogic`, get the child state that results from executing the move.
    // This also automatically reverts the change so that the next call can be made immediately.
    // All in all this is a bit confusing, but I'm trying to keep a balance between readability and performance.
    private GameState GetCurrentChild(Vector2Int moveDirection)
    {
        _gameLogic.ForceMove(Vector2Int.up);
        GameState state = _gameLogic.GetCurrentState();
        _gameLogic.Undo();
        return state;
    }

    #endregion

    #region Query

    public Vector2Int? GetWinningMove(GameState state)
    {
        SearchNode node = _nodes[state];

        if (!node.IsWin)
        {
            return null;
        }

        foreach (GameState child in node.Children)
        {
            if (_nodes[child].IsWin)
            {
                return _nodes[child].PrevMove;
            }
        }

        throw new Exception("Failed to find winning move in a win state");
    }

    #endregion
}