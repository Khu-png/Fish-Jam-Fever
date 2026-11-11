using UnityEngine;

namespace FishJam.UI
{
    /// <summary>
    /// Parent of the screens that use Quan's art (Resources/UI/Canvas_*). They are normal UICanvas screens opened by the
    /// game's UIManager; the only extra is the camera of Screen Space - Camera canvases (a prefab cannot reference it).
    /// </summary>
    public class SkinCanvas : UICanvas
    {
        protected virtual void Awake()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (canvas.isRootCanvas)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                {
                    canvas.worldCamera = Camera.main;
                }
            }
            else
            {
                // Under the scene's canvas (the usual case): fill it, like the game's own screens do. A canvas prefab
                // saved as a root has zero-size anchors, which would collapse everything in it.
                RectTransform rect = (RectTransform)transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
        }
    }
}
