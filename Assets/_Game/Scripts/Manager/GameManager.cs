using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { MainMenu, GamePlay, Finish, Revive, Setting }

public class GameManager : Singleton<GameManager>
{
    private static GameState gameState;

    public static void ChangeState(GameState state)
    {
        gameState = state;
    }

    public static bool IsState(GameState state) => gameState == state;

    // Revives used in the current level (reset when the level starts).
    public int ReviveCount { get; set; }

    // Win/lose are decided here: gameplay reports the result, GameManager moves the game to Finish and opens the result UI.
    public void OnWin()
    {
        LevelManager.Ins.CompleteLevel();
        ChangeState(GameState.Finish);
        SoundManager.Ins.PlayFx(FxID.Win);
        // The gameplay HUD (level, fish count, boosters...) goes away behind the win screen.
        UIManager.Ins.CloseUI<FishJam.UI.GamePlay>();
        UIManager.Ins.OpenUI<FishJam.UI.Win>();
    }

    // Tray full: offer a revive first. Declining it (the X button) ends the level through OnLose.
    public void OnAlmostLose(float progress)
    {
        ChangeState(GameState.Revive);
        UIManager.Ins.OpenUI<FishJam.UI.Revive>().SetProgress(progress);
    }

    public void OnLose(float progress)
    {
        ChangeState(GameState.Finish);
        SoundManager.Ins.PlayFx(FxID.Lose);
        UIManager.Ins.OpenUI<FishJam.UI.Lose>();
    }

    void Awake()
    {
        RegisterSingleton(this);

        // Project uses the Input System only (legacy UnityEngine.Input throws).
        // Multi-touch is handled by reading Pointer.current (primary touch) in FishPicker.
        //target frame rate ve 60 fps
        Application.targetFrameRate = 60;
        //tranh viec tat man hinh
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //xu tai tho
        int maxScreenHeight = 1280;
        float ratio = (float)Screen.currentResolution.width / (float)Screen.currentResolution.height;
        if (Screen.currentResolution.height > maxScreenHeight)
        {
            Screen.SetResolution(Mathf.RoundToInt(ratio * (float)maxScreenHeight), maxScreenHeight, true);
        }
    }

    private void Start()
    {
        // The loading screen prepares everything, then opens the main menu (see LoadingManager).
        LoadingManager.Ins.Begin();
    }
}
