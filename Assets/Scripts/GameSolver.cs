using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// BFS solver to find a solution to the game.
/// Can be run outside Unity's main thread.
/// </summary>
public class GameSolver
{
    /// <summary>
    /// Each game state corresponds to a search node. A search node can be one of three "meta-states": nonexistant, discovered, explored.
    /// Nonexistant search nodes do not exist in the state tree at all.
    /// Discovered search nodes have been discovered by at least one parent, but their children have not been resolved yet.
    /// Explored search nodes have their children resolved. Here we might also know whether they are a winning state or a losing state.
    /// </summary>
    /// <remarks>
    /// A state is a winning state if any of its children are winning states.
    /// A state is a losing state if all of its children are losing states.
    /// </remarks>
    private class SearchNode
    {
        /// <summary>
        /// List of parents that can lead to this game state within one move. Gets added to during the search.
        /// </summary>
        public List<GameState> Parents;

        /// <summary>
        /// List of child states that is one step away from this state. Does not change or mutate once assigned.
        /// The presence of child nodes also indicates that a node has been fully explored.
        /// </summary>
        public List<GameState> Children;
        /// <summary>
        /// If null, the state is not necessarily a winning state. If not null, indicates the minimum number of moves until a win.
        /// </summary>
        public int? WinDistance;
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

    #region Breadth First Search

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
                // Since I made the solver for a game, I think it is best to solve the state tree as fully as possible and provide advice along every step.
            }
            else if (_gameLogic.IsLoss())
            {
                // Create an empty children array to indicate that the node has been fully explored.
                _nodes[currentState].Children = new List<GameState>();
                PropagateLoss(currentState);
            }
            else // Game is not directly won or lost
            {
                SearchNode currentNode = _nodes[currentState];
                currentNode.Children = GetChildren(currentState);

                // If some or all children are fully explored, we might already know the win/loss situation.
                int? winDistance = null;
                bool isLoss = true;
                foreach (GameState child in currentNode.Children)
                {
                    if (_nodes.TryGetValue(child, out SearchNode childNode))
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
                        _nodes.Add(child, new SearchNode
                        {
                            Parents = new List<GameState> { currentState },
                        });
                        _exploreQueue.Enqueue(child);

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

    // Set a state as a winning state, and propagate upward as appropriate.
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

    // Set a state as a losing state, and propagate upward as appropriate.
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
                    // Propagation stops here. We can exit out of the function entirely since this is parent state is not a loss state.
                    return;
                }
            }

            // If the function reaches here, then no early return triggered and the node is actually a loss state.
            // Keep propagating upward.
            PropagateLoss(parentState);
        }
    }

    // Given the parent state, compute all connected child states.
    private List<GameState> GetChildren(GameState parent)
    {
        // Technically not necessary, but execute a `LoadState` anyway for consistency.
        _gameLogic.LoadState(parent);
        List<GameState> children = new List<GameState>();

        var waitState = GetCurrentChild(Vector2Int.zero);

        if (!waitState.Equals(parent))
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
    private GameState GetCurrentChild(Vector2Int moveDirection)
    {
        _gameLogic.ForceMove(moveDirection);
        GameState state = _gameLogic.GetCurrentState();
        _gameLogic.Undo();
        return state;
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