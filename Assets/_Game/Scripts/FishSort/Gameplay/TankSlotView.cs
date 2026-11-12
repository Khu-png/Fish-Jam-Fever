using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FishSort
{
    // Visuals of one open tank slot: glass, 3 fish icon points, goal badge, complete/enter animations.
    public sealed class TankSlotView : MonoBehaviour
    {
        // The pop effect is built for a bubble of diameter 1; a tank is about this much wider.
        private const float TANK_POP_SCALE = 1.2f;
        private const float BADGE_FISH_MAX_WIDTH = 0.54f;
        private const float BADGE_FISH_MAX_HEIGHT = 0.40f;
        private const float BADGE_FADE_TIME = 0.4f;
        // The hanging loops sit above the plate, so its visual center is below the sprite pivot.
        private static readonly Vector3 BadgeFishCenter = new Vector3(0f, -0.065f, 0f);

        [SerializeField] private Transform tankRoot;
        [SerializeField] private SpriteRenderer flashRenderer;
        [SerializeField] private Transform[] iconPoints;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private Transform badgeTransform;
        [SerializeField] private SpriteRenderer badgeFishRenderer;
        [SerializeField] private TextMeshPro badgeCountText;
        [SerializeField] private FishSortConfig config;
        [SerializeField] private TankWaterSplash waterSplash;

        private readonly List<Fish> heldFish = new List<Fish>();
        private Vector3 tankRestPosition;
        private Vector3 badgeRestScale = Vector3.one;
        private Vector3 countRestScale = Vector3.one;
        private Coroutine badgeRoutine;
        private Coroutine badgeFadeRoutine;
        private SpriteRenderer[] fadeSprites;
        private float[] fadeAlpha;
        private SpriteRenderer[] badgeSprites;
        private TextMeshPro[] badgeTexts;
        private float[] badgeSpriteAlpha;
        private float[] badgeTextAlpha;
        private bool isInitialized;

        private void Awake()
        {
            CacheRest();
        }

        // Fades the whole tank (its sprites and its badge) in or out: 0 = invisible, 1 = as drawn.
        public void SetFade(float factor)
        {
            if (fadeSprites == null)
            {
                fadeSprites = tankRoot.GetComponentsInChildren<SpriteRenderer>(true);
                fadeAlpha = new float[fadeSprites.Length];
                for (int i = 0; i < fadeSprites.Length; i++)
                {
                    fadeAlpha[i] = fadeSprites[i].color.a;
                }
            }

            for (int i = 0; i < fadeSprites.Length; i++)
            {
                Color color = fadeSprites[i].color;
                color.a = fadeAlpha[i] * factor;
                fadeSprites[i].color = color;
            }

            SetBadgeAlpha(factor);
        }

        // The area the tank covers in the world (all its sprites together).
        public Bounds GetTankBounds()
        {
            SpriteRenderer[] renderers = tankRoot.GetComponentsInChildren<SpriteRenderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        public Vector3 GetIconPosition(int iconIndex)
        {
            return iconPoints[iconIndex].position;
        }

        public Vector3 GetWaterPosition(int iconIndex)
        {
            float localX = tankRoot.InverseTransformPoint(iconPoints[iconIndex].position).x;
            return waterSplash.SurfacePosition(localX);
        }

        public void Splash(int iconIndex)
        {
            waterSplash.Play(GetWaterPosition(iconIndex));
        }

        public void ResetView()
        {
            CacheRest();
            StopAllCoroutines();
            badgeRoutine = null;
            badgeFadeRoutine = null;
            SetBadgeAlpha(1f);
            heldFish.Clear();
            waterSplash.Clear();
            tankRoot.localPosition = tankRestPosition;
            tankRoot.localScale = Vector3.one;
            badgeTransform.localScale = badgeRestScale;
            badgeCountText.transform.localScale = countRestScale;
            SetFlash(0f);
        }

        public void PlaceFish(Fish fish, int iconIndex)
        {
            fish.AttachToTank(iconPoints[iconIndex], config.tankIconWidth, config.tankFishHeight);
            heldFish.Add(fish);
        }

        public void ShowBadge(Sprite fishSprite, int landed, int capacity, bool punch)
        {
            StopBadgeFade();
            SetBadgeAlpha(1f);
            badgeRoot.SetActive(true);
            badgeFishRenderer.sprite = fishSprite;
            // Fit both dimensions, including tall species, and compensate for each sprite's pivot.
            Bounds fishBounds = fishSprite.bounds;
            float fishScale = Mathf.Min(BADGE_FISH_MAX_WIDTH / fishBounds.size.x,
                BADGE_FISH_MAX_HEIGHT / fishBounds.size.y);
            Transform fishTransform = badgeFishRenderer.transform;
            fishTransform.localScale = Vector3.one * fishScale;
            fishTransform.localRotation = Quaternion.identity;
            fishTransform.localPosition = BadgeFishCenter - fishBounds.center * fishScale;
            badgeCountText.text = string.Format(FishSortConst.FORMAT_PROGRESS, landed, capacity);
            if (badgeRoutine != null)
            {
                StopCoroutine(badgeRoutine);
                badgeRoutine = null;
            }
            badgeCountText.transform.localScale = countRestScale;
            if (punch)
            {
                badgeRoutine = StartCoroutine(PunchCount());
            }
        }

        public void HideBadge()
        {
            StopBadgeFade();
            SetBadgeAlpha(1f);
            badgeRoot.SetActive(false);
        }

        // The last goal of the level is done: the badge fades away instead of vanishing at once.
        public void FadeOutBadge()
        {
            if (badgeRoot.activeSelf)
            {
                StopBadgeFade();
                badgeFadeRoutine = StartCoroutine(FadeBadgeRoutine());
            }
        }

        private void StopBadgeFade()
        {
            if (badgeFadeRoutine != null)
            {
                StopCoroutine(badgeFadeRoutine);
                badgeFadeRoutine = null;
            }
        }

        private IEnumerator FadeBadgeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < BADGE_FADE_TIME)
            {
                elapsed += Time.deltaTime;
                SetBadgeAlpha(1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / BADGE_FADE_TIME)));
                yield return null;
            }

            badgeRoot.SetActive(false);
            SetBadgeAlpha(1f);
            badgeFadeRoutine = null;
        }

        // Every sprite and text of the badge, each keeping its own original opacity.
        private void SetBadgeAlpha(float factor)
        {
            if (badgeSprites == null)
            {
                badgeSprites = badgeRoot.GetComponentsInChildren<SpriteRenderer>(true);
                badgeTexts = badgeRoot.GetComponentsInChildren<TextMeshPro>(true);
                badgeSpriteAlpha = new float[badgeSprites.Length];
                badgeTextAlpha = new float[badgeTexts.Length];
                for (int i = 0; i < badgeSprites.Length; i++)
                {
                    badgeSpriteAlpha[i] = badgeSprites[i].color.a;
                }

                for (int i = 0; i < badgeTexts.Length; i++)
                {
                    badgeTextAlpha[i] = badgeTexts[i].alpha;
                }
            }

            for (int i = 0; i < badgeSprites.Length; i++)
            {
                Color color = badgeSprites[i].color;
                color.a = badgeSpriteAlpha[i] * factor;
                badgeSprites[i].color = color;
            }

            for (int i = 0; i < badgeTexts.Length; i++)
            {
                badgeTexts[i].alpha = badgeTextAlpha[i] * factor;
            }
        }

        // Flash, then shrink + rise out. Held fish are returned to the pool.
        public IEnumerator PlayComplete()
        {
            CacheRest();
            float elapsed = 0f;
            while (elapsed < config.tankFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / config.tankFlashDuration));
                SetFlash(t);
                tankRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.04f, t);
                yield return null;
            }

            // The tank pops away like a bubble, plus a burst of colourful bubbles and stars.
            ParticlePool.Play(ParticleType.TankBubblePop, tankRoot.position, Quaternion.identity, TANK_POP_SCALE);
            ParticlePool.Play(ParticleType.TankBubbleBurst, tankRoot.position, Quaternion.identity);
            elapsed = 0f;
            Vector3 risePosition = tankRestPosition + Vector3.up * config.tankLeaveRise;
            Vector3 leaveStartScale = tankRoot.localScale;
            while (elapsed < config.tankLeaveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.InQuad(Mathf.Clamp01(elapsed / config.tankLeaveDuration));
                tankRoot.localPosition = Vector3.Lerp(tankRestPosition, risePosition, t);
                tankRoot.localScale = Vector3.Lerp(leaveStartScale, Vector3.one * config.tankLeaveScale, t);
                SetFlash(1f - Mathf.SmoothStep(0f, 1f, t));
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
                badgeRestScale = badgeTransform.localScale;
                countRestScale = badgeCountText.transform.localScale;
                isInitialized = true;
            }
        }

        private void SetFlash(float alpha)
        {
            flashRenderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private IEnumerator PunchCount()
        {
            float elapsed = 0f;
            while (elapsed < config.badgePunchDuration)
            {
                elapsed += Time.deltaTime;
                float bump = Ease.Punch(Mathf.Clamp01(elapsed / config.badgePunchDuration));
                badgeCountText.transform.localScale = countRestScale * Mathf.Lerp(1f, config.badgePunchScale, bump);
                yield return null;
            }

            badgeCountText.transform.localScale = countRestScale;
            badgeRoutine = null;
        }
    }
}
