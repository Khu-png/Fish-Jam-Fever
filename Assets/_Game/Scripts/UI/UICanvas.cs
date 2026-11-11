using System.Collections;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>
    /// Lop cha cua moi man UI (MainMenu, GamePlay, Setting...). Prefab dat trong Resources/UI, goc prefab gan lop con.
    /// UIManager goi SetUp() roi Open(). Sua so voi Bottle_Up:
    ///  - Awake protected virtual (lop con override duoc);
    ///  - Close(time) dung thoi gian that (chay ca khi Time.timeScale = 0);
    ///  - hieu ung mo/dong (dim/panel) khong nam o day: chi Setting dung, xem Setting.cs;
    ///  - tai tho: dung SafeAreaFitter ben trong prefab (khong doan theo ti le man).
    /// </summary>
    public class UICanvas : MonoBehaviour
    {
        [SerializeField] bool isDestroyOnClose = false;

        protected virtual void Awake()
        {
            // Canvas ScreenSpaceCamera tao tu prefab chua co camera -> lay Camera.main
            var c = GetComponent<Canvas>();
            if (c && c.renderMode == RenderMode.ScreenSpaceCamera && !c.worldCamera) c.worldCamera = Camera.main;
        }

        /// <summary>Dien du lieu truoc khi mo. Lop con override.</summary>
        public virtual void SetUp() { }

        public virtual void Open()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Dong sau 'time' giay (thoi gian that).</summary>
        public virtual void Close(float time)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(CloseAfter(time));
        }

        /// <summary>Dong ngay. Moi duong dong deu di qua day.</summary>
        public virtual void CloseDirectly()
        {
            if (isDestroyOnClose) Destroy(gameObject); else gameObject.SetActive(false);
        }

        IEnumerator CloseAfter(float t)
        {
            yield return new WaitForSecondsRealtime(t);
            CloseDirectly();
        }
    }
}

