#if UNITY_EDITOR
using FishSort;
using UnityEditor;
using UnityEngine;

namespace FishSort.Editor
{
    // Editor shortcut: sets the saved level back to level 1 (other settings are kept).
    public static class ResetProgressTool
    {
        private const string MENU_RESET_LEVEL = "Tools/FishSort/Reset Level To 1";
        private const string LOG_DONE = "[FishSort] Saved level reset to {0}.";
        private const string LOG_PLAYING = "[FishSort] Saved level reset to {0}. Go back to Home to see it on the Play button.";

        [MenuItem(MENU_RESET_LEVEL)]
        private static void ResetLevelToFirst()
        {
            PlayerData.Level = FishSortConst.FIRST_LEVEL;
            Debug.Log(string.Format(EditorApplication.isPlaying ? LOG_PLAYING : LOG_DONE, FishSortConst.FIRST_LEVEL));
        }
    }
}
#endif
