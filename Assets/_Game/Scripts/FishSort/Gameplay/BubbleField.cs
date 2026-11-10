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

        [SerializeField] private FishSortConfig config;
        [SerializeField] private FishCatalog catalog;
        [SerializeField] private Transform bubbleRoot;
        [SerializeField] private Transform floorPoint;
        [SerializeField] private Transform fieldTopPoint;

        private readonly List<Bubble> bubbles = new List<Bubble>();
        private readonly List<Fish> allFish = new List<Fish>();
        private readonly Dictionary<Collider2D, Fish> fishByCollider = new Dictionary<Collider2D, Fish>();
        private readonly List<Collider2D> hits = new List<Collider2D>(HIT_BUFFER_SIZE);
        private ContactFilter2D pickFilter;

        public int TotalFish => allFish.Count;
        public float FieldTopY => fieldTopPoint.position.y;

        private void Awake()
        {
            pickFilter = ContactFilter2D.noFilter;
            pickFilter.useTriggers = true;
        }

        public void Build(LevelData level, int seed)
        {
            Clear();
            System.Random rng = new System.Random(seed);
            int levelFishTotal = level.TotalFish;
            float fieldWidth = config.fieldHalfWidth * BOTH_SIDES;
            float cellWidth = fieldWidth / level.Columns;
            float[] columnTop = new float[level.Columns];
            for (int c = 0; c < columnTop.Length; c++)
            {
                columnTop[c] = floorPoint.position.y;
            }

            BubbleData[] data = level.Bubbles;
            for (int i = 0; i < data.Length; i++)
            {
                int fishInBubble = data[i].fishTypes.Length;
                int column = Mathf.Clamp(data[i].column, 0, level.Columns - 1);
                float diameter = config.BubbleDiameter(fishInBubble, levelFishTotal);
                float x = -config.fieldHalfWidth + cellWidth * (column + HALF) + RandomRange(rng, config.spawnJitter);
                float visibleHalfWidth = config.fieldHalfWidth - diameter * HALF - 0.04f;
                x = Mathf.Clamp(x, -visibleHalfWidth, visibleHalfWidth);
                float y = columnTop[column] + diameter * HALF;
                columnTop[column] += diameter;

                Bubble bubble = HBPool.Spawn<Bubble>(PoolType.Bubble, new Vector3(x, y, 0f), Quaternion.identity);
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

            Presettle();
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
            fishByCollider.Remove(fish.HitCollider);
            allFish.Remove(fish);
            bubble.RemoveFish(fish);
            if (bubble.FishCount == 0)
            {
                bubbles.Remove(bubble);
                ParticlePool.Play(ParticleType.BubblePop, bubble.TF.position, Quaternion.identity);
                SoundManager.Ins.PlayFx(FxID.BubblePop);
                bubble.Pop();
            }
        }

        // Design doc 4.4: every pick of type X cracks each frozen bubble that contains X.
        public void CrackIce(int typeId)
        {
            for (int i = 0; i < bubbles.Count; i++)
            {
                Bubble bubble = bubbles[i];
                if (bubble.IsFrozen && bubble.ContainsType(typeId))
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

        // Runs the physics ahead of time so the level opens already packed.
        private void Presettle()
        {
            Physics2D.SyncTransforms();
            SimulationMode2D previousMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int i = 0; i < config.presettleSteps; i++)
            {
                Physics2D.Simulate(Time.fixedDeltaTime);
            }

            Physics2D.simulationMode = previousMode;
        }

        private static float RandomRange(System.Random rng, float halfRange)
        {
            return ((float)rng.NextDouble() * BOTH_SIDES - 1f) * halfRange;
        }
    }
}
