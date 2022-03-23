using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The controller responsible for overall game flow.
/// Handles level selection, reloading, next level, and win/loss states.
/// </summary>
public class GameController : MonoBehaviour
{
    [Header("References")]
    public LevelController LevelController;

    [Space]
    public GameObject GameCanvas;

    [Space]
    public GameObject LevelEndScreen;
    public TMP_Text WinLossText;
    public GameObject RestartLevelButton;
    public GameObject NextLevelButton;

    [Space]
    public GameObject LevelSelect;


    [Header("Prefabs")]
    public GameObject LevelSelectButtonPrefab;

    public TextAsset[] Levels;

    private int _currentLevel;

    private void Start()
    {
        for (int i = 0; i < Levels.Length; i++)
        {
            // `i` changes over the loop but `levelIndex` does not.
            // This is important for when we construct the button's on click listener.
            int levelIndex = i;
            GameObject button = Instantiate(LevelSelectButtonPrefab, LevelSelect.transform);
            button.GetComponent<Button>().onClick.AddListener(() => LoadLevel(levelIndex));
            button.GetComponentInChildren<TMP_Text>().text = JsonUtility.FromJson<GameLevel>(Levels[i].text).Name;
        }

        LoadLevel(0);
    }

    private void LoadLevel(int levelIndex)
    {
        LevelController.LoadLevel(JsonUtility.FromJson<GameLevel>(Levels[levelIndex].text));
        _currentLevel = levelIndex;

        LevelController.enabled = true;
        GameCanvas.SetActive(false);
    }

    #region Callbacks

    public void OpenWinScreen()
    {
        LevelController.enabled = false;
        GameCanvas.SetActive(true);
        LevelEndScreen.SetActive(true);
        LevelSelect.SetActive(false);

        RestartLevelButton.SetActive(false);
        NextLevelButton.SetActive(true);
        NextLevelButton.GetComponent<Button>().interactable = _currentLevel < Levels.Length - 1;

        WinLossText.text = "Win - You escaped!";
        WinLossText.color = Color.green;
    }

    public void OpenLossScreen()
    {
        LevelController.enabled = false;
        GameCanvas.SetActive(true);
        LevelEndScreen.SetActive(true);
        LevelSelect.SetActive(false);

        RestartLevelButton.SetActive(true);
        NextLevelButton.SetActive(false);

        WinLossText.text = "Loss - The minotaur got you!";
        WinLossText.color = Color.red;
    }

    public void OpenLevelSelect()
    {
        LevelController.enabled = false;
        GameCanvas.SetActive(true);
        LevelEndScreen.SetActive(false);
        LevelSelect.SetActive(true);
    }

    public void NextLevel()
    {
        if (_currentLevel >= Levels.Length - 1)
        {
            Debug.LogError("NextLevel called when the current level is already the last one!");
            return;
        }

        LoadLevel(_currentLevel + 1);
    }

    public void RestartLevel()
    {
        LoadLevel(_currentLevel);
    }

    #endregion
}
