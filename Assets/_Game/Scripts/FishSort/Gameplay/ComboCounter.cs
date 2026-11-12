using UnityEngine;

namespace FishSort
{
    // Counts correct picks in a row, only for the rising pitch of the pick sound. The praise text and voice (Nice! ...
    // Perfect!) belong to the tanks completed in a row and are handled by the HUD (GamePlay.OnTankCompleted).
    public sealed class ComboCounter : MonoBehaviour
    {
        private const float RESET_WINDOW_MULTIPLIER = 2f;

        [SerializeField] private FishSortConfig config;

        private float lastHitTime;

        public int Streak { get; private set; }

        public void ResetCombo()
        {
            Streak = 0;
            lastHitTime = -config.comboWindow * RESET_WINDOW_MULTIPLIER;
            if (UIManager.Ins.IsLoaded<FishJam.UI.GamePlay>())
            {
                UIManager.Ins.GetUI<FishJam.UI.GamePlay>().HidePraise();
            }
        }

        public void RegisterCorrect()
        {
            float now = Time.time;
            Streak = now - lastHitTime <= config.comboWindow ? Streak + 1 : 1;
            lastHitTime = now;
        }

        public void Break()
        {
            Streak = 0;
        }
    }
}
