using UnityEngine;

namespace FishSort
{
    // Root of everything that exists only while playing: background, tank shelf, waiting tray, bubble field and walls.
    // Lives in a prefab; LevelManager spawns it once and turns it on/off.
    public sealed class GameplayWorld : MonoBehaviour
    {
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
