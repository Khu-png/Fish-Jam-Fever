using FishSort;
using UnityEngine;

// Level progression: which LevelData to play, and the menu / win / lose flow around it.
public class LevelManager : Singleton<LevelManager>
{
    // Level 1 is the tutorial; after the last asset, play cycles through the non-tutorial levels.
    [SerializeField] private LevelData[] levels;

    public int CurrentLevelNumber => PlayerData.Level;

    public void OnPlay()
    {
        int levelNumber = CurrentLevelNumber;
        GameManager.ChangeState(GameState.GamePlay);
        UIManager.Ins.CloseAll();
        UIManager.Ins.OpenUI<UIGameplay>().SetLevel(levelNumber);
        GameplayController.Ins.StartLevel(GetLevelData(levelNumber), levelNumber);
    }

    public void OnWin()
    {
        PlayerData.Level = CurrentLevelNumber + 1;
        GameManager.ChangeState(GameState.Finish);
        UIManager.Ins.OpenUI<UIWin>();
    }

    public void OnLose(float progress)
    {
        GameManager.ChangeState(GameState.Finish);
        UIManager.Ins.OpenUI<UILose>().SetProgress(progress);
    }

    // Win already advanced the saved level, so "next" just plays the current one.
    public void OnNextLevel()
    {
        OnPlay();
    }

    public void OnRetry()
    {
        OnPlay();
    }

    public void OnHome()
    {
        GameplayController.Ins.StopLevel();
        GameManager.ChangeState(GameState.MainMenu);
        UIManager.Ins.CloseAll();
        UIManager.Ins.OpenUI<UIMainMenu>();
    }

    private LevelData GetLevelData(int levelNumber)
    {
        int index = levelNumber - FishSortConst.FIRST_LEVEL;
        if (index >= levels.Length)
        {
            int loopLength = Mathf.Max(1, levels.Length - FishSortConst.FIRST_LEVEL);
            int loopStart = levels.Length - loopLength;
            index = loopStart + (index - levels.Length) % loopLength;
        }

        return levels[index];
    }
}
