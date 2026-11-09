using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FishSort
{
    // Visuals of one open tank slot: glass, 3 fish icon points, goal badge, complete/enter animations.
    public sealed class TankSlotView : MonoBehaviour
    {
        [SerializeField] private Transform tankRoot;
        [SerializeField] private SpriteRenderer flashRenderer;
        [SerializeField] private Transform[] iconPoints;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private Transform badgeTransform;
        [SerializeField] private SpriteRenderer badgeFishRenderer;
        [SerializeField] private TextMeshPro badgeCountText;
        [SerializeField] private FishSortConfig config;

        private readonly List<Fish> heldFish = new List<Fish>();
        private Vector3 tankRestPosition;
        private Coroutine badgeRoutine;
        private bool isInitialized;

        public Vector3 GetIconPosition(int iconIndex)
        {
            return iconPoints[iconIndex].position;
        }

        public void ResetView()
        {
            CacheRest();
            StopAllCoroutines();
            badgeRoutine = null;
            heldFish.Clear();
            tankRoot.localPosition = tankRestPosition;
            tankRoot.localScale = Vector3.one;
            badgeTransform.localScale = Vector3.one;
            SetFlash(0f);
        }

        public void PlaceFish(Fish fish, int iconIndex)
        {
            fish.AttachToTank(iconPoints[iconIndex], config.tankIconWidth);
            heldFish.Add(fish);
        }

        public void ShowBadge(Sprite fishSprite, int landed, int capacity, bool punch)
        {
            badgeRoot.SetActive(true);
            badgeFishRenderer.sprite = fishSprite;
            badgeCountText.text = string.Format(FishSortConst.FORMAT_PROGRESS, landed, capacity);
            if (punch)
            {
                if (badgeRoutine != null)
                {
                    StopCoroutine(badgeRoutine);
                }

                badgeRoutine = StartCoroutine(PunchBadge());
            }
        }

        public void HideBadge()
        {
            badgeRoot.SetActive(false);
        }

        // Flash, then shrink + rise out. Held fish are returned to the pool.
        public IEnumerator PlayComplete()
        {
            CacheRest();
            float elapsed = 0f;
            while (elapsed < config.tankFlashDuration)
            {
                elapsed += Time.deltaTime;
                SetFlash(Mathf.Clamp01(elapsed / config.tankFlashDuration));
                yield return null;
            }

            elapsed = 0f;
            Vector3 risePosition = tankRestPosition + Vector3.up * config.tankLeaveRise;
            while (elapsed < config.tankLeaveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.InQuad(Mathf.Clamp01(elapsed / config.tankLeaveDuration));
                tankRoot.localPosition = Vector3.Lerp(tankRestPosition, risePosition, t);
                tankRoot.localScale = Vector3.one * Mathf.Lerp(1f, config.tankLeaveScale, t);
                SetFlash(1f - t);
                yield return null;
            }

            for (int i = 0; i < heldFish.Count; i++)
            {
                heldFish[i].Despawn();
            }

            heldFish.Clear();
            tankRoot.localScale = Vector3.zero;
        }

        // New empty tank drops onto the shelf.
        public IEnumerator PlayEnter()
        {
            CacheRest();
            SetFlash(0f);
            Vector3 dropStart = tankRestPosition + Vector3.up * config.tankDropHeight;
            tankRoot.localScale = Vector3.one;
            float elapsed = 0f;
            while (elapsed < config.tankDropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.OutBack(Mathf.Clamp01(elapsed / config.tankDropDuration));
                tankRoot.localPosition = Vector3.LerpUnclamped(dropStart, tankRestPosition, t);
                yield return null;
            }

            tankRoot.localPosition = tankRestPosition;
        }

        private void CacheRest()
        {
            if (!isInitialized)
            {
                tankRestPosition = tankRoot.localPosition;
                isInitialized = true;
            }
        }

        private void SetFlash(float alpha)
        {
            flashRenderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private IEnumerator PunchBadge()
        {
            float elapsed = 0f;
            while (elapsed < config.badgePunchDuration)
            {
                elapsed += Time.deltaTime;
                float bump = Ease.Punch(Mathf.Clamp01(elapsed / config.badgePunchDuration));
                badgeTransform.localScale = Vector3.one * Mathf.Lerp(1f, config.badgePunchScale, bump);
                yield return null;
            }

            badgeTransform.localScale = Vector3.one;
            badgeRoutine = null;
        }
    }
}
