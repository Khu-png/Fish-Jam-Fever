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
            float aspect = (float)Screen.width / Screen.height;
            targetCamera.orthographicSize = Mathf.Max(minOrthographicSize, designWidth * HALF / aspect);
        }
    }
}
