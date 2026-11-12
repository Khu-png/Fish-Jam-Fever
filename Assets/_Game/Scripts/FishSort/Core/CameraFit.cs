using UnityEngine;

namespace FishSort
{
    // Keeps the full design width visible on every aspect (1080x1920, 720x1600, ...): taller screens just see more field.
    [DefaultExecutionOrder(-100)]
    public sealed class CameraFit : MonoBehaviour
    {
        private const float HALF = 0.5f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private float designWidth = 5.4f;
        [SerializeField] private float minOrthographicSize = 4.8f;

        private void Awake()
        {
            Refit();
        }

        // Also called when the screen changes shape. The aspect is that of the area the camera draws into.
        public void Refit()
        {
            float aspect = (float)targetCamera.pixelWidth / targetCamera.pixelHeight;
            targetCamera.orthographicSize = Mathf.Max(minOrthographicSize, designWidth * HALF / aspect);
        }
    }
}
