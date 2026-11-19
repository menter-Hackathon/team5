using Team5.Core;
using UnityEngine;

namespace Team5.NPC
{
    // 「決められた道」から外れているか。村の道（いろいろの村の道と広場）から少し離れると「道から外れた」になり、
    // 見られている間にそこにいると「らしくない行動」になる（ForbiddenZone と同じ扱い）。
    // 道の形はエディターで焼いた mask（1 マス cellSize m）を使う。お題の場所・作業台・建物の中などは mask で道あつかいにしてある
    public class OffRoadWatcher : MonoBehaviour
    {
        [SerializeField] private Vector2 origin = new Vector2(-21f, -21f); // mask の左下（x, z）
        [SerializeField] private float cellSize = 0.25f;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField, HideInInspector] private byte[] mask; // 1 = 道（歩いてよい）
        [SerializeField] private float reportInterval = 0.5f;
        [SerializeField] private float graceSeconds = 0.6f;     // 道の角を少しかすめたくらいでは反応しない

        public static bool IsOff { get; private set; }

        private bool punished;
        private float offTime;
        private float nextReportTime;

        public void SetMask(Vector2 origin, float cellSize, int width, int height, byte[] mask)
        {
            this.origin = origin; this.cellSize = cellSize; this.width = width; this.height = height; this.mask = mask;
        }

        public bool IsRoad(Vector3 world)
        {
            if (mask == null || mask.Length != width * height) return true;
            int x = Mathf.FloorToInt((world.x - origin.x) / cellSize);
            int y = Mathf.FloorToInt((world.z - origin.y) / cellSize);
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            return mask[y * width + x] != 0;
        }

        private void OnDisable() => IsOff = false;

        private void Update()
        {
            var context = GameSceneContext.Instance;
            if (context == null || context.Player == null || context.Surveillance == null) { IsOff = false; return; }

            bool off = !IsRoad(context.Player.position);
            offTime = off ? offTime + Time.deltaTime : 0f;
            IsOff = offTime >= graceSeconds;
            if (!IsOff) { punished = false; return; }

            var surveillance = context.Surveillance;
            if (!surveillance.IsPlayerWatched) punished = false;     // 一度見られていない状態になったら、また罰を受けうる
            if (punished || Time.time < nextReportTime) return;
            nextReportTime = Time.time + reportInterval;
            if (surveillance.ReportAction(true)) punished = true;
        }
    }
}
