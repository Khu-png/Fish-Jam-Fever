using System;
using System.Collections;
using UnityEngine;

namespace FishSort
{
    public enum FishHighlight { None, Select, Good, Bad }

    // One pickable fish. Lives inside a Bubble, then flies to a tank or the tray.
    public sealed class Fish : GameUnit
    {
        private const float HALF_CHANCE = 0.5f;

        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private CircleCollider2D hitCollider;
        [SerializeField] private ParticleSystem flightTrail;
        [SerializeField] private FishSortConfig config;

        private Coroutine scaleRoutine;
        private Coroutine moveRoutine;
        private float restWidth;
        private Vector3 swimCenter;
        private float swimAngle;
        private float swimPhase;
        private Vector3 bodyRestScale;
        private bool idleInBubble;

        private void Awake()
        {
            bodyRestScale = bodyRenderer.transform.localScale;
            // Per-instance phases avoid synchronized movement without changing the level RNG.
            swimPhase = Mathf.Repeat(GetInstanceID() * 0.618034f, Mathf.PI * 2f);
        }

        private void LateUpdate()
        {
            if (!idleInBubble || Owner == null || IsFlying || IsFrozen) return;
            float t = Time.time * config.fishSwimSpeed + swimPhase;
            float strength = config.animateUnderwater ? 1f : 0f;
            Vector3 drift = new Vector3(Mathf.Sin(t * 0.73f) * config.fishSwimRadius,
                Mathf.Sin(t + swimPhase) * config.fishBobHeight, 0f) * strength;
            TF.localPosition = swimCenter + drift;
            TF.localRotation = Quaternion.Euler(0f, 0f, swimAngle + Mathf.Sin(t * 0.9f) * config.fishIdleTilt * strength);
            float breath = Mathf.Sin(t * 1.7f) * config.fishBreathing * strength;
            bodyRenderer.transform.localScale = Vector3.Scale(bodyRestScale, new Vector3(1f + breath, 1f - breath * 0.65f, 1f));
        }

        private void StopIdleMotion()
        {
            idleInBubble = false;
            bodyRenderer.transform.localScale = bodyRestScale;
        }

        public int TypeId { get; private set; }
        public Bubble Owner { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsSelectable => Owner != null && !IsFrozen;
        public Collider2D HitCollider => hitCollider;

        public void Setup(int typeId, Sprite sprite, Bubble owner, bool frozen)
        {
            StopAllCoroutines();
            StopIdleMotion();
            TypeId = typeId;
            Owner = owner;
            IsFlying = false;
            bodyRenderer.sprite = sprite;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_BUBBLE;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            glowRenderer.enabled = false;
            hitCollider.enabled = true;
            SetTrail(false);
            SetFrozen(frozen);
        }

        public void PlaceInBubble(Transform bubbleTransform, Vector2 localPosition, float localWidth, float angle)
        {
            TF.SetParent(bubbleTransform, false);
            TF.localPosition = localPosition;
            TF.localRotation = Quaternion.Euler(0f, 0f, angle);
            swimCenter = localPosition;
            swimAngle = angle;
            idleInBubble = true;
            restWidth = localWidth;
            TF.localScale = Vector3.one * localWidth;
        }

        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;
            bodyRenderer.color = frozen ? config.frozenFishTint : Color.white;
        }

        public void SetHighlight(FishHighlight highlight)
        {
            bool isOn = highlight != FishHighlight.None;
            glowRenderer.enabled = isOn;
            glowRenderer.color = HighlightColor(highlight);
            bodyRenderer.sortingOrder = isOn ? FishSortConst.SORT_FISH_HIGHLIGHT : FishSortConst.SORT_FISH_IN_BUBBLE;
            glowRenderer.sortingOrder = bodyRenderer.sortingOrder + FishSortConst.SORT_GLOW_OFFSET;
            float targetWidth = restWidth * (isOn ? config.highlightScale : 1f);
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
            }

            scaleRoutine = StartCoroutine(ScaleRoutine(targetWidth, config.highlightTweenDuration));
        }

        // Leaves the bubble: world position and size are kept, physics/mask no longer apply.
        public void Detach(Transform flightRoot)
        {
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
                scaleRoutine = null;
            }

