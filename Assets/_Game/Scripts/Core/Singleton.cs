using UnityEngine;

namespace FishJam
{
    /// <summary>
    /// Lop cha Singleton (chep tu Bottle_Up): moi loai chi co 1 object, goi bang XXX.Instance.
    /// Tim san trong scene; khong co thi tu tao moi va BAO CANH BAO (de biet minh quen dat vao scene).
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<T>();
                    if (instance == null)
                    {
                        Debug.LogWarning("[Singleton] Khong co " + typeof(T).Name + " trong scene, tu tao moi.");
                        instance = new GameObject(typeof(T).Name).AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        /// <summary>True neu da co object trong scene (khong tu tao). Dung cho scene thu UI.</summary>
        public static bool Exists => instance != null || Object.FindFirstObjectByType<T>() != null;

        protected virtual void Awake()
        {
            if (instance == null) instance = this as T;
            else if (instance != this) Destroy(gameObject);
        }
    }
}
