using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unity script responsible for managing the game solver and outputting its solution to the screen.
/// </summary>
public class LevelSolver : MonoBehaviour
{
    public int StepCount = 3;
    public Button ToggleSolutionButton;
    public TMP_Text SolutionText;

    private GameState _currentState;
    private GameLogic _gameLogic;
    private GameSolver _gameSolver;

    public void LoadLevel(GameLevel level)
    {
        _gameLogic = new GameLogic(level);
        _gameSolver = new GameSolver(level);
        // Currently, the solver is being run synchronously.
        // At one point it was done asynchronously using System.Threading.Tasks system, but that doesn't play nice with the WebGL build.
        // In the future,we could try running the solver asynchronously using Unity's Job system if that really becomes an issue.
        _gameSolver.RunSearch();

        SolutionText.gameObject.SetActive(false);
        ToggleSolutionButton.GetComponentInChildren<TMP_Text>().text = "Show solution";
    }

    public void ToggleSolution()
    {
        if (SolutionText.gameObject.activeSelf)
        {
            SolutionText.gameObject.SetActive(false);
            ToggleSolutionButton.GetComponentInChildren<TMP_Text>().text = "Show solution";
        }
        else
        {
            SolutionText.gameObject.SetActive(true);
            ShowSolution();
            ToggleSolutionButton.GetComponentInChildren<TMP_Text>().text = "Hide solution";
        }
    }

    /// <summary>
    /// Informs the solver of the current game state.
    /// This will update the solution being shown.
    /// </summary>
    public void SetGameState(GameState currentState)
    {
        _currentState = currentState;

        if (_gameSolver.Completed)
        {
            ShowSolution();
        }
    }

    private void ShowSolution()
    {
        _gameLogic.LoadState(_currentState);

        if (_gameLogic.IsWin())
        {
            SolutionText.text = "You've won.";
            return;
        }
        if (_gameLogic.IsLoss())
        {
            SolutionText.text = "You've lost.";
            return;
        }

        Vector2Int? move = _gameSolver.GetWinningMove(_gameLogic.GetCurrentState());

        if (move == null)
        {
            SolutionText.text = "<color=red>There is no escape.</color>";
        }
        else
        {
            // Returning a value means there is a way out. Build a string out of the instructions.
            StringBuilder builder = new StringBuilder($"Winning moves up to {StepCount} steps:");
            builder.AppendLine().Append(GetHumanReadableDirection(move.Value));

            _gameLogic.ForceMove(move.Value);

            for (int i = 1; i < StepCount; i++)
            {
                if (_gameLogic.IsWin())
                {
                    builder.AppendLine().Append("Win!");
                    break;
                }
                else
                {
                    move = _gameSolver.GetWinningMove(_gameLogic.GetCurrentState());
                    Debug.Assert(move != null, $"Step {i} of solution should not be null");

                    builder.AppendLine().Append(GetHumanReadableDirection(move.Value));
                    _gameLogic.ForceMove(move.Value);
                }
            }

            SolutionText.text = builder.ToString();
        }

    }

    private string GetHumanReadableDirection(Vector2Int move)
    {
        if (move == Vector2Int.zero)
        {
            return "Wait";
        }
        else if (move == Vector2Int.up)
        {
            return "Move up";
        }
        else if (move == Vector2Int.down)
        {
            return "Move down";
        }
        else if (move == Vector2Int.right)
        {
            return "Move right";
        }
        else if (move == Vector2Int.left)
        {
            return "Move left";
        }
        else
        {
            throw new ArgumentException();
        }
    }
}
