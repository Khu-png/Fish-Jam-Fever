using UnityEngine;

namespace FishSort
{
    // Per-tank reusable splash layers. All artwork comes from the existing project textures.
    public sealed class TankWaterSplash : MonoBehaviour
    {
        [SerializeField] private Texture2D splashSheet;
        [SerializeField] private Texture2D dropletTexture;
        [SerializeField] private Sprite bubbleSprite;
        [SerializeField] private float surfaceHeight = 0.35f;
        [SerializeField] private float duration = 0.48f;
        [SerializeField] private float splashWidth = 0.62f;
        [SerializeField] private float splashHeight = 0.46f;

        private const int PoolSize = 3;
        private const int DropCount = 6;
        private const int BubbleCount = 3;
        private readonly Burst[] bursts = new Burst[PoolSize];
        private Sprite[] frames;
        private Sprite dropSprite;
        private int nextBurst;

        public event System.Action WaterEntered;

        private sealed class Burst
        {
            public Transform root;
            public SpriteRenderer crown;
            public SpriteRenderer[] drops;
            public SpriteRenderer[] bubbles;
            public float age;
            public bool active;
        }

        public Vector3 SurfacePosition(float localX)
        {
            return transform.TransformPoint(new Vector3(Mathf.Clamp(localX, -0.32f, 0.32f), surfaceHeight, 0f));
        }

        private void Awake()
        {
            frames = new Sprite[25];
            float width = splashSheet.width / 5f;
            float height = splashSheet.height / 5f;
            for (int i = 0; i < frames.Length; i++)
            {
                Rect cell = new Rect(i % 5 * width, (4 - i / 5) * height, width, height);
                frames[i] = Sprite.Create(splashSheet, cell, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            }
            dropSprite = Sprite.Create(dropletTexture, new Rect(0, 0, dropletTexture.width, dropletTexture.height),
                new Vector2(0.5f, 0.5f), dropletTexture.width, 0, SpriteMeshType.FullRect);
            for (int i = 0; i < bursts.Length; i++)
            {
                Burst burst = new Burst();
                burst.root = new GameObject("WaterEntry_" + i).transform;
                burst.root.SetParent(transform, false);
                burst.crown = CreateRenderer("Splash", burst.root, frames[0]);
                burst.drops = new SpriteRenderer[DropCount];
                for (int j = 0; j < DropCount; j++) burst.drops[j] = CreateRenderer("Drop_" + j, burst.root, dropSprite);
                burst.bubbles = new SpriteRenderer[BubbleCount];
                for (int j = 0; j < BubbleCount; j++) burst.bubbles[j] = CreateRenderer("Bubble_" + j, burst.root, bubbleSprite);
                burst.root.gameObject.SetActive(false);
                bursts[i] = burst;
            }
        }

        private static SpriteRenderer CreateRenderer(string name, Transform parent, Sprite sprite)
        {
            SpriteRenderer renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT + 1;
            return renderer;
        }

        public void Play(Vector3 waterPosition)
        {
            ParticlePool.Play(ParticleType.TankWaterBubbles, waterPosition, Quaternion.identity);
            Burst burst = bursts[nextBurst];
            nextBurst = (nextBurst + 1) % bursts.Length;
            burst.root.position = waterPosition;
            burst.age = 0f;
            burst.active = true;
            burst.root.gameObject.SetActive(true);
            Animate(burst);
            WaterEntered?.Invoke();
        }

        public void Clear()
        {
            foreach (Burst burst in bursts)
            {
                if (burst == null) continue;
                burst.active = false;
                burst.root.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            foreach (Burst burst in bursts)
            {
                if (!burst.active) continue;
                burst.age += Time.deltaTime;
                if (burst.age >= duration)
                {
                    burst.active = false;
                    burst.root.gameObject.SetActive(false);
                }
                else Animate(burst);
            }
        }

        private void Animate(Burst burst)
        {
            float t = Mathf.Clamp01(burst.age / duration);
            int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length));
            burst.crown.sprite = frames[frame];
            Vector3 bounds = frames[frame].bounds.size;
            burst.crown.transform.localScale = new Vector3(splashWidth / bounds.x, splashHeight / bounds.y, 1f);
            burst.crown.color = new Color(0.7f, 0.96f, 1f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, t)));
            for (int i = 0; i < DropCount; i++)
            {
                float spread = (i - (DropCount - 1) * 0.5f) / ((DropCount - 1) * 0.5f);
                float vx = spread * 0.65f;
                float vy = 1.4f + (1f - Mathf.Abs(spread)) * 0.7f;
                float age = burst.age;
                SpriteRenderer drop = burst.drops[i];
                drop.transform.localPosition = new Vector3(vx * age, vy * age - 4.8f * age * age, 0f);
                drop.transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(vx, vy - 9.6f * age) * Mathf.Rad2Deg);
                float size = (i % 2 == 0 ? 0.035f : 0.045f) * (1f - t * 0.5f);
                drop.transform.localScale = new Vector3(size, size * 1.65f, 1f);
                drop.color = new Color(0.46f, 0.88f, 1f, 1f - Mathf.SmoothStep(0.45f, 1f, t));
            }
            for (int i = 0; i < BubbleCount; i++)
            {
                SpriteRenderer bubble = burst.bubbles[i];
                float delay = i * 0.045f;
                float age = Mathf.Max(0f, burst.age - delay);
                bubble.enabled = burst.age >= delay;
                bubble.transform.localPosition = new Vector3((i - 1) * 0.065f, -0.08f - i * 0.075f + age * 0.22f, 0f);
                bubble.transform.localScale = Vector3.one * ((0.035f + i * 0.012f) / bubbleSprite.bounds.size.x);
                bubble.color = new Color(0.72f, 0.96f, 1f, 0.75f * (1f - t));
            }
        }

        private void OnDisable() => Clear();

        private void OnDestroy()
        {
            if (frames != null) foreach (Sprite frame in frames) if (frame != null) Destroy(frame);
            if (dropSprite != null) Destroy(dropSprite);
        }
    }
}
