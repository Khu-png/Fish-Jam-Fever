using FishSort;
using UnityEngine;

// Level progression: which LevelData to play, and the menu / win / lose flow around it.
public class LevelManager : Singleton<LevelManager>
{
    // Level 1 is the tutorial; after the last asset, play cycles through the non-tutorial levels.
    [SerializeField] private LevelData[] levels;
    // Background, tank shelf, waiting tray and bubble field. Spawned once at start and hidden until a level begins.
    [SerializeField] private GameplayWorld worldPrefab;
    [SerializeField] private FishPicker fishPicker;

    private GameplayWorld world;

    public int CurrentLevelNumber => PlayerData.Level;

    void Awake()
    {
        RegisterSingleton(this);
    }

    void Start()
    {
        world = Instantiate(worldPrefab);
        world.name = worldPrefab.name;
        GameplayController.Ins.Bind(world);
        fishPicker.Bind(world);
        world.gameObject.SetActive(false);
    }

    public void OnPlay()
    {
        int levelNumber = CurrentLevelNumber;
        world.gameObject.SetActive(true);
        GameManager.ChangeState(GameState.GamePlay);
        UIManager.Ins.CloseAll();
        UIManager.Ins.OpenUI<UIGameplay>().SetLevel(levelNumber);
        GameplayController.Ins.StartLevel(GetLevelData(levelNumber), levelNumber);
    }

    // Called by GameManager.OnWin: the saved level moves on, so "next" and "retry" both just play the current one.
    public void CompleteLevel()
    {
        PlayerData.Level = CurrentLevelNumber + 1;
    }

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
        world.gameObject.SetActive(false);
        GameManager.ChangeState(GameState.MainMenu);
        UIManager.Ins.CloseAll();
        UIManager.Ins.OpenUI<FishJam.UI.MainMenu>();
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
