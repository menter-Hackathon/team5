using System.Collections.Generic;
using System.Text;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 画面の右に「目標」と「次にやること」を出す。初めて遊ぶ人が、何をすればゴールに近づくか迷わないように。
    // ロールのレシピ（RoleDefinition.recipes）と持ち物から、毎回いちばん近い一歩を計算する。地図の★（MapView）も同じ場所を指す
    public class ObjectiveView : MonoBehaviour
    {
        // 次に行く場所（なければ null）。MapView が★を出す
        public static Transform NextTarget { get; private set; }
        // 次にやること（説明のカードからも使う）
        public static string NextText { get; private set; } = "";

        private static readonly (string name, Vector2 pos)[] Areas =
        {
            ("村長の家", new Vector2(0f, 13f)), ("広場", new Vector2(0f, 0f)), ("食堂", new Vector2(-11.5f, 3f)),
            ("台所", new Vector2(-18.5f, 2f)), ("鍛冶場", new Vector2(-11f, 13.5f)), ("図書館", new Vector2(-18f, 17.5f)),
            ("わが家", new Vector2(-11.5f, -13.5f)), ("納屋", new Vector2(-17.5f, -18f)), ("井戸", new Vector2(11f, 3f)),
            ("訓練場", new Vector2(11f, 11f)), ("畑の奥", new Vector2(7f, -16.5f)), ("森の入口", new Vector2(15f, -6f)),
            ("森の奥", new Vector2(18f, -16f)),
        };

        private Font font;
        private Sprite panelSprite;
        private GameSceneContext context;
        private RoleDefinition role;
        private readonly HashSet<RecipeDefinition> crafted = new HashSet<RecipeDefinition>();
        private Text body;
        private Text next;
        private RectTransform panel;
        private float refresh;
        private string lastNext;

        public void Init(Font font, Sprite panelSprite)
        {
            this.font = font;
            this.panelSprite = panelSprite;
        }

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context == null) { enabled = false; return; }
            role = context.Role != null ? context.Role : GameManager.Instance != null ? GameManager.Instance.CurrentRole : null;
            if (role == null || role.recipes == null || role.recipes.Count == 0) { enabled = false; return; }
            if (context.Crafting != null) context.Crafting.OnCrafted += OnCrafted;
            if (context.Inventory != null) context.Inventory.OnChanged += Refresh;
            BuildUi();
            Refresh();
        }

        private void OnDestroy()
        {
            if (context != null && context.Crafting != null) context.Crafting.OnCrafted -= OnCrafted;
            if (context != null && context.Inventory != null) context.Inventory.OnChanged -= Refresh;
            NextTarget = null;
            NextText = "";
        }

        private void OnCrafted(RecipeDefinition recipe)
        {
            crafted.Add(recipe);
            Refresh();
        }

        private void Update()
        {
            // 拾った素材が消えたときなどにも追いつくように、ときどき計算し直す
            refresh -= Time.unscaledDeltaTime;
            if (refresh <= 0f) Refresh();
            if (panel != null)
            {
                bool ended = GameManager.Instance != null && GameManager.Instance.State == GameState.Result;
                if (panel.gameObject.activeSelf == ended) panel.gameObject.SetActive(!ended);
            }
        }

        private void Refresh()
        {
            refresh = 0.5f;
            if (body == null) return;
            var inventory = context.Inventory;
            string wanted = !string.IsNullOrEmpty(role.displayName) ? role.displayName : "なりたい自分";
            string goalName = role.goalItem != null ? role.goalItem.displayName : wanted;

            var sb = new StringBuilder();
            int step = 1;
            RecipeDefinition firstOpen = null;
            foreach (var recipe in role.recipes)
            {
                if (recipe == null) continue;
                bool done = crafted.Contains(recipe);
                if (!done && firstOpen == null) firstOpen = recipe;
                string mark = done ? "<color=#8EE59A>◎</color>" : recipe == firstOpen ? "<color=#FFD23F>▶</color>" : "・";
                sb.Append($"{mark} {step}. {recipe.displayName}");
                if (!done)
                {
                    sb.Append("\n    <size=19><color=#D9D2C5>");
                    var parts = new List<string>();
                    foreach (var input in recipe.inputs)
                    {
                        if (input.item == null) continue;
                        int have = inventory != null ? inventory.Count(input.item) : 0;
                        string c = have >= input.count ? "#8EE59A" : "#FFFFFF";
                        parts.Add($"<color={c}>{input.item.displayName} {Mathf.Min(have, input.count)}/{input.count}</color>");
                    }
                    sb.Append(string.Join("　", parts)).Append("</color></size>");
                }
                sb.Append('\n');
                step++;
            }
            AppendOutfits(sb);
            body.text = sb.ToString().TrimEnd('\n');

            string nextText;
            Transform target;
            if (firstOpen == null)
            {
                nextText = $"{goalName}が完成した！ 時間まで村にばれないように";
                target = null;
            }
            else Plan(firstOpen, inventory, out nextText, out target);
            NextText = nextText;
            NextTarget = target;
            next.text = "次：" + nextText;
            if (lastNext != nextText)
            {
                if (lastNext != null) UiFx.Punch(this, next.rectTransform, 0.08f, 0.3f);
                lastNext = nextText;
            }
        }

        // 次の一歩：材料がそろっていれば作業台へ。足りなければ、いちばん近い拾える素材か、それをくれるお題を案内する
        private void Plan(RecipeDefinition recipe, Inventory inventory, out string text, out Transform target)
        {
            if (inventory != null && inventory.Has(recipe.inputs))
            {
                var station = FindAnyObjectByType<CraftStation>();
                target = station != null ? station.transform : null;
                text = $"材料がそろった！ {AreaOf(target)}の作業台で\n「{recipe.displayName}」を作ろう\n（だれも見ていないときに）";
                return;
            }
            foreach (var input in recipe.inputs)
            {
                if (input.item == null) continue;
                int have = inventory != null ? inventory.Count(input.item) : 0;
                if (have >= input.count) continue;

                // 別のレシピで作る物（スープなど）は、そのレシピが先
                foreach (var other in role.recipes)
                    if (other != null && other.output == input.item && !crafted.Contains(other))
                    {
                        Plan(other, inventory, out text, out target);
                        return;
                    }

                var pickup = NearestPickup(input.item);
                if (pickup != null)
                {
                    target = pickup.transform;
                    text = $"{AreaOf(target)}で「{input.item.displayName}」を拾おう\n（見られていないときに）";
                    return;
                }
                foreach (var task in role.imposedTasks)
                    if (task != null && task.rewardItem == input.item)
                    {
                        target = null;
                        text = $"「{input.item.displayName}」は村長のお題「{task.title}」のごほうび。お題を待とう";
                        return;
                    }
                target = null;
                text = $"「{input.item.displayName}」をさがそう";
                return;
            }
            target = null;
            text = $"「{recipe.displayName}」の材料をそろえよう";
        }

        // 服：まだ持っていない服は、どこで拾えるかを出す（着るとなりたい姿に近づく）
        private void AppendOutfits(StringBuilder sb)
        {
            if (role.outfits == null || role.outfits.Count == 0) return;
            var outfits = FindAnyObjectByType<OutfitController>();
            var parts = new List<string>();
            foreach (var outfit in role.outfits)
            {
                if (outfit == null || outfit.stage <= 0) continue;
                string name = !string.IsNullOrEmpty(outfit.displayName) ? outfit.displayName : outfit.item != null ? outfit.item.displayName : outfit.outfitId;
                if (outfits != null && outfits.IsWearing(outfit)) parts.Add($"<color=#8EE59A>◎{name}</color>");
                else if (outfits != null && outfits.Owns(outfit)) parts.Add($"<color=#FFD23F>{name}（C で着る）</color>");
                else
                {
                    var pickup = outfit.item != null ? NearestPickup(outfit.item) : null;
                    parts.Add(pickup != null ? $"{name}（{AreaOf(pickup.transform)}）" : name);
                }
            }
            if (parts.Count == 0) return;
            sb.Append("<size=19><color=#D9D2C5>服：</color>").Append(string.Join("\n　　", parts)).Append("</size>\n");
        }

        private ItemPickup NearestPickup(ItemDefinition item)
        {
            ItemPickup best = null;
            float bestDistance = float.MaxValue;
            Vector3 from = context.Player != null ? context.Player.position : Vector3.zero;
            foreach (var pickup in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
            {
                if (pickup.Item != item || !pickup.isActiveAndEnabled) continue;
                float d = (pickup.transform.position - from).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = pickup; }
            }
            return best;
        }

        public static string AreaOf(Transform t)
        {
            if (t == null) return "村";
            var p = new Vector2(t.position.x, t.position.z);
            string best = "村";
            float bestDistance = float.MaxValue;
            foreach (var (name, pos) in Areas)
            {
                float d = (pos - p).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = name; }
            }
            return best;
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("ObjectiveCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var go = new GameObject("Objective", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasGo.transform, false);
            panel = (RectTransform)go.transform;
            // 右上の「なりたい姿」の下、右下の「Tab 持ち物」の上に置く
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-24f, -470f);
            panel.sizeDelta = new Vector2(500f, 510f);
            var image = go.GetComponent<Image>();
            image.sprite = panelSprite;
            image.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = panelSprite != null ? new Color(1f, 1f, 1f, 0.92f) : new Color(0.1f, 0.08f, 0.06f, 0.85f);
            image.raycastTarget = false;

            string goalName = role.goalItem != null ? role.goalItem.displayName : role.displayName;
            var title = MakeText(panel, "Title", 27, new Vector2(22f, -54f), new Vector2(-22f, -14f));
            title.text = $"目標：{goalName}を完成させる";
            title.color = new Color(1f, 0.82f, 0.24f);
            next = MakeText(panel, "Next", 23, new Vector2(22f, -168f), new Vector2(-22f, -58f));
            body = MakeText(panel, "Body", 23, new Vector2(22f, -500f), new Vector2(-22f, -176f));
            body.lineSpacing = 1.0f;
            next.color = new Color(1f, 0.72f, 0.86f);
        }

        // 上端からの位置で置く（offsetMin.y / offsetMax.y は上端からのマイナス）
        private Text MakeText(RectTransform parent, string name, int size, Vector2 bottomLeftFromTop, Vector2 topRightFromTop)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = bottomLeftFromTop;
            rect.offsetMax = topRightFromTop;
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
