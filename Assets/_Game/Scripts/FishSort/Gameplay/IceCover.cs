using System.Collections;
using UnityEngine;

namespace FishSort
{
    // Frozen layer on a bubble. Each fish picked from a touching bubble removes one tap.
    public sealed class IceCover : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer iceRenderer;
        [SerializeField] private SpriteRenderer numberRenderer;
        [SerializeField] private Transform numberTransform;
        [SerializeField] private Sprite[] numberSprites;
        [Tooltip("Twinkling stars on the ice (child IceSparkles); they stop when the ice melts.")]
        [SerializeField] private ParticleSystem sparkles;
        [SerializeField] private FishSortConfig config;

        private Transform cachedTransform;
        private int remainingTaps;

        public bool IsActive => remainingTaps > 0;

        // Lazy cache: the ice object can start inactive, so Awake is not guaranteed before Setup.
        private Transform TF
        {
            get
            {
                if (cachedTransform == null)
                {
                    cachedTransform = transform;
                }

                return cachedTransform;
            }
        }

        public void Setup(int taps)
        {
            StopAllCoroutines();
            remainingTaps = taps;
            gameObject.SetActive(taps > 0);
            TF.localScale = Vector3.one;
            iceRenderer.color = Color.white;
            numberRenderer.color = Color.white;
            numberTransform.localScale = Vector3.one;
            RefreshNumber();
            if (taps > 0)
            {
                sparkles.Play();
            }
        }

        // Returns true when this tap melted the ice.
        public bool Crack()
        {
            bool melted = false;
            if (IsActive)
            {
                remainingTaps--;
                melted = remainingTaps == 0;
                if (melted)
                {
                    sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    StartCoroutine(MeltRoutine());
                }
                else
                {
                    RefreshNumber();
                    StartCoroutine(PunchRoutine());
                }
            }

            return melted;
        }

        private void RefreshNumber()
        {
            if (remainingTaps > 0)
            {
                numberRenderer.sprite = numberSprites[Mathf.Min(remainingTaps, numberSprites.Length) - 1];
            }
        }

        private IEnumerator PunchRoutine()
        {
            float elapsed = 0f;
            while (elapsed < config.icePunchDuration)
            {
                elapsed += Time.deltaTime;
                float bump = Ease.Punch(Mathf.Clamp01(elapsed / config.icePunchDuration));
                numberTransform.localScale = Vector3.one * Mathf.Lerp(1f, config.icePunchScale, bump);
                yield return null;
            }

            numberTransform.localScale = Vector3.one;
        }

        private IEnumerator MeltRoutine()
        {
            float elapsed = 0f;
            Color clear = new Color(1f, 1f, 1f, 0f);
            while (elapsed < config.iceMeltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.iceMeltDuration);
                TF.localScale = Vector3.one * Mathf.Lerp(1f, config.bubblePopScale, t);
                iceRenderer.color = Color.Lerp(Color.white, clear, t);
                numberRenderer.color = iceRenderer.color;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
