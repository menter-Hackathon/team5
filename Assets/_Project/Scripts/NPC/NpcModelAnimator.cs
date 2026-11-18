using UnityEngine;
using UnityEngine.AI;

namespace Team5.NPC
{
    // NPC のモデルを、歩いている間だけ歩きモーションで動かす。止まったら立ち姿に戻す
    public class NpcModelAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string walkState = "Walking";
        [SerializeField] private float moveThreshold = 0.1f;
        [SerializeField] private float speedToAnim = 0.8f; // 移動の速さ → 再生速度

        private NavMeshAgent agent;
        private bool walking;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null || agent == null) return;
            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            bool moving = speed > moveThreshold;
            if (moving)
            {
                animator.speed = Mathf.Clamp(speed * speedToAnim, 0.6f, 1.6f);
                walking = true;
            }
            else if (walking)
            {
                animator.Play(walkState, 0, 0f); // 1 コマ目（足をそろえた姿）で止める
                animator.speed = 0f;
                walking = false;
            }
        }
    }
}
