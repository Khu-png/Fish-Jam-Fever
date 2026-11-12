using System.Collections;
using FishJam.UI;
using FishSort;
using UnityEngine;

// Start-up only: shows the loading screen, does the heavy preparation in stages that end on fixed marks of the bar
// (30 / 50 / 70 / 90 %), and then reveals the main menu. Moving between menu and levels is not handled here.
public class LoadingManager : Singleton<LoadingManager>
{
    private const string STEP_CONFIG = "Loading config...";
    private const string STEP_FISH = "Loading fish...";
    private const string STEP_INTERFACE = "Loading interface...";
    private const string STEP_WORLD = "Building the sea...";
    // Where the bar stands when each stage is done.
    private const float MARK_START = 0f;
    private const float MARK_CONFIG = 0.3f;
    private const float MARK_FISH = 0.5f;
    private const float MARK_INTERFACE = 0.7f;
    private const float MARK_WORLD = 0.9f;
    private const float MARK_DONE = 1f;

    [SerializeField] private FishCatalog catalog;
    [Tooltip("Every stage takes at least this long, so the bar climbs steadily instead of jumping.")]
    [SerializeField] private float stageDuration = 0.8f;
    [Tooltip("The loading screen stays at least this long in total.")]
    [SerializeField] private float minDuration = 3.5f;

    void Awake()
    {
        RegisterSingleton(this);
    }

    // Called once by GameManager when the game starts.
    public void Begin()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float startTime = Time.realtimeSinceStartup;
        Loading screen = UIManager.Ins.OpenUI<Loading>();
        yield return Stage(screen, STEP_CONFIG, MARK_START, MARK_CONFIG, ReadConfig());
        yield return Stage(screen, STEP_FISH, MARK_CONFIG, MARK_FISH, PrewarmFish());
        yield return Stage(screen, STEP_INTERFACE, MARK_FISH, MARK_INTERFACE, BuildScreens());
        yield return Stage(screen, STEP_WORLD, MARK_INTERFACE, MARK_WORLD, WaitForWorld());
        screen.SetProgress(MARK_DONE, string.Empty);
        while (!screen.IsFull || Time.realtimeSinceStartup - startTime < minDuration)
        {
            yield return null;
        }

        // The menu opens underneath and the loading screen fades away over it.
        GameManager.ChangeState(GameState.MainMenu);
        UIManager.Ins.OpenUI<MainMenu>();
        SoundManager.Ins.StartMusic();
        screen.transform.SetAsLastSibling();
        yield return screen.FadeOut();
    }

    // Runs the work and, at the same time, moves the bar from one mark to the next over at least stageDuration.
    private IEnumerator Stage(Loading screen, string step, float from, float to, IEnumerator work)
    {
        float startTime = Time.realtimeSinceStartup;
        bool isWorking = true;
        StartCoroutine(Track(work, () => isWorking = false));
        screen.SetProgress(from, step);
        float elapsed = 0f;
        while (isWorking || elapsed < stageDuration)
        {
            elapsed = Time.realtimeSinceStartup - startTime;
            screen.SetProgress(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / stageDuration)), step);
            yield return null;
        }

        screen.SetProgress(to, step);
    }

    private static IEnumerator Track(IEnumerator work, System.Action onDone)
    {
        yield return work;
        onDone();
    }

    private static IEnumerator ReadConfig()
    {
        // Saved progress and settings are read here; the rest of the config is already part of the scene.
        Debug.Log("Saved level: " + PlayerData.Level);
        yield return null;
    }

    // Parses the fish rigs now (one per frame), so a fish does not stall the first time it is shown.
    private IEnumerator PrewarmFish()
    {
        int rigCount = catalog != null ? catalog.RigCount : 0;
        for (int i = 0; i < rigCount; i++)
        {
            catalog.PrewarmRig(i);
            yield return null;
        }
    }

    // Builds the screens now (one per frame), so opening them later does not stutter.
    private static IEnumerator BuildScreens()
    {
        Prewarm<MainMenu>();
        yield return null;
        Prewarm<GamePlay>();
        yield return null;
        Prewarm<Win>();
        yield return null;
        Prewarm<Lose>();
        yield return null;
        Prewarm<Transition>();
        Prewarm<BoosterShop>();
        Prewarm<Setting>();
        Prewarm<Revive>();
        yield return null;
    }

    private static IEnumerator WaitForWorld()
    {
        while (!LevelManager.Ins.IsWorldReady)
        {
            yield return null;
        }
    }

    private static void Prewarm<T>() where T : UICanvas
    {
        UIManager.Ins.GetUI<T>().gameObject.SetActive(false);
    }
}
