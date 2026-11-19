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
        [SerializeField] private float[] waitOverrides;      // ポイントごとの待ち時間（0 以下なら waitSeconds）。map.md：おかみは裏口で長く見張る
        [SerializeField] private float turnSpeed = 200f;     // 歩きながら向きを変える速さ（度/秒）
        [SerializeField] private float pivotOffset = 0.3f;   // 見回すとき、体の少し前を軸に回る（足を踏みかえるように見せる）

        private NavMeshAgent agent;
        private int index;
        private int direction = 1;
        private bool waiting;
        private float waitElapsed;
        private float baseYaw;
        private float pausedUntil;
        private bool paused;
        private Vector3 basePosition;
        private float currentYaw;
        private Vector3? faceTarget;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.updateRotation = false; // 向きはこのスクリプトで少しずつ変える
            currentYaw = transform.eulerAngles.y;
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

        // 止まっている間、この位置の方へ素早く（でも一瞬ではなく）振り向く。見つけたプレイヤーの方を見るときに使う
        public void FaceTowards(Vector3 position)
        {
            faceTarget = position;
        }

        private void Update()
        {
            if (paused)
            {
                if (faceTarget.HasValue)
                {
                    Vector3 to = faceTarget.Value - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.001f)
                    {
                        float target = Quaternion.LookRotation(to).eulerAngles.y;
                        currentYaw = Mathf.MoveTowardsAngle(currentYaw, target, turnSpeed * 2.5f * Time.deltaTime);
                        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
                    }
                }
                if (Time.time < pausedUntil) return;
                paused = false;
                faceTarget = null;
                if (agent.isOnNavMesh) agent.isStopped = false;
                if (waiting) { baseYaw = currentYaw; basePosition = transform.position; } // 見回しの途中だったら今の向きから続ける
            }
            if (waypoints == null || waypoints.Length == 0 || !agent.isOnNavMesh) return;

            if (waiting)
            {
                LookAround();
                return;
            }

            Steer();

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                waiting = true;
                waitElapsed = 0f;
                baseYaw = currentYaw;
                basePosition = transform.position;
            }
        }

        // 歩きながら少しずつ向きを変える。向きがずれている間は遅く歩くので、その場で回らず弧を描いて曲がる
        private void Steer()
        {
            Vector3 desired = agent.desiredVelocity;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.0001f) return;

            float targetYaw = Quaternion.LookRotation(desired).eulerAngles.y;
            currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

            float diff = Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw));
            float pace = Mathf.Lerp(1f, 0.3f, Mathf.InverseLerp(10f, 120f, diff));
            Vector3 forward = transform.forward;
            Vector3 blended = Vector3.Slerp(forward, desired.normalized, 0.35f) * desired.magnitude * pace;
            agent.velocity = blended;
        }

        // 左を見る → 少し止まる → 右を見る → 少し止まる → 前に戻す。視界の向きが変わるので、プレイヤーに隙ができる
        // 体の少し前を軸に回るので、足元がわずかに動く（その場でくるっと回らない）
        private void LookAround()
        {
            waitElapsed += Time.deltaTime;
            float wait = CurrentWait();
            float t = wait > 0f ? waitElapsed / wait : 1f;
            float k = LookCurve(Mathf.Clamp01(t));
            float yaw = baseYaw + k * lookAroundAngle;
            SetPose(yaw);

            if (t < 1f) return;
            SetPose(baseYaw);
            waiting = false;
            GoTo(NextIndex());
        }

        private void SetPose(float yaw)
        {
            currentYaw = yaw;
            Quaternion baseRot = Quaternion.Euler(0f, baseYaw, 0f);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 pivot = basePosition + baseRot * Vector3.forward * pivotOffset;
            Vector3 target = pivot - rot * Vector3.forward * pivotOffset;
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.000001f) agent.Move(delta);
            transform.rotation = rot;
        }

        // 0 → 左(-1) で止まる → 右(+1) で止まる → 0
        private static float LookCurve(float t)
        {
            if (t < 0.18f) return -Mathf.SmoothStep(0f, 1f, t / 0.18f);
            if (t < 0.38f) return -1f;
            if (t < 0.62f) return Mathf.Lerp(-1f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.38f) / 0.24f));
            if (t < 0.82f) return 1f;
            return 1f - Mathf.SmoothStep(0f, 1f, (t - 0.82f) / 0.18f);
        }

        private float CurrentWait()
        {
            if (waitOverrides != null && index < waitOverrides.Length && waitOverrides[index] > 0f) return waitOverrides[index];
            return waitSeconds;
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
