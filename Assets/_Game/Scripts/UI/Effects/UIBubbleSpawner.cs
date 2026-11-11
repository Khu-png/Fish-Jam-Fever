using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FishJam.UI
{
    /// <summary>Bong bong noi tu day len, lac lu. Gan len 1 RectTransform phu toan man.</summary>
    public class UIBubbleSpawner : MonoBehaviour
    {
        public Sprite bubble;
        public int count = 20;
        public Vector2 sizeRange = new Vector2(16, 60);
        public Vector2 speedRange = new Vector2(120, 260);
        class B 
        { 
            public RectTransform rt; 
            public float speed, wobble, phase, x; 
        }
        readonly List<B> list = new List<B>();
        RectTransform area;
        void Start()
        {
            area = (RectTransform)transform;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>(); 
                img.sprite = bubble; 
                img.raycastTarget = false; 
                img.color = new Color(1, 1, 1, 0.75f);
                var b = new B { rt = (RectTransform)go.transform };
                Respawn(b, Random.Range(0f, area.rect.height));
                list.Add(b);
            }
        }
        void Respawn(B b, float y)
        {
            float s = Random.Range(sizeRange.x, sizeRange.y);
            b.rt.sizeDelta = new Vector2(s, s);
            b.speed = Random.Range(speedRange.x, speedRange.y);
            b.wobble = Random.Range(8f, 20f); b.phase = Random.Range(0f, 6.28f);
            b.x = Random.Range(area.rect.xMin, area.rect.xMax);
            b.rt.anchoredPosition = new Vector2(b.x, area.rect.yMin + y - 60f);
        }
        void Update()
        {
            foreach (var b in list)
            {
                var p = b.rt.anchoredPosition; 
                p.y += b.speed * Time.deltaTime;
                p.x = b.x + Mathf.Sin(Time.time * 2f + b.phase) * b.wobble;
                b.rt.anchoredPosition = p;
                if (p.y > area.rect.yMax + 60f) Respawn(b, 0f);
            }
        }
    }
}
