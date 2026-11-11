using UnityEngine;

namespace FishSort
{
    // A shallow translucent ribbon gives the water a visible meniscus and a responsive surface.
    public sealed class WaterSurface : MonoBehaviour
    {
        private const int POINTS = 25;
        [SerializeField] private Material surfaceMaterial;
        [SerializeField] private float width = 0.9f;
        [SerializeField] private float height = 0.35f;
        [SerializeField] private float depth = 0.045f;
        [SerializeField, Range(0f, 1f)] private float idleWaveStrength = 0.32f;
        [SerializeField, Min(0f)] private float idleWaveSpeed = 1.2f;
        [SerializeField] private int sortingOrder = FishSortConst.SORT_FISH_IN_TANK;

        private Mesh surfaceMesh;
        private Vector3[] vertices;
        private Vector3[] front;
        private Vector3[] back;
        private LineRenderer frontEdge;
        private LineRenderer backEdge;
        private float phase;
        private float reactionAge = 2f;
        private float reactionX;

        private void Awake()
        {
            var layer = new GameObject("SurfaceFill");
            layer.transform.SetParent(transform, false);
            surfaceMesh = new Mesh { name = "Water meniscus" };
            surfaceMesh.MarkDynamic();
            layer.AddComponent<MeshFilter>().sharedMesh = surfaceMesh;
            var renderer = layer.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = surfaceMaterial;
            renderer.sortingOrder = sortingOrder;
            vertices = new Vector3[POINTS * 2];
            front = new Vector3[POINTS];
            back = new Vector3[POINTS];
            var colors = new Color[vertices.Length];
            var triangles = new int[(POINTS - 1) * 6];
            for (int i = 0; i < POINTS; i++)
            {
                colors[i * 2] = new Color(0.24f, 0.8f, 1f, 0.12f);
                colors[i * 2 + 1] = new Color(0.56f, 0.94f, 1f, 0.28f);
                if (i == POINTS - 1) continue;
                int t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 1;
                triangles[t + 2] = i * 2 + 2;
                triangles[t + 3] = i * 2 + 1;
                triangles[t + 4] = i * 2 + 3;
                triangles[t + 5] = i * 2 + 2;
            }
            surfaceMesh.vertices = vertices;
            surfaceMesh.colors = colors;
            surfaceMesh.triangles = triangles;
            frontEdge = CreateEdge("SurfaceHighlight", new Color(0.85f, 0.98f, 1f, 0.78f), 0.012f);
            backEdge = CreateEdge("SurfaceBackEdge", new Color(0.4f, 0.88f, 1f, 0.36f), 0.008f);
            phase = Mathf.Repeat(GetInstanceID() * 0.618f, Mathf.PI * 2f);
            Animate();
        }

        private LineRenderer CreateEdge(string label, Color color, float thickness)
        {
            var edge = new GameObject(label).AddComponent<LineRenderer>();
            edge.transform.SetParent(transform, false);
            edge.useWorldSpace = false;
            edge.sharedMaterial = surfaceMaterial;
            edge.sortingOrder = sortingOrder;
            edge.positionCount = POINTS;
            edge.widthMultiplier = thickness;
            edge.startColor = color;
            edge.endColor = color;
            edge.numCapVertices = 2;
            return edge;
        }

        public void React(Vector3 worldPosition)
        {
            reactionX = Mathf.Clamp(transform.InverseTransformPoint(worldPosition).x, -width * 0.4f, width * 0.4f);
            reactionAge = 0f;
        }

        private void OnEnable() => reactionAge = 2f;

        private void Update()
        {
            phase += Time.deltaTime * idleWaveSpeed;
            reactionAge += Time.deltaTime;
            Animate();
        }

        private void Animate()
        {
            float reaction = 1f - Mathf.Clamp01(reactionAge / 0.8f);
            for (int i = 0; i < POINTS; i++)
            {
                float u = (float)i / (POINTS - 1);
                float x = (u - 0.5f) * width;
                float curve = Mathf.Sin(u * Mathf.PI);
                // Two slow crossing waves keep empty containers alive without repeating in lockstep.
                float idle = Mathf.Sin(u * 6f + phase) +
                    Mathf.Sin(u * 11f - phase * 0.73f) * 0.28f;
                float wave = idle * depth * idleWaveStrength;
                wave += Mathf.Cos(Mathf.Abs(x - reactionX) * 23f - reactionAge * 15f) * depth * 0.65f * reaction;
                float y = height + wave * curve;
                back[i] = new Vector3(x, y + curve * depth * 0.18f, 0f);
                front[i] = new Vector3(x, y - curve * depth * 0.82f, 0f);
                vertices[i * 2] = back[i];
                vertices[i * 2 + 1] = front[i];
            }
            surfaceMesh.vertices = vertices;
            surfaceMesh.RecalculateBounds();
            frontEdge.SetPositions(front);
            backEdge.SetPositions(back);
        }

        private void OnDestroy()
        {
            if (surfaceMesh != null) Destroy(surfaceMesh);
        }
    }
}
