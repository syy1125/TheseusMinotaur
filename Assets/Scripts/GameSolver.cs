using System;
using System.Collections.Generic;
using System.Text;
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
        // List of parents that can lead to this game state within one move. Gets added to during the search.
        public List<GameState> Parents;
        // List of child states that is one step away from this state. Does not change or mutate once assigned.
        // The presence of child nodes also indicates that a node has been fully explored.
        public List<GameState> Children;
        // Nulable int. If null, the state is not necessarily a winning state. If not null, indicates the minimum number of moves until a win.
        public int? WinDistance;
        // A state is a loss if all its children are loss.
        public bool IsLoss;
    }

    // For the first theee fields, readonly modifier is just to prevent accidental modification. It does not indicate whether the fields are mutable.
    private readonly GameLogic _gameLogic;
    private readonly Dictionary<GameState, SearchNode> _nodes;
    private readonly Queue<GameState> _exploreQueue;

    // `_rootState` is actually immutable.
    private readonly GameState _rootState;

    public bool Completed { get; private set; }

    public GameSolver(GameLevel level)
    {
        _gameLogic = new GameLogic(level);
        _nodes = new Dictionary<GameState, SearchNode>();
        _exploreQueue = new Queue<GameState>();
        _rootState = _gameLogic.GetCurrentState();

        Completed = false;
    }

    #region Tree Search

    public void RunSearch()
    {
        _nodes.Clear();
        _exploreQueue.Clear();

        _nodes.Add(_rootState, new SearchNode
        {
            Parents = new List<GameState>(),
            Children = null,
            WinDistance = null,
            IsLoss = false
        });
        _exploreQueue.Enqueue(_rootState);

        while (_exploreQueue.Count > 0)
        {
            GameState currentState = _exploreQueue.Dequeue();

            _gameLogic.LoadState(currentState);

            if (_gameLogic.IsWin())
            {
                // Create an empty children array to indicate that the node has been fully explored.
                _nodes[currentState].Children = new List<GameState>();
                PropagateWin(currentState, 0);

                // We could go on and explore the entire state tree. This lets the solver provide advice every step of the way.
                // We could also say "good enough, I know how to win" and stop here.
                // If I have time, I can compare how long it takes to run each of the two options.
                // return;
            }
            else if (_gameLogic.IsLoss())
            {
                // Create an empty children array to indicate that the node has been fully explored.
                _nodes[currentState].Children = new List<GameState>();
                PropagateLoss(currentState);
            }
            else
            {
                SearchNode currentNode = _nodes[currentState];
                List<Tuple<Vector2Int, GameState>> childStates = GetCurrentChildren(currentState);
                currentNode.Children = new List<GameState>();

                // If some or all children are fully explored, we might already know the win/loss situation.
                int? winDistance = null;
                bool isLoss = true;
                foreach (Tuple<Vector2Int, GameState> childState in childStates)
                {
                    currentNode.Children.Add(childState.Item2);

                    if (_nodes.TryGetValue(childState.Item2, out SearchNode childNode))
                    {
                        // Child already discovered by another node
                        childNode.Parents.Add(currentState);

                        if (childNode.WinDistance != null)
                        {
                            winDistance = winDistance == null ? childNode.WinDistance + 1 : Mathf.Min(winDistance.Value + 1, childNode.WinDistance.Value + 1);
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
                            Parents = new List<GameState> { currentState },
                        });
                        _exploreQueue.Enqueue(childState.Item2);

                        // Since there are unexplored children, we definitely don't know if this is a loss state.
                        isLoss = false;

                    }
                }

                currentNode.WinDistance = winDistance;
                currentNode.IsLoss = isLoss;
            }
        }

        Completed = true;
    }

    private void PropagateWin(GameState winState, int winDistance)
    {
        SearchNode node = _nodes[winState];
        if (node.WinDistance != null && node.WinDistance < winDistance) return; // It already has a better winning state

        node.WinDistance = winDistance;

        foreach (GameState parentState in node.Parents)
        {
            PropagateWin(parentState, winDistance + 1);
        }
    }

    private void PropagateLoss(GameState lossState)
    {
        _nodes[lossState].IsLoss = true;

        foreach (GameState parentState in _nodes[lossState].Parents)
        {
            SearchNode node = _nodes[parentState];

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

            PropagateLoss(parentState);
        }
    }

    private List<Tuple<Vector2Int, GameState>> GetCurrentChildren(GameState parent)
    {
        List<Tuple<Vector2Int, GameState>> children = new List<Tuple<Vector2Int, GameState>>();

        var waitState = GetCurrentChild(Vector2Int.zero);

        if (!waitState.Item2.Equals(parent))
        {
            children.Add(waitState);
        }

        if (_gameLogic.CanMoveNorth(parent.TheseusPosition))
        {
            children.Add(GetCurrentChild(Vector2Int.up));
        }
        if (_gameLogic.CanMoveSouth(parent.TheseusPosition))
        {
            children.Add(GetCurrentChild(Vector2Int.down));
        }
        if (_gameLogic.CanMoveEast(parent.TheseusPosition))
        {
            children.Add(GetCurrentChild(Vector2Int.right));
        }
        if (_gameLogic.CanMoveWest(parent.TheseusPosition))
        {
            children.Add(GetCurrentChild(Vector2Int.left));
        }

        return children;
    }

    // Assuming that the parent state is already loaded into `_gameLogic`, get the child state that results from executing the move.
    // This also automatically reverts the change so that the next call can be made immediately.
    // All in all this is a bit confusing, but I'm trying to keep a balance between readability and performance.
    private Tuple<Vector2Int, GameState> GetCurrentChild(Vector2Int moveDirection)
    {
        _gameLogic.ForceMove(moveDirection);
        GameState state = _gameLogic.GetCurrentState();
        _gameLogic.Undo();
        return Tuple.Create(moveDirection, state);
    }

    #endregion

    #region Query

    public Vector2Int? GetWinningMove(GameState state)
    {
        SearchNode node = _nodes[state];

        if (node.WinDistance == null)
        {
            return null;
        }

        int? bestDistance = null;
        Vector2Int? bestMove = null;
        foreach (GameState child in node.Children)
        {
            if (_nodes[child].WinDistance != null)
            {
                if (bestDistance == null || bestDistance > _nodes[child].WinDistance)
                {
                    bestDistance = _nodes[child].WinDistance;
                    bestMove = child.TheseusPosition - state.TheseusPosition;
                }
            }
        }

        if (bestMove == null)
        {
            throw new Exception("Failed to find winning move in a win state");
        }
        return bestMove;
    }

    #endregion
}