using UnityEngine;
using UnityEngine.UI;

namespace FishSort
{
    // The game is designed for a portrait screen. On a wider screen (a computer window, a tablet or an emulator turned
    // sideways) it is drawn in a portrait-shaped area in the middle with plain bars at the sides, instead of being stretched
    // or showing far too much of the field. Portrait phones are not touched.
    [DefaultExecutionOrder(-200)]
    public sealed class PortraitViewport : MonoBehaviour
    {
        private const float DESIGN_WIDTH = 1080f;
        private const float BARS_DEPTH = -100f;
        private const float HALF = 0.5f;
        private static readonly Color BarColor = new Color32(4, 24, 70, 255);

        [Tooltip("The widest screen (width / height) that is used as it is. Wider screens get side bars.")]
        [SerializeField] private float maxAspect = 0.75f;

        private Camera barsCamera;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            Apply();
        }

        // Resizing a window or turning the device changes the screen: the layout follows.
        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                Apply();
            }
        }

        private void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float aspect = (float)Screen.width / Screen.height;
            bool isWide = aspect > maxAspect;
            float width = isWide ? maxAspect / aspect : 1f;
            Rect area = new Rect((1f - width) * HALF, 0f, width, 1f);
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != barsCamera)
                {
                    cameras[i].rect = area;
                }
            }

            ShowBars(isWide);
            FitUi(isWide ? Screen.width * width : 0f);
            CameraFit fit = FindFirstObjectByType<CameraFit>();
            if (fit != null)
            {
                fit.Refit();
            }
        }

        // A camera that draws nothing but the colour of the bars, before every other camera.
        private void ShowBars(bool isVisible)
        {
            if (barsCamera == null && isVisible)
            {
                GameObject bars = new GameObject("BarsCamera");
                bars.transform.SetParent(transform, false);
                barsCamera = bars.AddComponent<Camera>();
                barsCamera.clearFlags = CameraClearFlags.SolidColor;
                barsCamera.backgroundColor = BarColor;
                barsCamera.cullingMask = 0;
                barsCamera.depth = BARS_DEPTH;
            }

            if (barsCamera != null)
            {
                barsCamera.gameObject.SetActive(isVisible);
            }
        }

        // The canvas scaler works from the whole screen, not from the drawn area, so in a narrow area the screens are scaled
        // here from the width of that area (viewportWidth in pixels); 0 gives the normal scaling back.
        private static void FitUi(float viewportWidth)
        {
            CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < scalers.Length; i++)
            {
                Canvas canvas = scalers[i].GetComponent<Canvas>();
                if (canvas == null || !canvas.isRootCanvas)
                {
                    continue;
                }

                if (viewportWidth > 0f)
                {
                    scalers[i].uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scalers[i].scaleFactor = viewportWidth / DESIGN_WIDTH;
                }
                else
                {
                    scalers[i].uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                }
            }
        }
    }
}
