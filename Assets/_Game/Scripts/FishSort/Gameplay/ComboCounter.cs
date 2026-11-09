using System.Collections;
using TMPro;
using UnityEngine;

namespace FishSort
{
    // Praise text (Nice! ... Perfect!) for fast consecutive correct picks. Tray picks reset the streak.
    public sealed class ComboCounter : MonoBehaviour
    {
        private const int NO_TIER = -1;
        private const float RESET_WINDOW_MULTIPLIER = 2f;

        [SerializeField] private TextMeshPro comboText;
        [SerializeField] private Transform comboTransform;
        [SerializeField] private FishSortConfig config;

        private Vector3 restPosition;
        private bool hasRestPosition;
        private Coroutine showRoutine;
        private float lastHitTime;

        public int Streak { get; private set; }

        public void ResetCombo()
        {
            Streak = 0;
            lastHitTime = -config.comboWindow * RESET_WINDOW_MULTIPLIER;
            StopAllCoroutines();
            showRoutine = null;
            CacheRest();
            comboText.gameObject.SetActive(false);
        }

        public void RegisterCorrect()
        {
            float now = Time.time;
            Streak = now - lastHitTime <= config.comboWindow ? Streak + 1 : 1;
            lastHitTime = now;
            int tier = TierFor(Streak);
            if (tier != NO_TIER)
            {
                if (showRoutine != null)
                {
                    StopCoroutine(showRoutine);
                }

                showRoutine = StartCoroutine(ShowRoutine(config.comboSteps[tier]));
            }
        }

        public void Break()
        {
            Streak = 0;
        }

        private int TierFor(int streak)
        {
            int tier = NO_TIER;
            ComboStep[] steps = config.comboSteps;
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i].streak == streak)
                {
                    tier = i;
                }
            }

            int lastStreak = steps[steps.Length - 1].streak;
            if (streak > lastStreak && (streak - lastStreak) % config.perfectRepeat == 0)
            {
                tier = steps.Length - 1;
            }

            return tier;
        }

        private void CacheRest()
        {
            if (!hasRestPosition)
            {
                restPosition = comboTransform.localPosition;
                hasRestPosition = true;
            }
        }

        private IEnumerator ShowRoutine(ComboStep step)
        {
            CacheRest();
            comboText.gameObject.SetActive(true);
            comboText.text = step.text;
            comboText.color = step.color;
            comboText.alpha = 1f;
            comboTransform.localPosition = restPosition;

            float elapsed = 0f;
            while (elapsed < config.comboPopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.OutBack(Mathf.Clamp01(elapsed / config.comboPopDuration));
                comboTransform.localScale = Vector3.one * (step.scale * t);
                yield return null;
            }

            comboTransform.localScale = Vector3.one * step.scale;
            yield return new WaitForSeconds(config.comboHoldDuration);

            elapsed = 0f;
            Vector3 risePosition = restPosition + Vector3.up * config.comboRise;
            while (elapsed < config.comboFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.comboFadeDuration);
                comboTransform.localPosition = Vector3.Lerp(restPosition, risePosition, t);
                comboText.alpha = 1f - t;
                yield return null;
            }

            comboText.gameObject.SetActive(false);
            showRoutine = null;
        }
    }
}
