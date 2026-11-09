using UnityEngine;

namespace FishSort
{
    // Saved progress and settings (PlayerPrefs).
    public static class PlayerData
    {
        private const string KEY_LEVEL = "fishsort.level";
        private const string KEY_SOUND = "fishsort.sound";
        private const string KEY_PROBE_OFFSET = "fishsort.probeOffset";
        private const string KEY_HINT_COLORS = "fishsort.hintColors";
        private const int TRUE_VALUE = 1;
        private const int FALSE_VALUE = 0;

        public static int Level
        {
            get => Mathf.Max(FishSortConst.FIRST_LEVEL, PlayerPrefs.GetInt(KEY_LEVEL, FishSortConst.FIRST_LEVEL));
            set => SetInt(KEY_LEVEL, value);
        }

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(KEY_SOUND, TRUE_VALUE) == TRUE_VALUE;
            set => SetInt(KEY_SOUND, value ? TRUE_VALUE : FALSE_VALUE);
        }

        public static bool ProbeOffset
        {
            get => PlayerPrefs.GetInt(KEY_PROBE_OFFSET, FALSE_VALUE) == TRUE_VALUE;
            set => SetInt(KEY_PROBE_OFFSET, value ? TRUE_VALUE : FALSE_VALUE);
        }

        public static bool HintColors
        {
            get => PlayerPrefs.GetInt(KEY_HINT_COLORS, FALSE_VALUE) == TRUE_VALUE;
            set => SetInt(KEY_HINT_COLORS, value ? TRUE_VALUE : FALSE_VALUE);
        }

        private static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
    }
}
