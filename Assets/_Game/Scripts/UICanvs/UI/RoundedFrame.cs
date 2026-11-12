using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    // Rounded contours keep the stroke uniform at any canvas size, without stretching a sprite.
    [AddComponentMenu("UI/Rounded Frame")]
    public sealed class RoundedFrame : MaskableGraphic
    {
        [SerializeField, Min(1f)] private float cornerRadius = 64f;
        [SerializeField, Min(1f)] private float thickness = 16f;
        [SerializeField, Min(0f)] private float glow = 40f;
        [SerializeField, Min(0f)] private float inset = 4f;
        [SerializeField] private bool fadeInward;
        private const int CornerSteps = 24;
        private const int ContourSize = (CornerSteps + 1) * 4;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            AddContour(vh, rect, inset, 0f);
            int bandCount;
            if (fadeInward)
            {
                // Fade across the entire border instead of keeping an opaque, flat strip.
                const int fadeSteps = 20;
                for (int step = 0; step <= fadeSteps; step++)
                {
                    float t = step / (float)fadeSteps;
                    AddContour(vh, rect, inset + Mathf.Lerp(2f, thickness + glow + 3f, t),
                        1f - Mathf.SmoothStep(0f, 1f, t));
                }
                bandCount = fadeSteps + 1;
            }
            else
            {
                AddContour(vh, rect, inset + 2f, 1f);
                AddContour(vh, rect, inset + thickness, 1f);
                AddContour(vh, rect, inset + thickness + 3f, .65f);
                AddContour(vh, rect, inset + thickness + glow * .4f + 3f, .18f);
                AddContour(vh, rect, inset + thickness + glow + 3f, 0f);
                bandCount = 5;
            }
            for (int band = 0; band < bandCount; band++)
            {
                int outer = band * ContourSize;
                int inner = outer + ContourSize;
                for (int i = 0; i < ContourSize; i++)
                {
                    int next = (i + 1) % ContourSize;
                    vh.AddTriangle(outer + i, outer + next, inner + i);
                    vh.AddTriangle(outer + next, inner + next, inner + i);
                }
            }
        }

        private void AddContour(VertexHelper vh, Rect rect, float distance, float alpha)
        {
            distance = Mathf.Min(distance, Mathf.Min(rect.width, rect.height) * .5f - .01f);
            float radius = Mathf.Clamp(cornerRadius - distance, .01f,
                Mathf.Min(rect.width, rect.height) * .5f - distance);
            Color tint = color;
            tint.a *= alpha;
            for (int corner = 0; corner < 4; corner++)
            {
                bool right = corner == 0 || corner == 3;
                bool top = corner < 2;
                Vector2 centre = new Vector2(
                    right ? rect.xMax - distance - radius : rect.xMin + distance + radius,
                    top ? rect.yMax - distance - radius : rect.yMin + distance + radius);
                for (int step = 0; step <= CornerSteps; step++)
                {
                    float angle = (corner * 90f + step * 90f / CornerSteps) * Mathf.Deg2Rad;
                    Vector2 point = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    vh.AddVert(point, tint, Vector2.zero);
                }
            }
        }
    }
}
