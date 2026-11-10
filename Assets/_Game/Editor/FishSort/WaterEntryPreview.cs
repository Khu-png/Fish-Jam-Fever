#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FishSort.Editor
{
    // Pause a real fish entry at the exact water-contact event for visual inspection.
    internal static class WaterEntryPreview
    {
        private static TankWaterSplash[] observed;

        [MenuItem("Tools/FishSort/Preview Tank Water Entry _F8")]
        private static void Preview()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("Start a level in Play Mode before previewing water entry.");
                return;
            }
            GameplayController gameplay = Object.FindFirstObjectByType<GameplayController>();
            TankShelf shelf = Object.FindFirstObjectByType<TankShelf>();
            if (gameplay == null || shelf == null || !gameplay.IsInputEnabled) return;
            foreach (Fish fish in Object.FindObjectsByType<Fish>(FindObjectsSortMode.None))
            {
                if (!fish.IsSelectable || shelf.FindGoal(fish.TypeId) == null) continue;
                Unsubscribe();
                observed = Object.FindObjectsByType<TankWaterSplash>(FindObjectsSortMode.None);
                foreach (TankWaterSplash splash in observed) splash.WaterEntered += PauseOnSplash;
                EditorApplication.isPaused = false;
                gameplay.PickFish(fish);
                return;
            }
            Debug.Log("No selectable fish currently matches an open tank.");
        }

        private static void Unsubscribe()
        {
            if (observed == null) return;
            foreach (TankWaterSplash splash in observed) if (splash != null) splash.WaterEntered -= PauseOnSplash;
            observed = null;
        }

        private static void PauseOnSplash()
        {
            Unsubscribe();
            EditorApplication.isPaused = true;
        }
    }
}
#endif
