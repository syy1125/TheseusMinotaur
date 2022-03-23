using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelController : MonoBehaviour
{
    /// <summary>
    /// The maximum size of the grid. This should be slightly less than the full size visible to the camera.
    /// The grid will be scaled down if necessary to fit within <c>MaxSize</c>.
    /// </summary>
    [Header("Configuration")]
    public float MaxSize;
    public float AnimationTime = 0.1f;

    [Header("References")]
    public GameController GameController;
    public Transform TheseusTransform;
    public Transform MinotaurTransform;

    [Header("Prefabs")]
    public GameObject TilePrefab;
    public GameObject ExitTilePrefab;
    public GameObject NorthWallPrefab;
    public GameObject EastWallPrefab;


    [Header("Actions")]
    public InputAction MoveNorthAction;
    public InputAction MoveSouthAction;
    public InputAction MoveEastAction;
    public InputAction MoveWestAction;
    public InputAction WaitAction;

    private GameLogic _gameLogic;

    // The coroutine that animates the game piece movement.
    // If the coroutine is not null, then the game is actively animating the game pieces. In that case, the game will ignore further player input until the animation is done.
    private Coroutine _moveAnimation;

    private void OnEnable()
    {
        MoveNorthAction.Enable();
        MoveSouthAction.Enable();
        MoveEastAction.Enable();
        MoveWestAction.Enable();
        WaitAction.Enable();
    }

    private void OnDisable()
    {
        MoveNorthAction.Disable();
        MoveSouthAction.Disable();
        MoveEastAction.Disable();
        MoveWestAction.Disable();
        WaitAction.Disable();
    }

    #region Level Loading

    public void LoadLevel(GameLevel level)
    {
        _gameLogic = new GameLogic(level);

        CleanupPreviousGrid();
        ScaleGrid(level);
        SpawnTiles(level);
        SpawnBoundaryWalls(level);
        SpawnInnerWalls(level);
        PositionGamePieces(level);
    }

    private void CleanupPreviousGrid()
    {
        foreach (Transform child in transform)
        {
            if (child != TheseusTransform && child != MinotaurTransform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void ScaleGrid(GameLevel level)
    {
        Vector2 center = new Vector2((level.Width - 1) / 2f, (level.Height - 1) / 2f);
        float scale = Mathf.Min(MaxSize / Mathf.Max(level.Width, level.Height), 1f);
        transform.position = -center * scale;
        transform.localScale = new Vector3(scale, scale, 1);
    }

    private void SpawnTiles(GameLevel level)
    {
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                GameObject tile = Instantiate(TilePrefab, transform);
                tile.transform.localPosition = new Vector3(x, y);
            }
        }

        GameObject exitTile = Instantiate(ExitTilePrefab, transform);
        exitTile.transform.localPosition = new Vector3(level.ExitX, level.ExitY);
    }

    private void SpawnBoundaryWalls(GameLevel level)
    {
        for (int x = 0; x < level.Width; x++)
        {
            GameObject southWall = Instantiate(NorthWallPrefab, transform);
            southWall.transform.localPosition = new Vector3(x, -1, 0);
            GameObject northWall = Instantiate(NorthWallPrefab, transform);
            northWall.transform.localPosition = new Vector3(x, level.Height - 1, 0);

            if (level.ExitX == x)
            {
                if (level.ExitY == -1)
                {
                    southWall.SetActive(false);
                }
                else if (level.ExitY == level.Height)
                {
                    northWall.SetActive(false);
                }
            }
        }

        for (int y = 0; y < level.Height; y++)
        {
            GameObject westWall = Instantiate(EastWallPrefab, transform);
            westWall.transform.localPosition = new Vector3(-1, y, 0);
            GameObject eastWall = Instantiate(EastWallPrefab, transform);
            eastWall.transform.localPosition = new Vector3(level.Width - 1, y, 0);

            if (level.ExitY == y)
            {
                if (level.ExitX == -1)
                {
                    westWall.SetActive(false);
                }
                else if (level.ExitX == level.Width)
                {
                    eastWall.SetActive(false);
                }
            }
        }
    }

    private void SpawnInnerWalls(GameLevel level)
    {
        foreach (Wall wall in level.Walls)
        {
            switch (wall.Side)
            {
                case 0:
                    Instantiate(NorthWallPrefab, transform).transform.localPosition = new Vector3(wall.X, wall.Y);
                    break;
                case 1:
                    Instantiate(EastWallPrefab, transform).transform.localPosition = new Vector3(wall.X, wall.Y);
                    break;
                default:
                    throw new ArgumentException();
            }
        }
    }

    private void PositionGamePieces(GameLevel level)
    {
        TheseusTransform.localPosition = new Vector3(level.TheseusX, level.TheseusY);
        MinotaurTransform.localPosition = new Vector3(level.MinotaurX, level.MinotaurY);
    }

    #endregion

    private void Update()
    {
        if (_moveAnimation != null) return;

        Vector2Int theseusPosition = _gameLogic.GetCurrentState().TheseusPosition;
        MoveResult moveResult = null;

        if (MoveNorthAction.triggered && _gameLogic.CanMoveNorth(theseusPosition))
        {
            moveResult = _gameLogic.MakeMove(Vector2Int.up);
        }
        else if (MoveSouthAction.triggered && _gameLogic.CanMoveSouth(theseusPosition))
        {
            moveResult = _gameLogic.MakeMove(Vector2Int.down);
        }
        else if (MoveEastAction.triggered && _gameLogic.CanMoveEast(theseusPosition))
        {
            moveResult = _gameLogic.MakeMove(Vector2Int.right);
        }
        else if (MoveWestAction.triggered && _gameLogic.CanMoveWest(theseusPosition))
        {
            moveResult = _gameLogic.MakeMove(Vector2Int.left);
        }
        else if (WaitAction.triggered)
        {
            moveResult = _gameLogic.MakeMove(Vector2Int.zero);
        }

        if (moveResult != null)
        {
            _moveAnimation = StartCoroutine(AnimateGameMove(moveResult));
            return;
        }
    }

    private IEnumerator AnimateGameMove(MoveResult moveResult)
    {
        if (moveResult.TheseusStopPosition != moveResult.TheseusStartPosition)
        {
            yield return StartCoroutine(AnimateGamePiece(TheseusTransform, moveResult.TheseusStartPosition, moveResult.TheseusStopPosition));
        }

        if (_gameLogic.IsWin())
        {
            GameController.OpenWinScreen();
            EndAnimation();
            yield break;
        }

        for (int i = 0; i < moveResult.MinotaurPath.Count - 1; i++)
        {
            yield return StartCoroutine(AnimateGamePiece(MinotaurTransform, moveResult.MinotaurPath[i], moveResult.MinotaurPath[i + 1]));
        }

        if (_gameLogic.IsLoss())
        {
            GameController.OpenLossScreen();
        }
        EndAnimation();
    }

    private IEnumerator AnimateGamePiece(Transform gamePiece, Vector2 start, Vector2 stop)
    {
        float startTime = Time.time;
        float stopTime = startTime + AnimationTime;

        while (Time.time < stopTime)
        {
            gamePiece.localPosition = Vector2.Lerp(start, stop, Mathf.InverseLerp(startTime, stopTime, Time.time));
            yield return null;
        }

        gamePiece.localPosition = stop;
    }

    private void EndAnimation()
    {
        _moveAnimation = null;
    }
}
