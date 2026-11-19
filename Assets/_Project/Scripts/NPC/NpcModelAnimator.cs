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

        [SerializeField] private float turnStepSpeed = 0.55f; // その場で向きを変えるときの足踏みの再生速度

        private NavMeshAgent agent;
        private bool walking;
        private float lastYaw;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            lastYaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (animator == null || agent == null) return;
            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            bool moving = speed > moveThreshold;
            float yaw = transform.eulerAngles.y;
            float turnRate = Time.deltaTime > 0f ? Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) / Time.deltaTime : 0f;
            lastYaw = yaw;
            bool turning = turnRate > 25f; // 見回しで向きを変えている間は足踏みする
            if (moving)
            {
                animator.speed = Mathf.Clamp(speed * speedToAnim, 0.6f, 1.6f);
                walking = true;
            }
            else if (turning)
            {
                animator.speed = turnStepSpeed;
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
