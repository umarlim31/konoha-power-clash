using UnityEngine;

namespace Konoha.Campaign
{
    // 0.2.5: falling water (Istana cascades). Scrolls the texture of the given renderers
    // downward with a property block (no material copies) and pulses the foam at the foot.
    public sealed class CampaignWaterFlow : MonoBehaviour
    {
        public Renderer[] falls = new Renderer[0];
        public Transform[] foam = new Transform[0];
        public Vector2 tiling = new Vector2(1f, 3f);
        public float speed = 1.1f;

        private MaterialPropertyBlock block;
        private Vector3[] foamScale = new Vector3[0];
        private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            foamScale = new Vector3[foam.Length];
            for (int i = 0; i < foam.Length; i++)
                if (foam[i] != null)
                    foamScale[i] = foam[i].localScale;
        }

        private void Update()
        {
            float t = Time.time;
            // Texture v runs up the strip; increasing the offset makes the water fall.
            var st = new Vector4(tiling.x, tiling.y, 0f, Mathf.Repeat(t * speed, 1f));
            for (int i = 0; i < falls.Length; i++)
            {
                if (falls[i] == null) continue;
                falls[i].GetPropertyBlock(block);
                block.SetVector(BaseMapST, st);
                falls[i].SetPropertyBlock(block);
            }
            for (int i = 0; i < foam.Length; i++)
                if (foam[i] != null)
                    foam[i].localScale = foamScale[i] * (1f + .12f * Mathf.Sin(t * 7f + i * 1.9f));
        }
    }
}
