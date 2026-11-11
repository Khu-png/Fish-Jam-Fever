using System;
using System.Collections;
using UnityEngine;

namespace FishSort
{
    // Flight half of Fish: pick anticipation, arcs into tanks / tray, tray slides.
    public sealed partial class Fish
    {
        private const float HALF_CHANCE = 0.5f;
        private const float TRAY_SURFACE_OFFSET = 0.13f;
        private const float TRAY_ENTRY_ARC_HEIGHT = 0.35f;
        private const float TANK_ENTRY_ARC_HEIGHT = 0.65f;
        private const float ARRIVAL_SORT_PROGRESS = 0.85f;
        private const float AIR_TIME_RATIO = 0.62f;
        private const float MIN_TAKEOFF_LIFT = 0.6f;
        private const float TAKEOFF_LIFT_RATIO = 0.5f;
        private const float MIN_DIRECTION_SQR = 0.000001f;
        private const float FACE_LEFT_DEGREES = 180f;
        private const float MAX_DIVE_TILT = 55f;
        private const float TURN_SPEED_DEGREES = 280f;
        private const float AIR_SCALE_RATIO = 0.65f;
        private const float MIN_DIVE_DURATION = 0.08f;
        private const float PICK_GLOW_ALPHA = 0.65f;
        private const float QUADRATIC_WEIGHT = 2f;
        private const float CUBIC_WEIGHT = 3f;

        public void FlyTo(Func<Vector3> target, float duration, float endWidth, Action<Fish> onArrive,
            int arrivalSortingOrder = FishSortConst.SORT_FISH_FLIGHT)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(FlyRoutine(target, duration, endWidth, onArrive, arrivalSortingOrder));
        }

        public void FlyIntoTank(Func<Vector3> target, Func<Vector3> surface,
            float duration, float endWidth, Action onWaterEntry, Action<Fish> onArrive)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(WaterEntryRoutine(target, surface, duration, endWidth, onWaterEntry, onArrive,
                FishSortConst.SORT_FISH_IN_TANK, TANK_ENTRY_ARC_HEIGHT));
        }

        public void FlyIntoTray(Func<Vector3> target, float duration, float endScale,
            Action onWaterEntry, Action<Fish> onArrive)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(WaterEntryRoutine(target, () => target() + Vector3.up * TRAY_SURFACE_OFFSET,
                duration, endScale, onWaterEntry, onArrive, FishSortConst.SORT_FISH_IN_TRAY, TRAY_ENTRY_ARC_HEIGHT));
        }

        // Short slide used when tray items shift left.
        public void SlideTo(Vector3 position, float duration)
        {
            if (!IsFlying)
            {
                StopMove();
                moveRoutine = StartCoroutine(SlideRoutine(position, duration));
            }
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
            yield return AnticipatePick();
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
                if (t >= ARRIVAL_SORT_PROGRESS)
                {
                    bodyRenderer.sortingOrder = arrivalSortingOrder;
                    if (arrivalSortingOrder == FishSortConst.SORT_FISH_IN_TRAY)
                    {
                        bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    }
                }

                Vector3 end = target();
                Vector3 control = new Vector3(
                    start.x + side * config.flySideOffset,
                    start.y + (end.y - start.y) * config.flyArcRatio + config.flyArcLift,
                    start.z);
                float u = 1f - e;
                TF.position = u * u * start + QUADRATIC_WEIGHT * u * e * control + e * e * end;
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

        // Arc to the water surface (splash there), then dive to the resting spot inside the container.
        private IEnumerator WaterEntryRoutine(Func<Vector3> target, Func<Vector3> surface,
            float duration, float endWidth, Action onWaterEntry, Action<Fish> onArrive,
            int arrivalSortingOrder, float entryArcHeight)
        {
            IsFlying = true;
            yield return AnticipatePick();
            SetTrail(true);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            Vector3 start = TF.position;
            float startWidth = TF.localScale.x;
            float airDuration = duration * AIR_TIME_RATIO;
            float elapsed = 0f;
            Vector3 previous = start;
            while (elapsed < airDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.InOutCubic(Mathf.Clamp01(elapsed / airDuration));
                Vector3 water = surface();
                Vector3 controlA = start + Vector3.up * Mathf.Max(MIN_TAKEOFF_LIFT, (water.y - start.y) * TAKEOFF_LIFT_RATIO);
                Vector3 controlB = water + Vector3.up * entryArcHeight;
                float u = 1f - t;
                Vector3 position = u * u * u * start + CUBIC_WEIGHT * u * u * t * controlA
                    + CUBIC_WEIGHT * u * t * t * controlB + t * t * t * water;
                Vector3 direction = position - previous;
                if (direction.sqrMagnitude > MIN_DIRECTION_SQR)
                {
                    float angle = Mathf.DeltaAngle(0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - FACE_LEFT_DEGREES);
                    angle = Mathf.Clamp(angle, -MAX_DIVE_TILT, MAX_DIVE_TILT);
                    TF.rotation = Quaternion.Euler(0f, 0f,
                        Mathf.MoveTowardsAngle(TF.eulerAngles.z, angle, TURN_SPEED_DEGREES * Time.deltaTime));
                }

                TF.position = position;
                TF.localScale = Vector3.one * Mathf.Lerp(startWidth, endWidth, t * AIR_SCALE_RATIO);
                previous = position;
                yield return null;
            }

            // The splash is synchronized to crossing the surface, before settling inside.
            Vector3 entry = surface();
            TF.position = entry;
            SetTrail(false);
            bodyRenderer.sortingOrder = arrivalSortingOrder;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            onWaterEntry?.Invoke();
            float entryAngle = TF.eulerAngles.z;
            float entryWidth = TF.localScale.x;
            float diveDuration = Mathf.Max(MIN_DIVE_DURATION, duration - airDuration);
            elapsed = 0f;
            while (elapsed < diveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.InOutCubic(Mathf.Clamp01(elapsed / diveDuration));
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

        // Tap feedback: quick scale punch with a fading glow before the fish leaves.
        private IEnumerator AnticipatePick()
        {
            Vector3 startScale = TF.localScale;
            ShowOutline(config.highlightColor);
            glowRenderer.maskInteraction = SpriteMaskInteraction.None;
            glowRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT + FishSortConst.SORT_GLOW_OFFSET;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            float elapsed = 0f;
            while (elapsed < config.pickAnticipationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.pickAnticipationDuration);
                TF.localScale = startScale * Mathf.Lerp(1f, config.pickPopScale, Ease.Punch(t));
                Color color = config.highlightColor;
                color.a = (1f - t) * PICK_GLOW_ALPHA;
                glowRenderer.color = color;
                yield return null;
            }

            TF.localScale = startScale;
            glowRenderer.enabled = false;
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
    }
}
