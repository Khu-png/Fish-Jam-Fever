using UnityEngine;

namespace FishSort
{
    // Pins a world-space group to the top or bottom edge of the camera (runs after CameraFit).
    public sealed class ScreenEdgeAnchor : MonoBehaviour
    {
        public enum Edge { Top, Bottom }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Edge edge = Edge.Top;
        [SerializeField] private float offset;
        [SerializeField] private float referenceViewportHeight;

        private bool hasPositionedChildren;

        // The anchor lives in a prefab, so the scene camera is injected right after the prefab is spawned.
        public void SetCamera(Camera sceneCamera)
        {
            targetCamera = sceneCamera;
            ApplyPosition();
        }

        private void Start()
        {
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (targetCamera == null) return;
            Transform cachedTransform = transform;
            float halfHeight = targetCamera.orthographicSize;
            // Reflow the HUD's vertical spacing without stretching its artwork.
            if (!hasPositionedChildren && referenceViewportHeight > 0f)
            {
                float ratio = halfHeight * 2f / referenceViewportHeight;
                foreach (Transform child in cachedTransform)
                {
                    // The field mask must retain its full height and follow FieldTop.
                    Vector3 local = child.localPosition;
                    if (child.GetComponent<SpriteMask>() != null)
                    {
                        float top = local.y + child.localScale.y * 0.5f;
                        local.y += top * (ratio - 1f);
                    }
                    else local.y *= ratio;
                    child.localPosition = local;
                }
                hasPositionedChildren = true;
            }
            float cameraY = targetCamera.transform.position.y;
            float edgeY = edge == Edge.Top ? cameraY + halfHeight : cameraY - halfHeight;
            Vector3 position = cachedTransform.position;
            position.y = edgeY + offset;
            cachedTransform.position = position;
        }
    }
}
