using UnityEngine;

namespace FishSort
{
    // The fish side of the cannon's jar (FishJar): how a fish sits in it and how it is drawn there.
    public sealed partial class Fish
    {
        // Sorting orders above the bubbles (the jar sits above the field, with no field mask).
        private const int JAR_FISH_BASE_ORDER = FishSortConst.SORT_FISH_HIGHLIGHT + 2;
        private const int JAR_LIT_BOOST = 2;

        private int jarOrder;

        // True while the fish sits in the jar (instead of a bubble).
        public bool IsInJar { get; private set; }

        private int RestOrder => IsInJar ? jarOrder : FishSortConst.SORT_FISH_IN_BUBBLE;
        private int LitOrder => IsInJar ? jarOrder + JAR_LIT_BOOST : FishSortConst.SORT_FISH_HIGHLIGHT;

        // Puts the fish into the jar at a local spot (jar-diameter units). Later fish overlap earlier ones.
        public void EnterJar(Transform jar, Vector2 localPosition, float width, float tilt, int stackIndex)
        {
            StopMove();
            IsFlying = false;
            PlaceInBubble(jar, localPosition, width, tilt);
            IsInJar = true;
            jarOrder = JAR_FISH_BASE_ORDER + stackIndex;
            SetFrozen(false);
            bodyRenderer.sharedMaterial = flightMaterial;
            glowRenderer.sharedMaterial = outlineFlightMaterial;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
            glowRenderer.maskInteraction = SpriteMaskInteraction.None;
            bodyRenderer.sortingOrder = jarOrder;
            glowRenderer.enabled = false;
            hitCollider.enabled = true;
        }

        // The cannon's bullet has passed: the fish gets its white outline and from now on is moved by the caller
        // (it follows the bullet, then enters the jar).
        public void BeginFollow()
        {
            StopIdleMotion();
            StopMove();
            IsFlying = true;
            ShowOutline(config.highlightColor);
            glowRenderer.maskInteraction = SpriteMaskInteraction.None;
            glowRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT + FishSortConst.SORT_GLOW_OFFSET;
            bodyRenderer.sortingOrder = FishSortConst.SORT_FISH_FLIGHT;
            bodyRenderer.maskInteraction = SpriteMaskInteraction.None;
        }
    }
}
