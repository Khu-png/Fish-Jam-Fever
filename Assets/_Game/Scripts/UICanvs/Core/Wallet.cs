using System;
using UnityEngine;

namespace FishJam
{
    /// <summary>So xu cua nguoi choi (PlayerPrefs "coins", mac dinh 820, chi ap dung khi chua co du lieu luu). UI nghe Wallet.Changed.</summary>
    public static class Wallet
    {
        const string Key = "coins";
        public static event Action<int> Changed;
        public static int Coins => PlayerPrefs.GetInt(Key, 820);

        public static void Add(int n)
        {
            PlayerPrefs.SetInt(Key, Mathf.Max(0, Coins + n)); PlayerPrefs.Save();
            Changed?.Invoke(Coins);
        }

        /// <summary>Tru xu neu du. Tra ve false neu khong du.</summary>
        public static bool Spend(int n)
        {
            if (Coins < n) return false;
            Add(-n); return true;
        }
    }
}
