using UnityEngine;

namespace FishJam.UI
{
    /// <summary>Co RectTransform vao Screen.safeArea (tranh tai tho / thanh he thong).</summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect last;
        void Update()
        {
            var sa = Screen.safeArea; if (sa == last || Screen.width == 0) return; last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
