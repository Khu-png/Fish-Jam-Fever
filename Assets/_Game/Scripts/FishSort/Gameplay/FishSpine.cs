using Spine.Unity;
using UnityEngine;

namespace FishSort
{
    // Fits one animated rig to its static species image, then follows the gameplay renderer state.
    [DefaultExecutionOrder(100)]
    public sealed class FishSpine : MonoBehaviour
    {
        private const string ANIMATION = "standby_1";
        private const int TRACK = 0;
        private const float MIN_SIZE = 0.0001f;
        private static readonly int FadeOnId = Shader.PropertyToID("_FadeOn");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int SilhouetteId = Shader.PropertyToID("_Silhouette");

        [SerializeField] private SkeletonAnimation skeleton;
        [SerializeField] private MeshRenderer meshRenderer;

        private MaterialPropertyBlock block;
        private Vector2 spriteCenter;
        private Vector2 rigCenter;
        private float fitScale;
        private bool rigFacesRight;
        private bool isFlipped;
        private bool hasFlip;
        private bool isFading;
        private bool hasFade;
        private MeshRenderer outline;
        private MeshFilter outlineFilter;
        private MeshFilter bodyFilter;
        private SpriteRenderer glow;
        private MaterialPropertyBlock outlineBlock;
        private SpriteMaskInteraction outlineMask;
        private bool hasOutlineMaterials;

        public bool IsReady { get; private set; }

        // Loads the rig for the fish and fits it to the sprite. Returns false when the fish has no rig.
        public bool Bind(SkeletonDataAsset asset, bool facesRight, Sprite reference, float phaseSeed)
        {
            IsReady = false;
            Show(false);
            if (asset == null || reference == null)
            {
                return false;
            }

            skeleton.skeletonDataAsset = asset;
            skeleton.Initialize(true);
            Spine.SkeletonData data = asset.GetSkeletonData(false);
            Spine.TrackEntry entry = skeleton.AnimationState.SetAnimation(TRACK, ANIMATION, true);
            entry.TrackTime = Mathf.Repeat(phaseSeed, Mathf.Max(MIN_SIZE, entry.Animation.Duration));
            skeleton.Update(0f);
            float width = Mathf.Max(MIN_SIZE, data.Width * asset.scale);
            float height = Mathf.Max(MIN_SIZE, data.Height * asset.scale);
            fitScale = Mathf.Min(reference.bounds.size.x / width, reference.bounds.size.y / height);
            spriteCenter = reference.bounds.center;
            rigCenter = new Vector2(data.X + data.Width * 0.5f, data.Y + data.Height * 0.5f) * asset.scale;
            rigFacesRight = facesRight;
            hasFlip = false;
            hasFade = false;
            hasOutlineMaterials = false;
            IsReady = true;
            return true;
        }

        public void Show(bool isVisible)
        {
            skeleton.enabled = isVisible;
            meshRenderer.enabled = isVisible;
            if (!isVisible && outline != null) outline.enabled = false;
        }

        // Called every frame while shown. spriteFlipped = the still sprite is mirrored (the fish faces right).
        public void Sync(int sortingOrder, SpriteMaskInteraction mask, bool fadeAtTop, bool spriteFlipped,
            float animationSpeed, Color tint, SpriteRenderer glowRenderer)
        {
            meshRenderer.sortingOrder = sortingOrder;
            skeleton.maskInteraction = mask;
            skeleton.timeScale = animationSpeed;
            ApplyFade(fadeAtTop);
            ApplyFlip(spriteFlipped);
            glow = glowRenderer;
            meshRenderer.GetPropertyBlock(block);
            block.SetColor(TintId, tint);
            meshRenderer.SetPropertyBlock(block);
        }

        private void LateUpdate()
        {
            bool showOutline = IsReady && meshRenderer.enabled && glow != null && glow.enabled;
            if (!showOutline)
            {
                if (outline != null) outline.enabled = false;
                return;
            }
            if (outline == null)
            {
                var visual = new GameObject("AnimatedOutline", typeof(MeshFilter), typeof(MeshRenderer));
                visual.transform.SetParent(transform, false);
                visual.transform.localScale = Vector3.one * 1.08f;
                outline = visual.GetComponent<MeshRenderer>();
                outlineFilter = visual.GetComponent<MeshFilter>();
                bodyFilter = skeleton.GetComponent<MeshFilter>();
            }
            // Reuse the current pose mesh, so selection never needs a second animated skeleton.
            outlineFilter.sharedMesh = bodyFilter.sharedMesh;
            if (!hasOutlineMaterials || outlineMask != skeleton.maskInteraction)
            {
                outline.sharedMaterials = meshRenderer.sharedMaterials;
                outlineMask = skeleton.maskInteraction;
                hasOutlineMaterials = true;
            }
            outline.enabled = true;
            outline.sortingLayerID = meshRenderer.sortingLayerID;
            outline.sortingOrder = meshRenderer.sortingOrder - 1;
            float sign = isFlipped != rigFacesRight ? -1f : 1f;
            outline.transform.localPosition = new Vector3(-sign * rigCenter.x * .08f, -rigCenter.y * .08f, 0f);
            if (outlineBlock == null) outlineBlock = new MaterialPropertyBlock();
            outlineBlock.SetFloat(FadeOnId, isFading ? 1f : 0f);
            outlineBlock.SetFloat(SilhouetteId, 1f);
            outlineBlock.SetColor(TintId, glow.color);
            outline.SetPropertyBlock(outlineBlock);
        }

        private void ApplyFlip(bool spriteFlipped)
        {
            if (hasFlip && spriteFlipped == isFlipped)
            {
                return;
            }

            hasFlip = true;
            isFlipped = spriteFlipped;
            // The sprite faces left; a rig drawn facing right has to be mirrored when the sprite is not.
            bool mirrorRig = spriteFlipped != rigFacesRight;
            float rigSign = mirrorRig ? -1f : 1f;
            float spriteSign = spriteFlipped ? -1f : 1f;
            skeleton.Skeleton.ScaleX = rigSign;
            Transform own = transform;
            own.localScale = Vector3.one * fitScale;
            own.localPosition = new Vector3(spriteSign * spriteCenter.x - rigSign * rigCenter.x * fitScale,
                spriteCenter.y - rigCenter.y * fitScale, 0f);
        }

        private void ApplyFade(bool fadeAtTop)
        {
            if (hasFade && fadeAtTop == isFading)
            {
                return;
            }

            hasFade = true;
            isFading = fadeAtTop;
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(block);
            block.SetFloat(FadeOnId, fadeAtTop ? 1f : 0f);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
