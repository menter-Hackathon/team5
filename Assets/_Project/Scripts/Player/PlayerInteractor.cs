using System;
using Team5.Core;
using UnityEngine;

namespace Team5.Player
{
    // プレイヤーの前にある Interactable のうち一番近いものを注目し、E で Interact を呼ぶ
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMover))]
    public class PlayerInteractor : MonoBehaviour
    {
        private const string InteractableLayer = "Interactable";

        [SerializeField] private float range = 1.5f;        // 前方どこまで届くか（m）
        [SerializeField] private float sensorHeight = 0.8f; // 球の中心の高さ（胸のあたり）

        public IInteractable Current { get; private set; }
        public event Action<IInteractable> OnFocusChanged; // null も通知する

        // ほかの処理が CanMove を止められるように公開する
        public PlayerMover Mover { get; private set; }

        // E を押し続けているか（TaskSpot の長押しに使う）
        public bool IsHoldingInteract => input != null && input.InteractHeld;

        private readonly Collider[] hits = new Collider[16];
        private PlayerInputReader input;
        private int layerMask;

        private void Awake()
        {
            Mover = GetComponent<PlayerMover>();
            input = GetComponent<PlayerInputReader>();
            layerMask = LayerMask.GetMask(InteractableLayer);
        }

        private void OnEnable() => input.OnInteract += HandleInteract;

        private void OnDisable() => input.OnInteract -= HandleInteract;

        private void Update()
        {
            IInteractable next = FindNearest();
            if (!ReferenceEquals(next, Current))
            {
                Current = next;
                OnFocusChanged?.Invoke(Current);
            }
        }

        // 前方 range の範囲に届く球（中心は体の少し前）の中から、CanInteract が true の一番近いもの
        private IInteractable FindNearest()
        {
            Vector3 origin = transform.position + Vector3.up * sensorHeight;
            Vector3 center = origin + transform.forward * (range * 0.5f);
            int count = Physics.OverlapSphereNonAlloc(center, range * 0.75f, hits, layerMask, QueryTriggerInteraction.Collide);

            IInteractable nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var target = hits[i].GetComponentInParent<IInteractable>();
                if (target == null || !target.CanInteract(this)) continue;
                float sqr = (hits[i].ClosestPoint(origin) - origin).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = target;
                }
            }
            return nearest;
        }

        private void HandleInteract()
        {
            // Destroy された直後のものは使わない
            if (Current == null || (Current is UnityEngine.Object obj && obj == null)) return;

            var context = GameSceneContext.Instance;
            // npc.md：見つかったら、その行動はできない（拾えない）
            bool caught = context != null && context.Surveillance != null && context.Surveillance.ReportAction(Current.IsSuspicious);
            if (caught) return;
            Current.Interact(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = transform.position + Vector3.up * sensorHeight;
            Gizmos.DrawWireSphere(origin + transform.forward * (range * 0.5f), range * 0.75f);
        }
    }
}
