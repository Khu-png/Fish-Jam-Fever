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
    public bool IsWorldReady => world != null;

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

    // Every change between menu and game goes through the transition: the screen closes, the change happens while
    // it is covered, and the new screen is revealed.
    public void OnPlay()
    {
        TransitionManager.Ins.Play(ShowGame, StartLevel);
    }

    private void ShowGame()
    {
        world.RandomizeBackground();
        world.gameObject.SetActive(true);
        GameManager.ChangeState(GameState.GamePlay);
        UIManager.Ins.CloseAll();
        UIManager.Ins.OpenUI<FishJam.UI.GamePlay>().SetLevel(CurrentLevelNumber);
    }

    // Builds the level just before the screen opens, so its opening drop is seen.
    private void StartLevel()
    {
        GameplayController.Ins.StartLevel(GetLevelData(CurrentLevelNumber), CurrentLevelNumber);
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
        TransitionManager.Ins.Play(ShowMenu, null);
    }

    private void ShowMenu()
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
