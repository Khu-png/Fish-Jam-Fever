using System;
using System.Collections;
using UnityEngine;

namespace FishSort
{
    // Flight half of Fish: pick anticipation, one continuous path into a tank / the tray, tray slides.
    public sealed partial class Fish
    {
        private const float TRAY_SURFACE_OFFSET = 0.13f;
        private const float HALF = 0.5f;
        // A quadratic corner of this length (per unit radius) has the same speed as the straight parts around it.
        private const float CORNER_LENGTH_RATIO = 2f;
        private const float QUADRATIC_WEIGHT = 2f;
        private const float MIN_DIRECTION_SQR = 0.000001f;
        private const float MAX_PITCH_DEGREES = 80f;
        private const float FLIP_DEADZONE = 0.05f;
        private const float HORIZONTAL_EPSILON = 0.0001f;
        private const float TURN_SMOOTH_TIME = 0.08f;
        private const float PICK_GLOW_ALPHA = 0.65f;

        // Where the corner sits (above the water, towards the side the fish comes from) and how round it is.
        private readonly struct FlightShape
        {
            public readonly float apexHeight;
            public readonly float sideOffset;
            public readonly float cornerRadius;

            public FlightShape(float apexHeight, float sideOffset, float cornerRadius)
            {
                this.apexHeight = apexHeight;
                this.sideOffset = sideOffset;
                this.cornerRadius = cornerRadius;
            }
        }

        private static readonly FlightShape TankShape = new FlightShape(0.65f, 0.45f, 0.3f);
        private static readonly FlightShape TrayShape = new FlightShape(0.4f, 0.25f, 0.2f);

        public void FlyIntoTank(Func<Vector3> target, Func<Vector3> surface,
            float duration, float endWidth, Action onWaterEntry, Action<Fish> onArrive)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(WaterEntryRoutine(target, surface, duration, endWidth, onWaterEntry, onArrive,
                FishSortConst.SORT_FISH_IN_TANK, TankShape));
        }

