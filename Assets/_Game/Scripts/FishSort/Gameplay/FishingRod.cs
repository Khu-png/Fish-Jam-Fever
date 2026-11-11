using System;
using System.Collections;
using UnityEngine;

namespace FishSort
{
    // Visual of the fishing-rod booster: a line hangs straight down from above the screen, the hook comes down to
    // a gathering point, the caught fish hang around its tip, and the whole thing is lifted to a tank.
    // Movement is driven by GameplayController through the coroutines below (so they stop with the level).
    public sealed class FishingRod : MonoBehaviour
    {
        private const int LINE_POINTS = 14;
        private const float ANCHOR_LAG = 7f;
        private const float TILT_DEGREES = 9f;
        private const float TILT_FREQUENCY = 2.4f;
        private const float WAVE_AMOUNT = 0.05f;
        private const float WAVE_SPEED = 6f;
        private const float WAVES = 3f;
        private const float HOOK_PUNCH_SCALE = 1.25f;

        [SerializeField] private Transform hook;
        [SerializeField] private Transform tip;
        [SerializeField] private LineRenderer line;
        [SerializeField] private FishSortConfig config;

        private float topY;
        private float anchorX;
        private bool isShown;

        public Transform Tip => tip;

        // Hook-top position at which the tip is exactly on the given point.
        public Vector3 HookTopFor(Vector3 tipPoint)
        {
            return tipPoint - tip.localPosition;
        }

        public void Hide()
        {
            isShown = false;
            gameObject.SetActive(false);
        }

        // The hook comes straight down from above the screen to the point where the fish will gather.
        public IEnumerator Enter(float screenTopY, Vector3 tipPoint)
        {
            topY = screenTopY;
            Vector3 destination = HookTopFor(tipPoint);
            Vector3 from = new Vector3(destination.x, topY, 0f);
            gameObject.SetActive(true);
            isShown = true;
            hook.position = from;
            anchorX = from.x;
            yield return Move(from, destination, config.rodDropDuration, Ease.OutCubic);
        }

        // Carries the hook (and the fish hanging from it) so that the tip ends on tipPoint.
        public IEnumerator MoveTo(Vector3 tipPoint, float duration)
        {
            yield return Move(hook.position, HookTopFor(tipPoint), duration, Ease.InOutSine);
        }

        // A short pop on the hook when the fish have gathered.
        public IEnumerator Catch()
        {
            float elapsed = 0f;
            while (elapsed < config.rodHookPause)
            {
                elapsed += Time.deltaTime;
                float bump = Ease.Punch(Mathf.Clamp01(elapsed / config.rodHookPause));
                hook.localScale = Vector3.one * Mathf.Lerp(1f, HOOK_PUNCH_SCALE, bump);
                yield return null;
            }

            hook.localScale = Vector3.one;
        }

        // The fish have been let go: the empty hook is reeled straight back up and out of sight.
        public IEnumerator Retract()
        {
            Vector3 from = hook.position;
            Vector3 to = new Vector3(from.x, topY, from.z);
            yield return Move(from, to, config.rodRetractDuration, Ease.InQuad);
            Hide();
        }

        private IEnumerator Move(Vector3 from, Vector3 to, float duration, Func<float, float> ease)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                hook.position = Vector3.Lerp(from, to, ease(t));
                float tilt = Mathf.Sin(t * Mathf.PI * TILT_FREQUENCY) * (1f - t) * Mathf.Sign(to.x - from.x);
                hook.rotation = Quaternion.Euler(0f, 0f, tilt * TILT_DEGREES);
                yield return null;
            }

            hook.position = to;
            hook.rotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (isShown)
            {
                // The top of the line trails the hook sideways a little, so the line bows when the hook moves.
                anchorX = Mathf.Lerp(anchorX, hook.position.x, 1f - Mathf.Exp(-ANCHOR_LAG * Time.deltaTime));
                DrawLine();
            }
        }

        private void DrawLine()
        {
            Vector3 top = new Vector3(anchorX, topY, 0f);
            Vector3 end = hook.position;
            for (int i = 0; i < LINE_POINTS; i++)
            {
                float u = i / (LINE_POINTS - 1f);
                float wave = Mathf.Sin(Time.time * WAVE_SPEED - u * WAVES) * WAVE_AMOUNT * Mathf.Sin(u * Mathf.PI);
                line.SetPosition(i, Vector3.Lerp(top, end, u) + Vector3.right * wave);
            }
        }
    }
}
