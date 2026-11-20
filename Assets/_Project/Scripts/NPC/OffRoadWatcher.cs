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
        [SerializeField] private float graceSeconds = 0f;       // 道からはみ出したらすぐ「道から外れた」
        [SerializeField] private int erodeCells = 3;            // 焼いた mask は道の外へ 1m 広げてあるので、実行時にこのマス数だけ削って道の幅に近づける

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

        private void Awake()
        {
            if (mask == null || mask.Length != width * height || erodeCells <= 0) return;
            // 道の外側にはみ出した分を削る（縁から erodeCells マス以内の道マスを「道でない」にする）
            var src = mask;
            var dst = new byte[src.Length];
            int r = erodeCells;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (src[y * width + x] == 0) continue;
                    bool keep = true;
                    for (int dy = -r; dy <= r && keep; dy++)
                        for (int dx = -r; dx <= r; dx++)
                        {
                            if (dx * dx + dy * dy > r * r) continue;
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= width || yy >= height || src[yy * width + xx] == 0) { keep = false; break; }
                        }
                    if (keep) dst[y * width + x] = 1;
                }
            // 拾う物・お題の場所・作業台のまわり（元から道あつかいの所）は削らない
            int keepR = Mathf.CeilToInt(3f / cellSize);
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (!(mb is Team5.Player.IInteractable)) continue;
                int cx = Mathf.FloorToInt((mb.transform.position.x - origin.x) / cellSize);
                int cy = Mathf.FloorToInt((mb.transform.position.z - origin.y) / cellSize);
                for (int dy = -keepR; dy <= keepR; dy++)
                    for (int dx = -keepR; dx <= keepR; dx++)
                    {
                        int xx = cx + dx, yy = cy + dy;
                        if (xx < 0 || yy < 0 || xx >= width || yy >= height || dx * dx + dy * dy > keepR * keepR) continue;
                        if (src[yy * width + xx] != 0) dst[yy * width + xx] = 1;
                    }
            }
            mask = dst;
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
