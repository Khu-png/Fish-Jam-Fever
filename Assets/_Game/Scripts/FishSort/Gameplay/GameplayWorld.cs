using UnityEngine;

namespace FishSort
{
    // Root of everything that exists only while playing: background, tank shelf, waiting tray, bubble field and walls.
    // Lives in a prefab; LevelManager spawns it once and turns it on/off.
    public sealed class GameplayWorld : MonoBehaviour
    {
        // Sea backgrounds (Resources/data/images/sprites/Resources/background). One is picked at random for each level.
        private const string BACKGROUND_FOLDER = "data/images/sprites/Resources/background/";
        private static readonly string[] BackgroundNames =
        {
            "Map_1", "Map_2", "Map_3", "Map_4", "Map_5", "Map_6", "Map_7", "Map_8", "Map_9",
            "Map_11", "Map_14", "Map_15", "Map_21", "Map_22", "Map_23", "Map_24"
        };

        [SerializeField] private SpriteRenderer background;
        [SerializeField] private BubbleField field;
        [SerializeField] private TankShelf shelf;
        [SerializeField] private WaitingTray tray;
        [SerializeField] private ComboCounter combo;
        [SerializeField] private FishingRod rod;
        [SerializeField] private CannonBooster cannon;
        [SerializeField] private FishJar jar;
        [SerializeField] private ScreenEdgeAnchor[] edgeAnchors;

        public BubbleField Field => field;
        public TankShelf Shelf => shelf;
        public WaitingTray Tray => tray;
        public ComboCounter Combo => combo;
        public FishingRod Rod => rod;
        public CannonBooster Cannon => cannon;
        public FishJar Jar => jar;

        private int backgroundIndex = -1;

        // A new random background (never the same one twice in a row). It is loaded now, one at a time, and the
        // ones no longer used are released.
        public void RandomizeBackground()
        {
            int next = Random.Range(0, BackgroundNames.Length - (backgroundIndex >= 0 ? 1 : 0));
            if (backgroundIndex >= 0 && next >= backgroundIndex)
            {
                next++;
            }

            Sprite sprite = Resources.Load<Sprite>(BACKGROUND_FOLDER + BackgroundNames[next]);
            if (sprite != null)
            {
                background.sprite = sprite;
                backgroundIndex = next;
                Resources.UnloadUnusedAssets();
            }
        }

        // A prefab cannot reference the scene camera, so the owner passes it in.
        public void Initialize(Camera sceneCamera)
        {
            for (int i = 0; i < edgeAnchors.Length; i++)
            {
                edgeAnchors[i].SetCamera(sceneCamera);
            }
        }
    }
}
