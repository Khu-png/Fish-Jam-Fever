using System.Collections.Generic;
using UnityEngine;

namespace FishJam.UI
{
    /// <summary>
    /// UIManager (chep tu Bottle_Up): nap moi prefab UI trong Resources/UI, mo/dong theo KIEU.
    ///   UIManager.Instance.Open&lt;Setting&gt;();  GetUI&lt;GamePlay&gt;();  CloseDirectly&lt;MainMenu&gt;();  CloseAll();
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        readonly Dictionary<System.Type, UICanvas> canvasActives = new Dictionary<System.Type, UICanvas>();
        readonly Dictionary<System.Type, UICanvas> canvasPrefab = new Dictionary<System.Type, UICanvas>();

        [Tooltip("Object cha chua cac Canvas duoc tao ra (UIRoot)")]
        [SerializeField] Transform parent;

        protected override void Awake()
        {
            base.Awake();
            foreach (var p in Resources.LoadAll<UICanvas>("UI/")) canvasPrefab[p.GetType()] = p;
        }

        /// <summary>Mo UI kieu T (chua co thi tao tu prefab): SetUp() roi Open().</summary>
        public T Open<T>() where T : UICanvas
        {
            T canvas = GetUI<T>();
            if (canvas == null) return null;
            canvas.SetUp();
            canvas.Open();
            return canvas;
        }

        public void Close<T>(float time) where T : UICanvas { if (IsLoaded<T>()) canvasActives[typeof(T)].Close(time); }
        public void CloseDirectly<T>() where T : UICanvas { if (IsLoaded<T>()) canvasActives[typeof(T)].CloseDirectly(); }
        public bool IsLoaded<T>() where T : UICanvas => canvasActives.TryGetValue(typeof(T), out var c) && c != null;
        public bool IsOpened<T>() where T : UICanvas => IsLoaded<T>() && canvasActives[typeof(T)].gameObject.activeSelf;

        /// <summary>Lay UI kieu T; chua co thi tao tu prefab (khong mo).</summary>
        public T GetUI<T>() where T : UICanvas
        {
            if (!IsLoaded<T>())
            {
                if (!canvasPrefab.TryGetValue(typeof(T), out var prefab) || prefab == null)
                {
                    Debug.LogError("[UIManager] Khong tim thay prefab UI kieu " + typeof(T).Name + " trong Resources/UI", this);
                    return null;
                }
                canvasActives[typeof(T)] = Instantiate(prefab, parent);
            }
            return canvasActives[typeof(T)] as T;
        }

        /// <summary>Dong moi UI dang mo.</summary>
        public void CloseAll()
        {
            foreach (var c in canvasActives.Values)
                if (c != null && c.gameObject.activeSelf) c.CloseDirectly();
        }
    }
}
