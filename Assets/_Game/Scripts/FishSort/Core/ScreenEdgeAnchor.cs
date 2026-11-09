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

        private void Start()
        {
            Transform cachedTransform = transform;
            float halfHeight = targetCamera.orthographicSize;
            float cameraY = targetCamera.transform.position.y;
            float edgeY = edge == Edge.Top ? cameraY + halfHeight : cameraY - halfHeight;
            Vector3 position = cachedTransform.position;
            position.y = edgeY + offset;
            cachedTransform.position = position;
        }
    }
}
