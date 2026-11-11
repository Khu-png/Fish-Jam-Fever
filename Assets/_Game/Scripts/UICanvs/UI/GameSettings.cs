using System;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>
    /// Cai dat nguoi choi (luu PlayerPrefs, dung chung moi scene).
    /// Am thanh / nhac doc GameSettings.Music, GameSettings.Sound va nghe GameSettings.Changed.
    /// </summary>
    public static class GameSettings
    {
        const string KMusic = "set_music", KSound = "set_sound", KVibe = "set_vibration";
        public static event Action Changed;

        public static bool Music { get => Get(KMusic); set => Set(KMusic, value); }
        public static bool Sound { get => Get(KSound); set => Set(KSound, value); }
        public static bool Vibration { get => Get(KVibe); set => Set(KVibe, value); }

        /// <summary>Rung nhe (chi khi bat Vibration, tren dien thoai).</summary>
        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Vibration) Handheld.Vibrate();
#endif
        }

        static bool Get(string k) => PlayerPrefs.GetInt(k, 1) == 1;
        static void Set(string k, bool v)
        {
            if (Get(k) == v) return;
            PlayerPrefs.SetInt(k, v ? 1 : 0); PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
