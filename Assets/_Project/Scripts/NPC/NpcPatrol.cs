using UnityEngine;
using UnityEngine.AI;

namespace Team5.NPC
{
    // 決まったルートを歩き、各点で止まって左右を見回す
    [RequireComponent(typeof(NavMeshAgent))]
    public class NpcPatrol : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waitSeconds = 2f;   // 各点での待ち時間
        [SerializeField] private bool loop = true;         // false なら最後まで行ったら逆順
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float lookAroundAngle = 60f; // 待っている間に左右へ振る角度

        private NavMeshAgent agent;
        private int index;
        private int direction = 1;
        private bool waiting;
        private float waitElapsed;
        private float baseYaw;
        private float pausedUntil;
        private bool paused;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
        }

        private void Start()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            GoTo(0);
        }

        // プレイヤーと会話しているときなどに止める
        public void Pause(float seconds)
        {
            pausedUntil = Mathf.Max(pausedUntil, Time.time + seconds);
            paused = true;
            if (agent.isOnNavMesh) agent.isStopped = true;
        }

        private void Update()
        {
            if (paused)
            {
                if (Time.time < pausedUntil) return;
                paused = false;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
            if (waypoints == null || waypoints.Length == 0 || !agent.isOnNavMesh) return;

            if (waiting)
            {
                LookAround();
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                waiting = true;
                waitElapsed = 0f;
                baseYaw = transform.eulerAngles.y;
                agent.updateRotation = false;
            }
        }

        // ±lookAroundAngle をゆっくり1往復する。視界の向きが変わるので、プレイヤーに隙ができる
        private void LookAround()
        {
            waitElapsed += Time.deltaTime;
            float t = waitSeconds > 0f ? waitElapsed / waitSeconds : 1f;
            float yaw = baseYaw + Mathf.Sin(t * Mathf.PI * 2f) * lookAroundAngle;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (t < 1f) return;
            transform.rotation = Quaternion.Euler(0f, baseYaw, 0f);
            waiting = false;
            agent.updateRotation = true;
            GoTo(NextIndex());
        }

        private int NextIndex()
        {
            if (waypoints.Length == 1) return 0;
            if (loop) return (index + 1) % waypoints.Length;

            int next = index + direction;
            if (next < 0 || next >= waypoints.Length)
            {
                direction = -direction;
                next = index + direction;
            }
            return next;
        }

        private void GoTo(int next)
        {
            index = next;
            if (waypoints[index] != null && agent.isOnNavMesh) agent.SetDestination(waypoints[index].position);
        }
    }
}
