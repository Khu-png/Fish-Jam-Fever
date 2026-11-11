using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // The auxiliary container of the cannon booster: a small glass dome standing at the right end of the tray.
    // It is not a bubble (no physics, no pool, fixed size): it only holds the fish shot off the tray, shows how many
    // are left, gives them to tanks that need them, lets the player pick them, and goes away when empty.
    public sealed class FishJar : MonoBehaviour
    {
        // Fish positions inside the jar (jar-width units) for 1..5 fish: centred in the dome, above the base that
        // carries the counter. The dome spans y -0.41..0.41 and the base covers its bottom up to y -0.21.
        private static readonly Vector2[][] Layouts =
        {
            new[] { new Vector2(0f, 0.1f) },
            new[] { new Vector2(-0.2f, 0.08f), new Vector2(0.2f, 0.08f) },
            new[] { new Vector2(-0.2f, 0f), new Vector2(0.2f, 0f), new Vector2(0f, 0.2f) },
            new[] { new Vector2(-0.2f, 0f), new Vector2(0.2f, 0f), new Vector2(-0.2f, 0.2f), new Vector2(0.2f, 0.2f) },
            new[]
            {
                new Vector2(-0.28f, 0f), new Vector2(0f, 0f), new Vector2(0.28f, 0f),
                new Vector2(-0.14f, 0.2f), new Vector2(0.14f, 0.2f)
            }
        };

        private const float FISH_WIDTH = 0.38f;
        private const float FISH_TILT = 5f;
        private const float OPEN_START_SCALE = 0.2f;
        private const float OPEN_SPEED = 12f;
        private const float POP_SCALE = 0.8f;

        [SerializeField] private SpriteRenderer countRenderer;
        [SerializeField] private Sprite[] countSprites;
        [SerializeField] private FishSortConfig config;

        private readonly List<Fish> fishes = new List<Fish>(FishSortConst.MAX_FISH_PER_BUBBLE);
        private Transform cachedTransform;
        private int expectedFish;

        public bool IsOpen { get; private set; }
        public int Count => fishes.Count;

        private Transform TF
        {
            get
            {
                if (cachedTransform == null)
                {
                    cachedTransform = transform;
                }

                return cachedTransform;
            }
        }

        // Where the jar stands: the position of this object in GameplayWorld.prefab (drag it to move the jar).
        public Vector3 HomePosition => TF.position;

        // The jar appears where it stands; expectedCount is how many fish are about to enter (it decides their layout).
        public void Open(int expectedCount)
        {
            fishes.Clear();
            expectedFish = expectedCount;
            TF.localScale = Vector3.one * (config.cannonJarDiameter * OPEN_START_SCALE);
            gameObject.SetActive(true);
            IsOpen = true;
            SetCount(0);
        }

        public bool Owns(Fish fish)
        {
            return fishes.Contains(fish);
        }

        // A fish arrives: it takes the next spot of the layout and the counter goes up.
        public void Add(Fish fish)
        {
            Vector2[] layout = Layouts[Mathf.Clamp(expectedFish, 1, Layouts.Length) - 1];
            int index = fishes.Count;
            float tilt = index % 2 == 0 ? FISH_TILT : -FISH_TILT;
            fish.EnterJar(TF, layout[index % layout.Length], FISH_WIDTH, tilt, index);
            fishes.Add(fish);
            SetCount(fishes.Count);
        }

        // The fish leaves (picked by the player or sent to a tank): the counter goes down; an empty jar pops away.
        public void Release(Fish fish)
        {
            fishes.Remove(fish);
            SetCount(fishes.Count);
            if (IsOpen && fishes.Count == 0)
            {
                ParticlePool.Play(ParticleType.TankBubblePop, TF.position, Quaternion.identity, POP_SCALE);
                SoundManager.Ins.PlayFx(FxID.BubblePop);
                Hide();
            }
        }

        // Fish of the type, up to maxCount, appended to output.
        public void CollectFish(int typeId, int maxCount, List<Fish> output)
        {
            for (int i = 0; i < fishes.Count && output.Count < maxCount; i++)
            {
                if (fishes[i].TypeId == typeId)
                {
                    output.Add(fishes[i]);
                }
            }
        }

        // The fish whose hit area is closest to the point (within the pick radius), or null.
        public Fish FindFishNear(Vector2 point)
        {
            Fish best = null;
            float bestDistance = config.pickRadius * config.pickRadius;
            for (int i = 0; i < fishes.Count; i++)
            {
                float distance = fishes[i].HitCollider.bounds.SqrDistance(point);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = fishes[i];
                }
            }

            return best;
        }

        // Back to nothing (the fish themselves are pooled by the level reset).
        public void Hide()
        {
            IsOpen = false;
            fishes.Clear();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            float target = config.cannonJarDiameter;
            float current = TF.localScale.x;
            if (Mathf.Abs(current - target) > Mathf.Epsilon)
            {
                TF.localScale = Vector3.one * Mathf.MoveTowards(current, target, Time.deltaTime * OPEN_SPEED * target);
            }
        }

        private void SetCount(int count)
        {
            countRenderer.enabled = count > 0;
            if (count > 0)
            {
                countRenderer.sprite = countSprites[Mathf.Min(count, countSprites.Length) - 1];
            }
        }
    }
}
