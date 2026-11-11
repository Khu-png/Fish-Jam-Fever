using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Spawns bubbles + fish for a level, lets Physics2D stack them, and answers "which fish is under the finger".
    public sealed class BubbleField : MonoBehaviour
    {
        private const int HIT_BUFFER_SIZE = 16;
        private const int FIRST_FISH_SLOT = 0;
        private const float HALF = 0.5f;
        private const float BOTH_SIDES = 2f;
        private const float PLACE_GAP = 0.02f;
        private const string SHADER_FIELD_TOP = "_FieldTopY";
        private static readonly int FieldTopId = Shader.PropertyToID(SHADER_FIELD_TOP);

        [SerializeField] private FishSortConfig config;
        [SerializeField] private FishCatalog catalog;
        [SerializeField] private Transform bubbleRoot;
        [SerializeField] private Transform floorPoint;
        [SerializeField] private Transform fieldTopPoint;

        private readonly List<Bubble> bubbles = new List<Bubble>();
        private readonly List<Fish> allFish = new List<Fish>();
        private readonly Dictionary<Collider2D, Fish> fishByCollider = new Dictionary<Collider2D, Fish>();
        private readonly List<Collider2D> hits = new List<Collider2D>(HIT_BUFFER_SIZE);
        // x, y, collider radius of bubbles already placed in this build.
        private readonly List<Vector3> placed = new List<Vector3>();
        private ContactFilter2D pickFilter;

        public int TotalFish => allFish.Count;
        public float FieldTopY => fieldTopPoint.position.y;

        private void Awake()
        {
            pickFilter = ContactFilter2D.noFilter;
            pickFilter.useTriggers = true;
        }

        // New random look for every fish type; call before the tanks and bubbles are built.
        public void ShuffleSpecies()
        {
            catalog.Shuffle(new System.Random(System.Environment.TickCount));
        }

        public void Build(LevelData level, int seed)
        {
            Clear();
            // In-bubble sprites fade out towards this height (SpriteFieldFade shader).
            Shader.SetGlobalFloat(FieldTopId, FieldTopY);
            System.Random rng = new System.Random(seed);
            int levelFishTotal = level.TotalFish;
            float fieldWidth = config.fieldHalfWidth * BOTH_SIDES;
            float cellWidth = fieldWidth / level.Columns;
            float startY = floorPoint.position.y + config.bubbleEntryDropHeight;
            placed.Clear();
            BubbleData[] data = level.Bubbles;
            for (int i = 0; i < data.Length; i++)
            {
                int fishInBubble = data[i].fishTypes.Length;
                int column = Mathf.Clamp(data[i].column, 0, level.Columns - 1);
                float diameter = config.BubbleDiameter(fishInBubble, levelFishTotal);
                float x = -config.fieldHalfWidth + cellWidth * (column + HALF) + RandomRange(rng, config.spawnJitter);
                float visibleHalfWidth = config.fieldHalfWidth - diameter * HALF - 0.04f;
                x = Mathf.Clamp(x, -visibleHalfWidth, visibleHalfWidth);
                Bubble bubble = HBPool.Spawn<Bubble>(PoolType.Bubble, new Vector3(x, startY, 0f), Quaternion.identity);
                float radius = bubble.UnitColliderRadius * diameter;
                float y = LowestFreeY(x, radius, startY);
                placed.Add(new Vector3(x, y, radius));
                bubble.TF.SetParent(bubbleRoot, true);
                bubble.Setup(new Vector2(x, y), diameter, data[i].iceTaps);
                bool frozen = data[i].iceTaps > 0;
                for (int k = 0; k < fishInBubble; k++)
                {
                    int typeId = data[i].fishTypes[k];
                    Fish fish = HBPool.Spawn<Fish>(PoolType.Fish, bubble.TF.position, Quaternion.identity);
                    fish.Setup(typeId, catalog.GetSprite(typeId), bubble, frozen);
                    bubble.AddFish(fish, k, fishInBubble, RandomRange(rng, config.fishRotationRange));
                    allFish.Add(fish);
                    fishByCollider[fish.HitCollider] = fish;
                }

                bubbles.Add(bubble);
            }

            // Let the real simulation play the opening fall while the level is visible.
            for (int i = 0; i < bubbles.Count; i++)
            {
                bubbles[i].SetSettleTarget(floorPoint.position);
                bubbles[i].BeginEntryDrop();
            }

            Physics2D.SyncTransforms();
        }

        public void Clear()
        {
            bubbles.Clear();
            allFish.Clear();
            fishByCollider.Clear();
        }

        // Nearest selectable fish centre to the point, among fish whose hit circle covers it.
        public Fish FindFishNear(Vector2 point)
        {
            Fish best = null;
            float bestDistance = float.MaxValue;
            int count = Physics2D.OverlapCircle(point, config.pickRadius, pickFilter, hits);
            float fieldTop = FieldTopY;
            for (int i = 0; i < count; i++)
            {
                if (!fishByCollider.TryGetValue(hits[i], out Fish fish) || !fish.IsSelectable)
                {
                    continue;
                }

                Vector2 center = fish.TF.position;
                if (center.y > fieldTop)
                {
                    continue;
                }

                float distance = (center - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = fish;
                }
            }

            return best;
        }

        // Called when the player picks a fish: shrink its bubble or pop it.
        public void RemoveFish(Fish fish)
        {
            Bubble bubble = fish.Owner;
            CrackIce(bubble);
            fishByCollider.Remove(fish.HitCollider);
            allFish.Remove(fish);
            bubble.RemoveFish(fish);
            if (bubble.FishCount == 0)
            {
                bubbles.Remove(bubble);
                // Bubble root scale = its diameter, so bigger bubbles burst bigger.
                ParticlePool.Play(ParticleType.BubblePop, bubble.TF.position, Quaternion.identity, bubble.TF.localScale.x);
                SoundManager.Ins.PlayFx(FxID.BubblePop);
                bubble.Pop();
            }
        }

        // Check the source collider before it shrinks or pops, including the last fish picked.
        public void CrackIce(Bubble source)
        {
            float contactTolerance = Physics2D.defaultContactOffset * BOTH_SIDES;
            for (int i = 0; i < bubbles.Count; i++)
            {
                Bubble bubble = bubbles[i];
                if (bubble == source || !bubble.IsFrozen) continue;
                ColliderDistance2D distance = source.BodyCollider.Distance(bubble.BodyCollider);
                // Resting physics contacts can retain a tiny positive separation.
                if (distance.isValid && distance.distance <= contactTolerance)
                {
                    bubble.CrackIce();
                    SoundManager.Ins.PlayFx(FxID.IceCrack);
                }
            }
        }

        public Fish FirstSelectableFish()
        {
            Fish result = null;
            for (int i = 0; i < bubbles.Count && result == null; i++)
            {
                if (!bubbles[i].IsFrozen && bubbles[i].FishCount > 0)
                {
                    result = bubbles[i].Fishes[FIRST_FISH_SLOT];
                }
            }

            return result;
        }

        // Lowest centre height at or above minY where a circle at x touches no already placed bubble,
        // so no two bubbles overlap when the level opens (overlap would be pushed out as slow upward drift).
        private float LowestFreeY(float x, float radius, float minY)
        {
            float y = minY + radius;
            for (int i = 0; i < placed.Count; i++)
            {
                float reach = radius + placed[i].z;
                float dx = x - placed[i].x;
                if (Mathf.Abs(dx) < reach)
                {
                    y = Mathf.Max(y, placed[i].y + Mathf.Sqrt(reach * reach - dx * dx) + PLACE_GAP);
                }
            }

            return y;
        }

        private static float RandomRange(System.Random rng, float halfRange)
        {
            return ((float)rng.NextDouble() * BOTH_SIDES - 1f) * halfRange;
        }
    }
}
