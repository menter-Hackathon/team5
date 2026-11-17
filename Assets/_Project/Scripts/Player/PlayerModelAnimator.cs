using UnityEngine;

namespace Team5.Player
{
    // 仮のアニメーション。動いている間だけ走りモーションを再生し、止まったら止める（本番は AN-01）
    public class PlayerModelAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float moveThreshold = 0.1f; // これより速く動いていたら「走っている」

        private CharacterController controller;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null || controller == null) return;
            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            animator.speed = velocity.sqrMagnitude > moveThreshold * moveThreshold ? 1f : 0f;
        }
    }
}
