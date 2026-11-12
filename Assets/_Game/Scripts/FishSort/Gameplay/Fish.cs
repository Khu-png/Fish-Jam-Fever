using System.Collections;
using UnityEngine;

namespace FishSort
{
    public enum FishHighlight { None, Select, Good, Bad }

    // One pickable fish. Lives inside a Bubble, then flies to a tank or the tray (see FishFlight.cs).
    public sealed partial class Fish : GameUnit
    {
        private const float PHASE_SPREAD = 0.618034f;
        private const float FULL_TURN_RADIANS = Mathf.PI * 2f;
        private const float CONTAINER_BOB_RATIO = 0.35f;
        private const float DRIFT_FREQUENCY = 0.73f;
        private const float TILT_FREQUENCY = 0.9f;
        private const float BREATH_FREQUENCY = 1.7f;
        private const float BREATH_SQUASH_RATIO = 0.65f;

        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;
        [Tooltip("Material while inside a bubble: fades out towards the top of the field.")]
        [SerializeField] private Material bubbleMaterial;
        [Tooltip("Plain sprite material once the fish has left its bubble.")]
        [SerializeField] private Material flightMaterial;
        [Tooltip("Solid-colour silhouette material for the outline while in a bubble (fades like the body).")]
        [SerializeField] private Material outlineBubbleMaterial;
        [Tooltip("Same silhouette material without the fade, for flight.")]
        [SerializeField] private Material outlineFlightMaterial;
        [Tooltip("Small twinkling stars while the fish is held/selected (child HoldSparkles).")]
        [SerializeField] private ParticleSystem holdSparkles;
        [SerializeField] private CircleCollider2D hitCollider;
        [Tooltip("Bubble stream left behind while flying.")]
        [SerializeField] private ParticleSystem flightTrail;
        [Tooltip("Twinkling stars left behind while flying (child TrailSparkles).")]
        [SerializeField] private ParticleSystem trailSparkles;
        [SerializeField] private FishSortConfig config;

        private Coroutine scaleRoutine;
        private Coroutine moveRoutine;
        private Transform bodyTransform;
        private Transform glowTransform;
        private float restWidth;
        private Vector3 swimCenter;
        private float swimAngle;
        private float swimPhase;
        private Vector3 bodyRestScale;
        private Vector3 bodyRestPosition;
        private bool idleInBubble;
        private bool idleInContainer;

