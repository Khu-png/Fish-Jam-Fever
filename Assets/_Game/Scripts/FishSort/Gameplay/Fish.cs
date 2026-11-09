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

        public int TypeId { get; private set; }
        public Bubble Owner { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsSelectable => Owner != null && !IsFrozen;
        public Collider2D HitCollider => hitCollider;

        public void Setup(int typeId, Sprite sprite, Bubble owner, bool frozen)
        {
            StopAllCoroutines();
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

            Owner = null;
            hitCollider.enabled = false;
            glowRenderer.enabled = false;
            TF.SetParent(flightRoot, true);
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
        }

        public void FlyTo(Func<Vector3> target, float duration, float endWidth, Action<Fish> onArrive)
        {
            StopMove();
            moveRoutine = StartCoroutine(FlyRoutine(target, duration, endWidth, onArrive));
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

        public void RestInTray()
        {
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_TRAY;
        }

        public void AttachToTank(Transform iconPoint, float width)
        {
            StopMove();
            TF.SetParent(iconPoint, false);
            TF.localPosition = Vector3.zero;
            TF.localRotation = Quaternion.identity;
            TF.localScale = Vector3.one * (width / iconPoint.lossyScale.x);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_TANK;
        }

        public void Despawn()
        {
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

        private IEnumerator FlyRoutine(Func<Vector3> target, float duration, float endWidth, Action<Fish> onArrive)
        {
            IsFlying = true;
            SetTrail(true);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            Vector3 start = TF.position;
            float startWidth = TF.localScale.x;
            float side = UnityEngine.Random.value < HALF_CHANCE ? -1f : 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float e = Ease.InOutCubic(t);
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
