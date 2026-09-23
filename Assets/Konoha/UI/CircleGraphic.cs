using UnityEngine;
using UnityEngine.UI;

namespace Konoha.UI
{
    public sealed class CircleGraphic : MaskableGraphic
    {
        [Range(12, 64)]
        public int segments = 40;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            int count = Mathf.Clamp(segments, 12, 64);
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            Vector2 center = rect.center;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center;
            vertex.uv0 = new Vector2(0.5f, 0.5f);
            vertexHelper.AddVert(vertex);

            for (int i = 0; i <= count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                vertex.position = center + direction * radius;
                vertex.uv0 = direction * 0.5f + Vector2.one * 0.5f;
                vertexHelper.AddVert(vertex);
            }

            for (int i = 0; i < count; i++)
                vertexHelper.AddTriangle(0, i + 1, i + 2);
        }
    }
}
