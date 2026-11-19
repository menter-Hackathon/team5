using System;
using Team5.Core;
using UnityEngine;

namespace Team5.NPC
{
    // NPC の視界。プレイヤーが見えているかを判定し、足元に扇形の視界を描く
    public class VisionSensor : MonoBehaviour
    {
        [SerializeField] private float viewDistance = 8f;
        [SerializeField] private float viewAngle = 90f;      // 左右合計
        [SerializeField] private float eyeHeight = 1.5f;
        [SerializeField] private LayerMask obstacleMask;     // Obstacle
        [SerializeField] private float checkInterval = 0.1f;

        [Header("視界の表示")]
        [SerializeField] private Material coneMaterial;      // 空なら Sprites/Default で作る
        [SerializeField] private Color idleColor = new(1f, 0.85f, 0.2f, 0.25f);
        [SerializeField] private Color seeingColor = new(0.9f, 0.15f, 0.15f, 0.35f);
        [SerializeField] private int coneSegments = 24;
        [SerializeField] private float coneHeight = 0.05f;

        private const float PlayerChestHeight = 1.0f;

        public bool CanSeePlayer { get; private set; }
        public event Action<bool> OnSightChanged;

        private float nextCheckTime;
        private Mesh coneMesh;
        private Vector3[] coneVertices;
        private Color[] coneColors;
        private bool registered;

        private void Awake()
        {
            BuildCone();
        }

        private void OnEnable()
        {
            TryRegister();
        }

        private void Start()
        {
            // GameSceneContext より先に OnEnable が呼ばれたときのため
            TryRegister();
        }

        private void OnDisable()
        {
            var context = GameSceneContext.Instance;
            if (registered && context != null && context.Surveillance != null) context.Surveillance.Unregister(this);
            registered = false;
        }

        private void TryRegister()
        {
            if (registered) return;
            var context = GameSceneContext.Instance;
            if (context == null || context.Surveillance == null) return;
            context.Surveillance.Register(this);
            registered = true;
        }

        private void Update()
        {
            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + checkInterval;

            bool canSee = CheckSight();
            if (canSee != CanSeePlayer)
            {
                CanSeePlayer = canSee;
                OnSightChanged?.Invoke(canSee);
            }
            UpdateCone();
        }

        private bool CheckSight()
        {
            var context = GameSceneContext.Instance;
            if (context == null || context.Player == null) return false;

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 chest = context.Player.position + Vector3.up * PlayerChestHeight;

            Vector3 flat = chest - transform.position;
            flat.y = 0f;
            if (flat.magnitude > viewDistance) return false;
            if (Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;

            Vector3 toChest = chest - eye;
            return !Physics.Raycast(eye, toChest.normalized, toChest.magnitude, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private void BuildCone()
        {
            var cone = new GameObject("VisionCone");
            cone.transform.SetParent(transform, false);
            cone.transform.localPosition = Vector3.up * coneHeight;

            coneMesh = new Mesh { name = "VisionCone" };
            coneMesh.MarkDynamic();
            coneVertices = new Vector3[coneSegments + 2];
            coneColors = new Color[coneSegments + 2];
            var triangles = new int[coneSegments * 3];
            for (int i = 0; i < coneSegments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            coneMesh.vertices = coneVertices;
            coneMesh.triangles = triangles;

            cone.AddComponent<MeshFilter>().sharedMesh = coneMesh;
            var meshRenderer = cone.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = coneMaterial != null ? coneMaterial : new Material(Shader.Find("Sprites/Default"));
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            UpdateCone();
        }

        // 壁に当たったところで扇を切って、死角が見えるようにする
        private void UpdateCone()
        {
            if (coneMesh == null) return;

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            float half = viewAngle * 0.5f;
            coneVertices[0] = Vector3.zero;
            for (int i = 0; i <= coneSegments; i++)
            {
                float angle = -half + viewAngle * i / coneSegments;
                Vector3 localDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 worldDir = transform.TransformDirection(localDir);
                worldDir.y = 0f;
                float distance = viewDistance;
                if (Physics.Raycast(eye, worldDir.normalized, out var hit, viewDistance, obstacleMask, QueryTriggerInteraction.Ignore))
                    distance = hit.distance;
                coneVertices[i + 1] = localDir * distance;
            }

            Color color = CanSeePlayer ? seeingColor : idleColor;
            if (CanSeePlayer) color.a *= 0.75f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 6f)); // 見られている間は脈打つ
            for (int i = 0; i < coneColors.Length; i++) coneColors[i] = color;

            coneMesh.vertices = coneVertices;
            coneMesh.colors = coneColors;
            coneMesh.RecalculateBounds();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Application.isPlaying && CanSeePlayer ? Color.red : Color.yellow;
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            float half = viewAngle * 0.5f;
            Vector3 left = Quaternion.Euler(0f, -half, 0f) * transform.forward * viewDistance;
            Vector3 right = Quaternion.Euler(0f, half, 0f) * transform.forward * viewDistance;
            Gizmos.DrawLine(origin, origin + left);
            Gizmos.DrawLine(origin, origin + right);
            const int steps = 16;
            Vector3 previous = origin + left;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 next = origin + Quaternion.Euler(0f, -half + viewAngle * i / steps, 0f) * transform.forward * viewDistance;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
