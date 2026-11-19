using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Team5.Core
{
    // プレイヤーが建物の中にいる間だけ、その建物の屋根を消す（影は残す）。屋根で中が見えなくならないように
    public class RoofHider : MonoBehaviour
    {
        [System.Serializable]
        public class Roof
        {
            public string name;
            public Renderer[] renderers;
            public Vector2 center; // 床の範囲（x, z）
            public Vector2 size;
        }

        [SerializeField] private List<Roof> roofs = new List<Roof>();
        [SerializeField] private float margin = 0.3f;

        private readonly Dictionary<Roof, bool> hidden = new Dictionary<Roof, bool>();

        public void Add(string roofName, Renderer[] renderers, Vector2 center, Vector2 size)
        {
            roofs.Add(new Roof { name = roofName, renderers = renderers, center = center, size = size });
        }

        private void LateUpdate()
        {
            var context = GameSceneContext.Instance;
            if (context == null || context.Player == null) return;
            Vector3 p = context.Player.position;
            foreach (var roof in roofs)
            {
                Vector2 half = roof.size * 0.5f + Vector2.one * margin;
                bool inside = Mathf.Abs(p.x - roof.center.x) < half.x && Mathf.Abs(p.z - roof.center.y) < half.y;
                if (hidden.TryGetValue(roof, out bool was) && was == inside) continue;
                hidden[roof] = inside;
                foreach (var r in roof.renderers)
                    if (r != null) r.shadowCastingMode = inside ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }
    }
}