        public int TypeId { get; private set; }
        public Bubble Owner { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsFlying { get; private set; }
        public bool IsSelectable => (Owner != null || IsInJar) && !IsFrozen;
        public Collider2D HitCollider => hitCollider;

        private void Awake()
        {
            bodyTransform = bodyRenderer.transform;
            glowTransform = glowRenderer.transform;
            bodyRestScale = bodyTransform.localScale;
            bodyRestPosition = bodyTransform.localPosition;
            // Per-instance phases avoid synchronized movement without changing the level RNG.
            swimPhase = Mathf.Repeat(GetInstanceID() * PHASE_SPREAD, FULL_TURN_RADIANS);
        }

        private void LateUpdate()
        {
            if (!IsFlying)
            {
                if (idleInContainer)
                {
                    BobInContainer();
                }
                else if (idleInBubble && (Owner != null || IsInJar) && !IsFrozen)
                {
                    SwimInBubble();
                }
            }
            if (glowRenderer.enabled && !IsFlying)
            {
                glowTransform.localPosition = bodyTransform.localPosition;
                glowTransform.localScale = bodyTransform.localScale;
            }

            UpdateSpine();
        }

        // Gentle vertical bob for fish resting in a tank or the tray.
        private void BobInContainer()
        {
            float phase = Time.time * config.fishSwimSpeed + swimPhase;
            bodyTransform.localPosition = bodyRestPosition + Vector3.up *
                (Mathf.Sin(phase) * config.fishBobHeight * CONTAINER_BOB_RATIO);
        }

        private void SwimInBubble()
        {
            float t = Time.time * config.fishSwimSpeed + swimPhase;
            float strength = config.animateUnderwater ? 1f : 0f;
            float bounce = Owner != null ? Owner.ImpactBounce * strength : 0f;
            Vector3 drift = new Vector3(Mathf.Sin(t * DRIFT_FREQUENCY) * config.fishSwimRadius,
                Mathf.Sin(t + swimPhase) * config.fishBobHeight, 0f) * strength;
            TF.localPosition = swimCenter + drift;
            TF.localRotation = Quaternion.Euler(0f, 0f, swimAngle + Mathf.Sin(t * TILT_FREQUENCY) * config.fishIdleTilt * strength + bounce * 18f);
            float breath = Mathf.Sin(t * BREATH_FREQUENCY) * config.fishBreathing * strength;
            float squash = breath + bounce * .5f;
            bodyTransform.localPosition = bodyRestPosition + Vector3.down * (bounce * .8f);
            bodyTransform.localScale = Vector3.Scale(bodyRestScale, new Vector3(1f + squash, 1f - squash * BREATH_SQUASH_RATIO, 1f));
        }

        private void StopIdleMotion()
        {
            idleInBubble = false;
            idleInContainer = false;
            bodyTransform.localScale = bodyRestScale;
            bodyTransform.localPosition = bodyRestPosition;
        }

        public void Setup(int typeId, FishLook look, Bubble owner, bool frozen)
        {
            StopAllCoroutines();
            StopIdleMotion();
            TypeId = typeId;
            Owner = owner;
            IsInJar = false;
            IsFlying = false;
            bodyRenderer.sprite = look.Sprite;
            bodyRenderer.flipX = false;
            bodyRenderer.enabled = true;
            bodyRenderer.sharedMaterial = bubbleMaterial;
            glowRenderer.sharedMaterial = outlineBubbleMaterial;
            holdSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_BUBBLE;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            glowRenderer.enabled = false;
            glowRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            hitCollider.enabled = true;
            SetTrail(false);
            SetFrozen(frozen);
            BindSpine(look);
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
            isHighlighted = isOn;
            if (isOn)
            {
                ShowOutline(HighlightColor(highlight));
                holdSparkles.Play();
            }
            else
            {
                glowRenderer.enabled = false;
                holdSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            bodyRenderer.sortingOrder = isOn ? LitOrder : RestOrder;
            glowRenderer.sortingOrder = bodyRenderer.sortingOrder + FishSortConst.SORT_GLOW_OFFSET;
            float targetWidth = restWidth * (isOn ? config.highlightScale : 1f);
            StopScale();
            scaleRoutine = StartCoroutine(ScaleRoutine(targetWidth, config.highlightTweenDuration));
        }

        // Outline = the SpriteOutline shader drawing a thin line around the body sprite, behind the body.
        // Line width and padding are material settings (OutlineBubble / OutlineFlight).
        private void ShowOutline(Color color)
        {
            glowRenderer.sprite = bodyRenderer.sprite;
            glowRenderer.flipX = bodyRenderer.flipX;
            glowTransform.localPosition = bodyRestPosition;
            glowTransform.localScale = bodyRestScale;
            glowRenderer.color = color;
            glowRenderer.enabled = true;
        }

        // Leaves the bubble: world position and size are kept, physics/mask no longer apply.
        public void Detach(Transform flightRoot)
        {
            StopScale();
            StopIdleMotion();
            Owner = null;
            IsInJar = false;
            hitCollider.enabled = false;
            glowRenderer.enabled = false;
            TF.SetParent(flightRoot, true);
            bodyRenderer.sharedMaterial = flightMaterial;
            glowRenderer.sharedMaterial = outlineFlightMaterial;
            holdSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
        }

        // Fit actual sprite bounds, so tall species also stay inside the tray glass.
        public float ScaleToFit(float maxWidth, float maxHeight)
        {
            Vector3 size = bodyRenderer.sprite.bounds.size;
            return Mathf.Min(maxWidth / (size.x * Mathf.Abs(bodyRestScale.x)),
                maxHeight / (size.y * Mathf.Abs(bodyRestScale.y)));
        }

        public void RestInTray()
        {
            idleInContainer = true;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_TRAY;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        public void AttachToTank(Transform iconPoint, float width, float height)
        {
            StopMove();
            TF.SetParent(iconPoint, false);
            TF.localPosition = Vector3.zero;
            TF.localRotation = Quaternion.identity;
            TF.localScale = Vector3.one * (ScaleToFit(width, height) / Mathf.Abs(iconPoint.lossyScale.x));
            idleInContainer = true;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_IN_TANK;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        public void Despawn()
        {
            HideSpine();
            StopIdleMotion();
            StopAllCoroutines();
            IsFlying = false;
            SetTrail(false);
            HBPool.Despawn(this);
        }

        private void StopScale()
        {
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
                scaleRoutine = null;
            }
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

        // The trail is a stream of bubbles plus small twinkling stars, both emitting by distance travelled.
        private void SetTrail(bool isOn)
        {
            ParticleSystem.EmissionModule bubbleEmission = flightTrail.emission;
            bubbleEmission.enabled = isOn;
            ParticleSystem.EmissionModule sparkleEmission = trailSparkles.emission;
            sparkleEmission.enabled = isOn;
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
