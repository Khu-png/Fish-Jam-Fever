using System;
using UnityEngine;

namespace FishSort
{
    // The power-ups of the game, in the order of the booster buttons.
    public enum BoosterKind { Rod = 0, Cannon = 1 }

    // Saved progress and settings (PlayerPrefs).
    public static class PlayerData
    {
        private const string KEY_LEVEL = "fishsort.level";
        private const string KEY_SOUND = "fishsort.sound";
        private const string KEY_PROBE_OFFSET = "fishsort.probeOffset";
        private const string KEY_HINT_COLORS = "fishsort.hintColors";
        private const string KEY_COINS = "fishsort.coins";
        // The key the old Wallet used; its saved coins are picked up until the new key is written.
        private const string LEGACY_KEY_COINS = "coins";
        private const int START_COINS = 820;
        private const string KEY_BOOSTER = "fishsort.booster.";
        private const int TRUE_VALUE = 1;
        private const int FALSE_VALUE = 0;

        public static int Level
        {
            get => Mathf.Max(FishSortConst.FIRST_LEVEL, PlayerPrefs.GetInt(KEY_LEVEL, FishSortConst.FIRST_LEVEL));
            set => SetInt(KEY_LEVEL, value);
        }

        // Fired with the new total whenever the coins change.
        public static event Action<int> CoinsChanged;

        public static int Coins => PlayerPrefs.GetInt(KEY_COINS, PlayerPrefs.GetInt(LEGACY_KEY_COINS, START_COINS));

        public static void AddCoins(int amount)
        {
            SetInt(KEY_COINS, Mathf.Max(0, Coins + amount));
            CoinsChanged?.Invoke(Coins);
        }

        // Takes the coins if there are enough; false (nothing taken) otherwise.
        public static bool SpendCoins(int amount)
        {
            bool canPay = Coins >= amount;
            if (canPay)
            {
                AddCoins(-amount);
            }

            return canPay;
        }

        // Power-ups the player owns, by kind (see BoosterKind). Fired whenever any stock changes.
        public static event Action BoostersChanged;

        public static int GetBooster(BoosterKind kind)
        {
            return PlayerPrefs.GetInt(BoosterKey(kind), 0);
        }

        public static void AddBooster(BoosterKind kind, int amount)
        {
            SetInt(BoosterKey(kind), Mathf.Max(0, GetBooster(kind) + amount));
            BoostersChanged?.Invoke();
        }

        // Uses one if there is one; false (nothing used) otherwise.
        public static bool UseBooster(BoosterKind kind)
        {
            bool hasOne = GetBooster(kind) > 0;
            if (hasOne)
            {
                AddBooster(kind, -1);
            }

            return hasOne;
        }

        private static string BoosterKey(BoosterKind kind)
        {
            return KEY_BOOSTER + (int)kind;
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
