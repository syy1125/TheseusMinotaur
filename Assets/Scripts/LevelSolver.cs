using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSolver : MonoBehaviour
{
    public int StepCount = 3;
    public Button ToggleSolutionButton;
    public TMP_Text SolutionText;

    private GameState _currentState;
    private GameLogic _gameLogic;
    private GameSolver _gameSolver;
    private Coroutine _solveCoroutine;

    public void LoadLevel(GameLevel level)
    {
        if (_solveCoroutine != null)
        {
            StopCoroutine(_solveCoroutine);
            _solveCoroutine = null;
        }

        _gameLogic = new GameLogic(level);
        _gameSolver = new GameSolver(level);
        _solveCoroutine = StartCoroutine(SolveLevel());

        ToggleSolutionButton.interactable = false;
        ToggleSolutionButton.GetComponentInChildren<TMP_Text>().text = "Solver is running...";
        SolutionText.gameObject.SetActive(false);
    }

    private IEnumerator SolveLevel()
    {
        Task solveTask = Task.Run(_gameSolver.RunSearch);

        float startTime = Time.time;
        int frames = 0;

        while (!solveTask.IsCompleted)
        {
            frames++;
            yield return null;
        }

        Debug.Log($"Solver final state is {solveTask.Status}; done in {(Time.time - startTime):0.00}s, {frames} frames");
        _solveCoroutine = null;

        ToggleSolutionButton.interactable = true;
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
            // Returning a value means there is a way out
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
