using UnityEngine;

namespace FishSort
{
    // Reuses the tank's existing artwork: only its rim is redrawn in front of submerged fish.
    public sealed class TankGlassFront : MonoBehaviour
    {
        [SerializeField] private Sprite tankSprite;
        [SerializeField] private Sprite glassSprite;
        private readonly Sprite[] rimSprites = new Sprite[4];

        private void Awake()
        {
            Rect source = tankSprite.rect;
            float w = source.width;
            float h = source.height;
            Rect[] strips =
            {
                new Rect(0f, 0f, w, h * 0.12f),
                new Rect(0f, h * 0.90f, w, h * 0.10f),
                new Rect(0f, h * 0.12f, w * 0.10f, h * 0.78f),
                new Rect(w * 0.90f, h * 0.12f, w * 0.10f, h * 0.78f)
            };
            for (int i = 0; i < strips.Length; i++)
            {
                Rect strip = strips[i];
                rimSprites[i] = Sprite.Create(tankSprite.texture,
                    new Rect(source.x + strip.x, source.y + strip.y, strip.width, strip.height),
                    new Vector2(0.5f, 0.5f), tankSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                SpriteRenderer rim = CreateLayer("FrontRim_" + i, rimSprites[i]);
                rim.transform.localPosition = new Vector3(
                    (strip.center.x - tankSprite.pivot.x) / tankSprite.pixelsPerUnit,
                    (strip.center.y - tankSprite.pivot.y) / tankSprite.pixelsPerUnit, 0f);
            }
            // Restrict submerged fish and the reflection to the interior, never the rim.
            SpriteMask interior = new GameObject("InteriorMask").AddComponent<SpriteMask>();
            interior.transform.SetParent(transform, false);
            interior.sprite = glassSprite;
            interior.alphaCutoff = 0.05f;
            interior.isCustomRangeActive = true;
            // Include the glass, but not the bubble shell at order 5: overlapping masks
            // would otherwise reveal bubble outlines inside the tanks.
            interior.frontSortingOrder = FishSortConst.SORT_TANK_GLASS;
            interior.backSortingOrder = FishSortConst.SORT_FISH_IN_TANK - 1;
            Vector3 maskBounds = glassSprite.bounds.size;
            interior.transform.localPosition = new Vector3(0f, -0.025f, 0f);
            interior.transform.localScale = new Vector3(0.96f / maskBounds.x, 0.76f / maskBounds.y, 1f);
            SpriteRenderer glass = CreateLayer("FrontGlass", glassSprite);
            glass.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            glass.transform.localPosition = new Vector3(0f, -0.015f, 0f);
            Vector3 bounds = glassSprite.bounds.size;
            glass.transform.localScale = new Vector3(0.94f / bounds.x, 0.72f / bounds.y, 1f);
            glass.color = new Color(0.65f, 0.91f, 1f, 0.9f);
        }

        private SpriteRenderer CreateLayer(string name, Sprite sprite)
        {
            SpriteRenderer layer = new GameObject(name).AddComponent<SpriteRenderer>();
            layer.transform.SetParent(transform, false);
            layer.sprite = sprite;
            layer.sortingOrder = FishSortConst.SORT_TANK_GLASS;
            return layer;
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in rimSprites) if (sprite != null) Destroy(sprite);
        }
    }
}
