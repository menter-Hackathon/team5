using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.NPC;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 次に行く場所を、画面の上で矢印と距離で示す。
    // 黄色の「！」＝村長のお題（届け物の素材が足りなければ、その素材の場所）、ピンクの「★」＝目標の次の一歩（ObjectiveView）。
    // 画面の中に見えていれば、その場所の上に印を出す。画面の外なら、画面の端に矢印を出してその方向を指す
    public class GuideArrowView : MonoBehaviour
    {
        private class Pointer
        {
            public RectTransform root;
            public RectTransform arrow;
            public Text label;
            public Transform target;
            public string name;
        }

        private static readonly Color TaskColor = new Color(1f, 0.82f, 0.24f);
        private static readonly Color GoalColor = new Color(0.98f, 0.5f, 0.78f);

        private Font font;
        private GameSceneContext context;
        private RectTransform canvasRect;
        private Pointer task;
        private Pointer goal;
        private Sprite arrowSprite;
        private Sprite dotSprite;

        public void Init(Font font) { this.font = font; }

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context == null) { enabled = false; return; }
            arrowSprite = MakeArrow();
            dotSprite = MakeDot();

            var canvasGo = new GameObject("GuideArrowCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)canvasGo.transform;

            goal = NewPointer("Goal", GoalColor, "★");
            task = NewPointer("Task", TaskColor, "！");
        }

        private void LateUpdate()
        {
            if (context == null || context.Player == null || canvasRect == null) return;
            bool ended = GameManager.Instance != null && GameManager.Instance.State == GameState.Result;

            ResolveTask(out task.target, out task.name);
            goal.target = ObjectiveView.NextTarget;
            goal.name = "目標";
            // 同じ場所なら黄色だけ
            if (goal.target != null && task.target != null && (goal.target.position - task.target.position).sqrMagnitude < 1f) goal.target = null;

            Place(task, ended);
            Place(goal, ended);
        }

        // 今のお題で行く場所。届け物で素材が足りなければ、いちばん近いその素材
        private void ResolveTask(out Transform target, out string label)
        {
            target = null;
            label = "お題";
            var current = context.Tasks != null ? context.Tasks.Current : null;
            if (current == null || current.State != TaskState.Active || current.Definition == null) return;
            var def = current.Definition;
            switch (def.type)
            {
                case TaskType.ActionAtSpot:
                    foreach (var spot in FindObjectsByType<TaskSpot>(FindObjectsSortMode.None))
                        if (spot.SpotId == def.targetSpotId) { target = spot.transform; break; }
                    break;
                case TaskType.TalkToNpc:
                    foreach (var npc in FindObjectsByType<NpcTalkable>(FindObjectsSortMode.None))
                        if (npc.NpcId == def.targetSpotId) { target = npc.transform; break; }
                    break;
                case TaskType.BringItem:
                    int have = context.Inventory != null && def.requiredItem != null ? context.Inventory.Count(def.requiredItem) : 0;
                    if (def.requiredItem != null && have < def.requiredCount)
                    {
                        target = NearestPickup(def.requiredItem);
                        if (target != null) { label = $"{def.requiredItem.displayName} {have}/{def.requiredCount}"; break; }
                    }
                    var point = FindAnyObjectByType<DeliveryPoint>();
                    if (point != null) { target = point.transform; label = "届ける"; }
                    break;
            }
        }

        private Transform NearestPickup(ItemDefinition item)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            Vector3 from = context.Player.position;
            foreach (var pickup in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
            {
                if (pickup.Item != item) continue;
                float d = (pickup.transform.position - from).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = pickup.transform; }
            }
            return best;
        }

        private void Place(Pointer p, bool ended)
        {
            var cam = Camera.main;
            bool visible = !ended && p.target != null && p.target.gameObject.activeInHierarchy && cam != null;
            float distance = visible ? Vector3.Distance(Flat(context.Player.position), Flat(p.target.position)) : 0f;
            if (visible && distance < 2.2f) visible = false; // もう着いている
            if (p.root.gameObject.activeSelf != visible) p.root.gameObject.SetActive(visible);
            if (!visible) return;

            Vector3 world = p.target.position + Vector3.up * 2.2f;
            Vector3 screen = cam.WorldToScreenPoint(world);
            bool behind = screen.z < 0f;
            Vector2 size = canvasRect.rect.size;
            float scale = size.x / Mathf.Max(Screen.width, 1);
            Vector2 local = new Vector2(screen.x, screen.y) * scale - size * 0.5f;
            if (behind) local = -local;

            Vector2 half = size * 0.5f - new Vector2(90f, 110f);
            bool onScreen = !behind && Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y;
            float bob = Mathf.Sin(Time.unscaledTime * 4f) * 8f;
            if (onScreen)
            {
                p.root.anchoredPosition = local + new Vector2(0f, bob);
                p.arrow.localRotation = Quaternion.Euler(0f, 0f, 180f); // 下向き：ここ
                p.arrow.anchoredPosition = new Vector2(0f, -34f);
            }
            else
            {
                // 画面の外：中心からの方向で、画面の端に置く
                Vector2 dir = local.sqrMagnitude > 0.01f ? local.normalized : Vector2.up;
                float k = Mathf.Min(half.x / Mathf.Max(Mathf.Abs(dir.x), 0.001f), half.y / Mathf.Max(Mathf.Abs(dir.y), 0.001f));
                p.root.anchoredPosition = dir * k;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                p.arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
                p.arrow.anchoredPosition = dir * (40f + bob * 0.5f);
            }
            p.label.text = $"{p.name}  {Mathf.RoundToInt(distance)}m";
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private Pointer NewPointer(string name, Color color, string mark)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasRect, false);
            root.sizeDelta = new Vector2(10f, 10f);

            var dot = NewImage("Mark", root, dotSprite, color, 46f);
            var markText = NewText(mark, dot.rectTransform, 30, new Color(0.25f, 0.15f, 0f));
            markText.rectTransform.sizeDelta = new Vector2(46f, 46f);

            var arrow = NewImage("Arrow", root, arrowSprite, color, 34f);
            var label = NewText("", root, 24, color);
            label.rectTransform.sizeDelta = new Vector2(320f, 34f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 44f);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);

            root.gameObject.SetActive(false);
            return new Pointer { root = root, arrow = arrow.rectTransform, label = label };
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            return image;
        }

        private Text NewText(string value, Transform parent, int size, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        // 上向きの三角
        private static Sprite MakeArrow()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float t = (float)y / (n - 1);                 // 0 = 下, 1 = 上
                    float halfWidth = (1f - t) * (n * 0.5f - 2f);
                    bool inside = y > 4 && Mathf.Abs(x - (n - 1) * 0.5f) <= halfWidth;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeDot()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f - 1f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2((n - 1) * 0.5f, (n - 1) * 0.5f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
