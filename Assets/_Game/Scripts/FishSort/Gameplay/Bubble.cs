using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // A physics bubble holding 1-5 fish. Size follows fish count; it shrinks as fish leave and pops when empty.
    public sealed class Bubble : GameUnit
    {
        // Fish positions inside the bubble, in bubble-diameter units (sprite diameter = 1).
        private static readonly Vector2[][] FishLayouts =
        {
            new[] { Vector2.zero },
            new[] { new Vector2(-0.27f, 0.02f), new Vector2(0.27f, -0.04f) },
            new[] { new Vector2(-0.22f, 0.15f), new Vector2(0.22f, 0.12f), new Vector2(0f, -0.2f) },
            new[] { new Vector2(-0.2f, 0.2f), new Vector2(0.2f, 0.2f), new Vector2(-0.2f, -0.18f), new Vector2(0.2f, -0.18f) },
            new[] { new Vector2(-0.22f, 0.22f), new Vector2(0.22f, 0.24f), Vector2.zero, new Vector2(-0.22f, -0.22f), new Vector2(0.22f, -0.2f) }
        };

        private const float SCALE_EPSILON = 0.001f;
        private const float SHELL_FADE_END = 0.65f;

        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CircleCollider2D bodyCollider;
        [SerializeField] private SpriteRenderer shellRenderer;
        [SerializeField] private IceCover ice;
        [SerializeField] private FishSortConfig config;

        private readonly List<Fish> fishes = new List<Fish>(FishSortConst.MAX_FISH_PER_BUBBLE);
        private int initialCount;
        private float fullDiameter;
        private float targetDiameter;
        private float waterPhase;
        private float lastDisturbance = float.NegativeInfinity;
        private bool isPopping;
        private bool isSettled;
        private Vector2 settleTarget;
        private Vector3 shellRestScale;
        private Quaternion shellRestRotation;

        private void Awake()
        {
            shellRestScale = shellRenderer.transform.localScale;
            shellRestRotation = shellRenderer.transform.localRotation;
            waterPhase = Mathf.Repeat(GetInstanceID() * 0.754878f, Mathf.PI * 2f);
        }

        // A very gentle pull towards the bottom centre while the bubble is nearly at rest, so the pile
        // keeps sliding into gaps. Only after the opening fall, and it fades out as the bubble moves.
        private void FixedUpdate()
        {
            float maxSpeedSqr = config.bubbleSettleMaxSpeed * config.bubbleSettleMaxSpeed;
            if (isSettled && !isPopping && body.simulated && body.linearVelocity.sqrMagnitude < maxSpeedSqr)
            {
                Vector2 towardTarget = settleTarget - body.position;
                body.AddForce(towardTarget.normalized * (config.bubbleSettlePull * body.mass));
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isPopping && collision.relativeVelocity.sqrMagnitude > 0.04f)
                lastDisturbance = Time.time;
        }

        private void AnimateShell()
        {
            float stretch = 0f;
            float tilt = 0f;
            if (config.animateUnderwater && !IsFrozen)
            {
                float t = Time.time * 1.1f + waterPhase;
                stretch = Mathf.Sin(t) * config.bubbleIdleStretch;
                float reactionAge = Time.time - lastDisturbance;
                if (reactionAge < 0.6f)
                    stretch += Mathf.Sin(reactionAge * 25f) * (1f - reactionAge / 0.6f) * config.bubbleReactionStretch;
                tilt = Mathf.Sin(t * 0.7f) * 1.5f;
            }
            shellRenderer.transform.localScale = Vector3.Scale(shellRestScale, new Vector3(1f + stretch, 1f - stretch, 1f));
            shellRenderer.transform.localRotation = shellRestRotation * Quaternion.Euler(0f, 0f, tilt);
        }

        public bool IsFrozen => ice.IsActive;
        public Collider2D BodyCollider => bodyCollider;
        // Collider radius for a bubble of diameter 1 (the root scale equals the diameter).
        public float UnitColliderRadius => bodyCollider.radius;
        public int FishCount => fishes.Count;
        public IReadOnlyList<Fish> Fishes => fishes;

        public void Setup(Vector2 position, float diameter, int iceTaps)
        {
            StopAllCoroutines();
            fishes.Clear();
            initialCount = 0;
            isPopping = false;
            isSettled = false;
            lastDisturbance = float.NegativeInfinity;
            shellRenderer.transform.localScale = shellRestScale;
            shellRenderer.transform.localRotation = shellRestRotation;
            body.gravityScale = config.bubbleGravityScale;
            body.linearDamping = config.bubbleWaterDamping;
            fullDiameter = diameter;
            targetDiameter = diameter;
            TF.position = position;
            TF.rotation = Quaternion.identity;
            TF.localScale = Vector3.one * diameter;
            shellRenderer.color = Color.white;
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            ice.Setup(iceTaps);
        }

        public void AddFish(Fish fish, int slot, int fishInBubble, float angle)
        {
            Vector2[] layout = FishLayouts[Mathf.Clamp(fishInBubble, 1, FishLayouts.Length) - 1];
            fish.PlaceInBubble(TF, layout[slot], config.FishWidth(fishInBubble), angle);
            fishes.Add(fish);
            initialCount++;
        }

        public void SetSettleTarget(Vector2 target)
        {
            settleTarget = target;
        }

        public void BeginEntryDrop()
        {
            body.gravityScale = config.bubbleEntryGravityScale;
            body.linearDamping = config.bubbleEntryDamping;
            body.WakeUp();
            StartCoroutine(EntryDropRoutine());
        }

        // Keep the gentle fall until the bubble has actually landed, so full gravity never kicks in mid-air.
        private IEnumerator EntryDropRoutine()
        {
            yield return new WaitForSeconds(config.bubbleEntryDuration);
            float settleSpeedSqr = config.bubbleEntrySettleSpeed * config.bubbleEntrySettleSpeed;
            float elapsed = config.bubbleEntryDuration;
            while (elapsed < config.bubbleEntryMaxDuration && body.linearVelocity.sqrMagnitude > settleSpeedSqr)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            body.gravityScale = config.bubbleGravityScale;
            body.linearDamping = config.bubbleWaterDamping;
            isSettled = true;
        }

        public void RemoveFish(Fish fish)
        {
            fishes.Remove(fish);
            lastDisturbance = Time.time;
            float fill = initialCount > 0 ? (float)fishes.Count / initialCount : 0f;
            targetDiameter = fullDiameter * Mathf.Lerp(config.bubbleMinScale, 1f, fill);
        }

        public bool ContainsType(int typeId)
        {
            bool found = false;
            for (int i = 0; i < fishes.Count && !found; i++)
            {
                found = fishes[i].TypeId == typeId;
            }

            return found;
        }

        // One fish picked from a touching bubble. Returns true when the ice melts.
        public bool CrackIce()
        {
            bool melted = ice.Crack();
            if (melted)
            {
                for (int i = 0; i < fishes.Count; i++)
                {
                    fishes[i].SetFrozen(false);
                }
            }

            return melted;
        }

        public void Pop()
        {
            isPopping = true;
            body.simulated = false;
            StartCoroutine(PopRoutine());
        }

        private void Update()
        {
            if (isPopping) return;
            AnimateShell();
            float current = TF.localScale.x;
            if (Mathf.Abs(current - targetDiameter) > SCALE_EPSILON)
            {
                TF.localScale = Vector3.one * Mathf.Lerp(current, targetDiameter, Time.deltaTime * config.bubbleShrinkSpeed);
            }
        }

        // Shell swells and fades; the droplet burst is the pooled FX_BubblePop played by BubbleField.
        private IEnumerator PopRoutine()
        {
            float startDiameter = TF.localScale.x;
            float endDiameter = startDiameter * config.bubblePopScale;
            targetDiameter = endDiameter;
            Color clear = new Color(1f, 1f, 1f, 0f);
            float elapsed = 0f;
            while (elapsed < config.bubblePopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.bubblePopDuration);
                float spread = Ease.OutQuad(t);
                TF.localScale = Vector3.one * Mathf.Lerp(startDiameter, endDiameter, spread);
                shellRenderer.color = Color.Lerp(Color.white, clear,
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, SHELL_FADE_END, t)));
                yield return null;
            }

            HBPool.Despawn(this);
        }
    }
}