            StopIdleMotion();
            Owner = null;
            hitCollider.enabled = false;
            glowRenderer.enabled = false;
            TF.SetParent(flightRoot, true);
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
        }

        public void FlyTo(Func<Vector3> target, float duration, float endWidth, Action<Fish> onArrive,
            int arrivalSortingOrder = FishSortConst.SORT_FISH_FLIGHT)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(FlyRoutine(target, duration, endWidth, onArrive, arrivalSortingOrder));
        }

        public void FlyIntoTank(System.Func<Vector3> target, System.Func<Vector3> surface,
            float duration, float endWidth, System.Action onWaterEntry, System.Action<Fish> onArrive)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(TankEntryRoutine(target, surface, duration, endWidth, onWaterEntry, onArrive));
        }

        // Short slide used when tray items shift left.
        public void SlideTo(Vector3 position, float duration)
        {
            if (IsFlying)
            {
                return;
            }

            StopMove();
            moveRoutine = StartCoroutine(SlideRoutine(position, duration));
        }

        // Fit actual sprite bounds, so tall species also stay inside the tray glass.
        public float ScaleToFit(float maxWidth, float maxHeight)
        {
            Vector3 size = bodyRenderer.sprite.bounds.size;
            Vector3 bodyScale = bodyRenderer.transform.localScale;
            return Mathf.Min(maxWidth / (size.x * Mathf.Abs(bodyScale.x)),
                maxHeight / (size.y * Mathf.Abs(bodyScale.y)));
        }

        public void RestInTray()
        {
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_TRAY;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        public void AttachToTank(Transform iconPoint, float width)
        {
            StopMove();
            TF.SetParent(iconPoint, false);
            TF.localPosition = Vector3.zero;
            TF.localRotation = Quaternion.identity;
            TF.localScale = Vector3.one * (width / iconPoint.lossyScale.x);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_TANK;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        public void Despawn()
        {
            StopIdleMotion();
            StopAllCoroutines();
            IsFlying = false;
            SetTrail(false);
            HBPool.Despawn(this);
        }

        private void StopMove()
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
        }

        private IEnumerator FlyRoutine(Func<Vector3> target, float duration, float endWidth, Action<Fish> onArrive, int arrivalSortingOrder)
        {
            IsFlying = true;
            SetTrail(true);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            Vector3 start = TF.position;
            float startWidth = TF.localScale.x;
            float side = UnityEngine.Random.value < HALF_CHANCE ? -1f : 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float e = Ease.InOutCubic(t);
                if (t >= 0.85f)
                {
                    bodyRenderer.sortingOrder = arrivalSortingOrder;
                    if (arrivalSortingOrder == FishSortConst.SORT_FISH_IN_TRAY)
                        bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
                Vector3 end = target();
                Vector3 control = new Vector3(
                    start.x + side * config.flySideOffset,
                    start.y + (end.y - start.y) * config.flyArcRatio + config.flyArcLift,
                    start.z);
                float u = 1f - e;
                TF.position = u * u * start + 2f * u * e * control + e * e * end;
                TF.localScale = Vector3.one * Mathf.Lerp(startWidth, endWidth, e);
                TF.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * side * -config.flyTiltDegrees);
                yield return null;
            }

            TF.position = target();
            TF.rotation = Quaternion.identity;
            IsFlying = false;
            SetTrail(false);
            moveRoutine = null;
            onArrive?.Invoke(this);
        }

        private IEnumerator TankEntryRoutine(System.Func<Vector3> target, System.Func<Vector3> surface,
            float duration, float endWidth, System.Action onWaterEntry, System.Action<Fish> onArrive)
        {
            IsFlying = true;
            SetTrail(true);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            Vector3 start = TF.position;
            float startWidth = TF.localScale.x;
            float airDuration = duration * 0.68f;
            float elapsed = 0f;
            Vector3 previous = start;
            while (elapsed < airDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.OutQuad(Mathf.Clamp01(elapsed / airDuration));
                Vector3 water = surface();
                Vector3 controlA = start + Vector3.up * Mathf.Max(0.6f, (water.y - start.y) * 0.5f);
                Vector3 controlB = water + Vector3.up * 0.85f;
                float u = 1f - t;
                Vector3 position = u * u * u * start + 3f * u * u * t * controlA + 3f * u * t * t * controlB + t * t * t * water;
                Vector3 direction = position - previous;
                if (direction.sqrMagnitude > 0.000001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 180f;
                    TF.rotation = Quaternion.Euler(0f, 0f, Mathf.MoveTowardsAngle(TF.eulerAngles.z, angle, 900f * Time.deltaTime));
                }
                TF.position = position;
                TF.localScale = Vector3.one * Mathf.Lerp(startWidth, endWidth, t * 0.65f);
                previous = position;
                yield return null;
            }

            // The splash is synchronized to crossing the surface, before settling on the tank floor.
            Vector3 entry = surface();
            TF.position = entry;
            SetTrail(false);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_TANK;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            onWaterEntry?.Invoke();
            float entryAngle = TF.eulerAngles.z;
            float entryWidth = TF.localScale.x;
            float diveDuration = Mathf.Max(0.08f, duration - airDuration);
            elapsed = 0f;
            while (elapsed < diveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.OutQuad(Mathf.Clamp01(elapsed / diveDuration));
                TF.position = Vector3.Lerp(entry, target(), t);
                TF.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(entryAngle, 0f, t));
                TF.localScale = Vector3.one * Mathf.Lerp(entryWidth, endWidth, t);
                yield return null;
            }
            TF.position = target();
            TF.rotation = Quaternion.identity;
            IsFlying = false;
            moveRoutine = null;
            onArrive?.Invoke(this);
        }

        private IEnumerator SlideRoutine(Vector3 position, float duration)
        {
            Vector3 start = TF.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                TF.position = Vector3.Lerp(start, position, Ease.OutQuad(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            TF.position = position;
            moveRoutine = null;
        }

        private IEnumerator ScaleRoutine(float targetWidth, float duration)
        {
            float startWidth = TF.localScale.x;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                TF.localScale = Vector3.one * Mathf.Lerp(startWidth, targetWidth, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            TF.localScale = Vector3.one * targetWidth;
            scaleRoutine = null;
        }

        private void SetTrail(bool isOn)
        {
            ParticleSystem.EmissionModule emission = flightTrail.emission;
            emission.enabled = isOn;
        }

        private Color HighlightColor(FishHighlight highlight)
        {
            Color color = config.highlightColor;
            if (highlight == FishHighlight.Good)
            {
                color = config.goodHintColor;
            }
            else if (highlight == FishHighlight.Bad)
            {
                color = config.badHintColor;
            }

            return color;
        }
    }
}
