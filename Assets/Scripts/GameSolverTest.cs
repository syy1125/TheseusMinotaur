using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameSolverTest : MonoBehaviour
{
    private IEnumerator Start()
    {
        GameLevel level = new GameLevel
        {
            Width = 3,
            Height = 3,
            TheseusX = 1,
            TheseusY = 2,
            MinotaurX = 1,
            MinotaurY = 0,
            ExitX = 3,
            ExitY = 1,
            Walls = new[] {
                new Wall{X=1,Y=0,Side=0},
                new Wall{X=1,Y=1,Side=0},
                new Wall{X=1,Y=1,Side=1},
            }
        };

        GameSolver solver = new GameSolver(level);

        // solver.RunSearch();

        Task searchTask = Task.Run(() => solver.RunSearch());

        float startTime = Time.time;
        int frames = 0;
        while (!searchTask.IsCompleted)
        {
            frames++;
            yield return null;
        }

        Debug.Log(searchTask.Status);
        Debug.Log($"Search done in {frames} frames, {(Time.time - startTime):0.00}s");

        Debug.Log("Winning move is " + solver.GetWinningMove(new GameLogic(level).GetCurrentState()));

        yield break;
    }
}
