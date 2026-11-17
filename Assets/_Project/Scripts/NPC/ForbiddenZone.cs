using Team5.Core;
using UnityEngine;

namespace Team5.NPC
{
    // 「決められた道」から外れた場所。見られている間に中にいると「らしくない行動」になる
    [RequireComponent(typeof(BoxCollider))]
    public class ForbiddenZone : MonoBehaviour
    {
        [SerializeField] private float reportInterval = 0.5f;

        // プレイヤーがどれかの禁止ゾーンに入っているか（画面の「道から外れている」表示に使う）
        public static bool IsPlayerInside => insideCount > 0;
        private static int insideCount;

        private bool playerInside;
        private bool punished;   // この入場ですでに罰を受けたか（npc.md：1 回入るごとに 1 回まで）
        private float nextReportTime;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playerInside || !IsPlayer(other)) return;
            playerInside = true;
            insideCount++;
            punished = false;
            nextReportTime = Time.time;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!playerInside || !IsPlayer(other)) return;
            Leave();
        }

        private void OnDisable()
        {
            if (playerInside) Leave();
        }

        private void Leave()
        {
            playerInside = false;
            insideCount = Mathf.Max(0, insideCount - 1);
        }

        private void Update()
        {
            if (!playerInside) return;
            var surveillance = GameSceneContext.Instance != null ? GameSceneContext.Instance.Surveillance : null;
            if (surveillance == null) return;

            // 一度見られていない状態になったら、また罰を受けうる
            if (!surveillance.IsPlayerWatched) punished = false;
            if (punished || Time.time < nextReportTime) return;

            nextReportTime = Time.time + reportInterval;
            if (surveillance.ReportAction(true)) punished = true;
        }

        private static bool IsPlayer(Collider other)
        {
            var context = GameSceneContext.Instance;
            if (context == null || context.Player == null) return false;
            return other.transform == context.Player || other.transform.IsChildOf(context.Player);
        }

        // Scene ビューだけで見える半透明の赤
        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