        public void FlyIntoTray(Func<Vector3> target, float duration, float endScale,
            Action onWaterEntry, Action<Fish> onArrive)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(WaterEntryRoutine(target, () => target() + Vector3.up * TRAY_SURFACE_OFFSET,
                duration, endScale, onWaterEntry, onArrive, FishSortConst.SORT_FISH_IN_TRAY, TrayShape));
        }

        // Fishing rod: the fish (already out of its bubble) flies to a spot around the hook tip, outlined in white,
        // and then hangs from the tip.
        public void FlyToHook(Transform hookTip, Vector3 offset, float duration)
        {
            StopIdleMotion();
            StopMove();
            moveRoutine = StartCoroutine(HookRoutine(hookTip, offset, duration));
        }

        private IEnumerator HookRoutine(Transform hookTip, Vector3 offset, float duration)
        {
            IsFlying = true;
            ShowOutline(config.highlightColor);
            glowRenderer.maskInteraction = SpriteMaskInteraction.None;
            glowRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT + FishSortConst.SORT_GLOW_OFFSET;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            Vector3 start = TF.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float eased = Ease.InOutSine(Mathf.Clamp01(elapsed / duration));
                TF.position = Vector3.Lerp(start, hookTip.position + offset, eased);
                yield return null;
            }

            TF.SetParent(hookTip, true);
            TF.position = hookTip.position + offset;
            moveRoutine = null;
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

        // Reference-game path: a nearly straight climb to a point above the container, one small rounded
        // corner there, then a straight drop through the surface to the resting spot. The whole trip uses a
        // single ease, so the fish speeds up gently, never stops on the way, and glides to a halt at the end.
        private IEnumerator WaterEntryRoutine(Func<Vector3> target, Func<Vector3> surface,
            float duration, float endWidth, Action onWaterEntry, Action<Fish> onArrive,
            int arrivalSortingOrder, FlightShape shape)
        {
            IsFlying = true;
            float facing = FaceDestination(surface().x - TF.position.x);
            yield return AnticipatePick();
            SetTrail(true);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            Vector3 start = TF.position;
            float startWidth = TF.localScale.x;
            // The apex sits on the side the fish comes from, so the climb and the drop never overlap.
            Vector3 apexOffset = new Vector3(-facing * shape.sideOffset, shape.apexHeight, 0f);
            bool hasEntered = false;
            float enteredAt = 0f;
            float angleVelocity = 0f;
            Vector3 previous = start;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float eased = Ease.InOutSine(Mathf.Clamp01(elapsed / duration));
                Vector3 position = PathPoint(start, surface() + apexOffset, target(), shape.cornerRadius, eased);
                if (!hasEntered && position.y < previous.y && position.y <= surface().y)
                {
                    hasEntered = true;
                    enteredAt = eased;
                    EnterWater(arrivalSortingOrder, onWaterEntry);
                }

                float levelBlend = hasEntered ? Mathf.SmoothStep(0f, 1f, (eased - enteredAt) / (1f - enteredAt)) : 0f;
                SteerTowards(position - previous, facing, levelBlend, ref angleVelocity);
                TF.position = position;
                TF.localScale = Vector3.one * Mathf.Lerp(startWidth, endWidth, eased);
                previous = position;
                yield return null;
            }

            if (!hasEntered)
            {
                EnterWater(arrivalSortingOrder, onWaterEntry);
            }

            TF.position = target();
            TF.rotation = Quaternion.identity;
            IsFlying = false;
            moveRoutine = null;
            onArrive?.Invoke(this);
        }

        // Point at eased progress 0..1 on: straight line start -> corner entry, rounded corner around the apex,
        // straight line corner exit -> end. Each part gets a share of the trip proportional to its length; the
        // corner is a quadratic curve whose length is set so its speed matches the straight parts.
        private static Vector3 PathPoint(Vector3 start, Vector3 apex, Vector3 end, float cornerRadius, float progress)
        {
            float radius = Mathf.Min(cornerRadius, Vector3.Distance(start, apex) * HALF, Vector3.Distance(end, apex) * HALF);
            Vector3 cornerIn = apex + (start - apex).normalized * radius;
            Vector3 cornerOut = apex + (end - apex).normalized * radius;
            float climb = Vector3.Distance(start, cornerIn);
            float corner = radius * CORNER_LENGTH_RATIO;
            float drop = Vector3.Distance(cornerOut, end);
            float total = climb + corner + drop;
            float climbShare = climb / total;
            float cornerShare = corner / total;
            Vector3 point;
            if (progress < climbShare)
            {
                point = Vector3.Lerp(start, cornerIn, progress / climbShare);
            }
            else if (progress < climbShare + cornerShare)
            {
                point = QuadraticBezier(cornerIn, apex, cornerOut, (progress - climbShare) / cornerShare);
            }
            else
            {
                point = Vector3.Lerp(cornerOut, end, (progress - climbShare - cornerShare) / (1f - climbShare - cornerShare));
            }

            return point;
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + QUADRATIC_WEIGHT * u * t * b + t * t * c;
        }

        // The fish turns to face the side it is heading to (flipX, no negative scale). Sprites face left,
        // so facing left = -1 and right = +1. The facing is kept for the whole flight.
        private float FaceDestination(float horizontalDistance)
        {
            if (Mathf.Abs(horizontalDistance) > FLIP_DEADZONE)
            {
                bodyRenderer.flipX = horizontalDistance > 0f;
            }

            return bodyRenderer.flipX ? 1f : -1f;
        }

        // Swim head first: the nose follows the path (up while climbing, level in the glide, steeply down
        // while diving), then levels out over the last part of the dive as the fish settles.
        private void SteerTowards(Vector3 step, float facing, float levelBlend, ref float angleVelocity)
        {
            float targetAngle = 0f;
            if (step.sqrMagnitude > MIN_DIRECTION_SQR)
            {
                float pitch = Mathf.Atan2(step.y, Mathf.Abs(step.x) + HORIZONTAL_EPSILON) * Mathf.Rad2Deg;
                targetAngle = Mathf.Clamp(pitch, -MAX_PITCH_DEGREES, MAX_PITCH_DEGREES) * facing * (1f - levelBlend);
            }

            float angle = Mathf.SmoothDampAngle(TF.eulerAngles.z, targetAngle, ref angleVelocity, TURN_SMOOTH_TIME);
            TF.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Crossing the surface: become part of the container (sorting, mask), end the trail, tell the splash.
        private void EnterWater(int arrivalSortingOrder, Action onWaterEntry)
        {
            SetTrail(false);
            bodyRenderer.sortingOrder = arrivalSortingOrder;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            onWaterEntry?.Invoke();
        }

        // Tap feedback: quick scale punch with a fading outline before the fish leaves.
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
