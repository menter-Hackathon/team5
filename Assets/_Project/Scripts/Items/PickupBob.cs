using UnityEngine;

namespace Team5.Items
{
    // 拾える物を、ふわふわ浮かせてゆっくり回し、足元に光の輪を出す（ここに拾える物があると分かるように）
    public class PickupBob : MonoBehaviour
    {
        [SerializeField] private float height = 0.12f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float turnDegreesPerSecond = 45f;
        [SerializeField] private Material glowMaterial;
        [SerializeField] private Color glowColor = new(1f, 0.784f, 0.239f, 0.45f); // 強調 #FFC83D

        private Transform model;
        private Vector3 basePosition;
        private float phase;
        private Transform glow;

        private void Start()
        {
            model = transform.Find("Model");
            if (model != null) basePosition = model.localPosition;
            phase = Random.value * 10f;
            if (glowMaterial != null) glow = MakeGlow();
        }

        private Transform MakeGlow()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Glow";
            quad.transform.SetParent(transform, false);
            quad.transform.position = new Vector3(transform.position.x, 0.03f, transform.position.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(1.1f / transform.lossyScale.x, 1.1f / transform.lossyScale.y, 1f);
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = glowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", glowColor);
            renderer.SetPropertyBlock(block);
            return quad.transform;
        }

        private void Update()
        {
            float t = Time.time * speed + phase;
            if (model != null)
            {
                model.localPosition = basePosition + Vector3.up * (Mathf.Sin(t) * 0.5f + 0.5f) * height / Mathf.Max(0.01f, transform.lossyScale.y);
                model.Rotate(0f, turnDegreesPerSecond * Time.deltaTime, 0f, Space.World);
            }
            if (glow != null)
            {
                float pulse = 1f + Mathf.Sin(t * 1.5f) * 0.12f;
                glow.localScale = new Vector3(1.1f * pulse / transform.lossyScale.x, 1.1f * pulse / transform.lossyScale.y, 1f);
            }
        }
    }
}
