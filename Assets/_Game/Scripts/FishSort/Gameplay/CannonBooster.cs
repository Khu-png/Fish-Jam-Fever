using System;
using System.Collections;
using UnityEngine;

namespace FishSort
{
    // Visual of the cannon booster: the cannon pops in at the left end of the tray in a puff of smoke, fires a
    // glowing blast along the tray, kicks back and leaves. GameplayController drives it through the coroutines
    // (so they stop with the level) and uses the callback of Fire to pick up the tray fish as the blast passes.
    public sealed class CannonBooster : MonoBehaviour
    {
        private const float ENTER_FROM_X = -1.4f;
        private const float ENTER_FROM_Y = -0.9f;
        private const float RECOIL_DISTANCE = 0.15f;
        private const float ORB_START_SCALE = 0.5f;
        private const float ORB_GROW_PER_FISH = 0.05f;
        private const float ORB_PULSE_SPEED = 18f;
        private const float ORB_PULSE_AMOUNT = 0.08f;

        [SerializeField] private Transform body;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform orb;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private ParticleSystem trail;
        [SerializeField] private FishSortConfig config;

        private Vector3 restPosition;
        private float orbScale;
        private bool isOrbFlying;

        public Transform Orb => orb;
        public Vector3 MuzzlePosition => muzzle.position;

        public void Hide()
        {
            isOrbFlying = false;
            orb.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        // The orb gets bigger with every fish it swallows.
        public void GrowOrb()
        {
            orbScale += ORB_GROW_PER_FISH;
        }

        public IEnumerator Enter(Vector3 restPoint)
        {
            restPosition = restPoint;
            gameObject.SetActive(true);
            orb.gameObject.SetActive(false);
            Vector3 from = restPoint + new Vector3(ENTER_FROM_X, ENTER_FROM_Y, 0f);
            body.position = from;
            smoke.Play();
            float elapsed = 0f;
            while (elapsed < config.cannonEnterDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.cannonEnterDuration);
                body.position = Vector3.LerpUnclamped(from, restPosition, Ease.OutBack(t));
                yield return null;
            }

            body.position = restPosition;
        }

        // The blast leaves the muzzle and sweeps to endPoint; onMove gets the orb position every frame.
        public IEnumerator Fire(Vector3 endPoint, Action<Vector3> onMove)
        {
            Vector3 from = muzzle.position;
            orbScale = ORB_START_SCALE;
            orb.position = from;
            orb.gameObject.SetActive(true);
            isOrbFlying = true;
            trail.Play();
            float elapsed = 0f;
            while (elapsed < config.cannonBlastDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.cannonBlastDuration);
                orb.position = Vector3.Lerp(from, endPoint, Ease.InQuad(t));
                // The cannon kicks back as it fires and settles again.
                body.position = restPosition + Vector3.left * (Ease.Punch(Mathf.Min(1f, t * 3f)) * RECOIL_DISTANCE);
                onMove(orb.position);
                yield return null;
            }

            orb.position = endPoint;
            onMove(endPoint);
            body.position = restPosition;
            isOrbFlying = false;
            orb.gameObject.SetActive(false);
            trail.Stop();
        }

        // Leaves the way it came, in a puff of smoke.
        public IEnumerator Retreat()
        {
            Vector3 to = restPosition + new Vector3(ENTER_FROM_X, 0f, 0f);
            smoke.Play();
            float elapsed = 0f;
            while (elapsed < config.cannonRetreatDuration)
            {
                elapsed += Time.deltaTime;
                float t = Ease.InQuad(Mathf.Clamp01(elapsed / config.cannonRetreatDuration));
                body.position = Vector3.Lerp(restPosition, to, t);
                yield return null;
            }

            Hide();
        }

        private void Update()
        {
            if (isOrbFlying)
            {
                float pulse = 1f + Mathf.Sin(Time.time * ORB_PULSE_SPEED) * ORB_PULSE_AMOUNT;
                orb.localScale = Vector3.one * (orbScale * pulse);
            }
        }
    }
}
