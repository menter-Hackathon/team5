using System.Collections;
using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team5.UI
{
    // チュートリアル（CD-16 の簡易版。台本は tutorial.md）。Game シーンに置く
    // タイトルで「体験入村してみる」にチェックしたとき、または初めて遊ぶときに、画面下に説明を 1 行ずつ出す。
    // 説明の間は制限時間を止める。歩く・拾うなどはそのままできる
    public class TutorialView : MonoBehaviour
    {
        private const string DoneKey = "tutorial_done";

        // TitleView が入村するときに入れる
        public static bool Requested { get; set; }

        [SerializeField] private Font font;
        [SerializeField] private Sprite panelSprite; // UI_Panel_Dark
        [SerializeField] private float walkDistance = 3f;

        private enum Next { Key, Walk, Inventory }
        private struct Step { public string text; public Next next; public Step(string t, Next n) { text = t; next = n; } }

        private GameSceneContext context;
        private Text label;
        private Text hint;
        private RectTransform panel;

        public static bool ShouldShowOnFirstPlay => PlayerPrefs.GetInt(DoneKey, 0) == 0;

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (!Requested || context == null || GameManager.Instance == null) return;
            Requested = false;
            BuildUi();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return null; // GameSceneContext がタイマーを動かしてから止める
            if (context.Timer != null) context.Timer.Stop();

            var manager = GameManager.Instance;
            RoleDefinition role = context.Role != null ? context.Role : manager != null ? manager.CurrentRole : null;
            string imposed = role != null && !string.IsNullOrEmpty(role.imposedRoleName) ? role.imposedRoleName : "勇者";
            string wanted = role != null && !string.IsNullOrEmpty(role.displayName) ? role.displayName : "鶏白湯職人";
            int minutes = context.Balance != null ? Mathf.RoundToInt(context.Balance.timeLimitSeconds / 60f) : 5;

            var steps = new[]
            {
                new Step($"村長「お前は{imposed}の家に生まれた。{imposed}らしく生きなさい」", Next.Key),
                new Step($"でも本当は……{wanted}になりたい！", Next.Key),
                new Step("WASD で歩いてみよう", Next.Walk),
                new Step("扇形は村人の視界。赤いときは見られている", Next.Key),
                new Step("見られている間は、村長のお題をこなそう（お題の場所で E を長押し）", Next.Key),
                new Step("お題をこなすと村は喜ぶ。でも色が薄くなる……", Next.Key),
                new Step("だれも見ていないうちに、素材を拾って作業台でこっそり料理だ", Next.Key),
                new Step("隠れて作ると色が濃くなり、なりたい姿に近づく", Next.Key),
                new Step("Tab で持ち物を開こう（C で着替え）", Next.Inventory),
                new Step("見られている間に拾う・作る・着替えると、平和度が減る", Next.Key),
                new Step("進み具合 100 で勝ち。色か平和度が 0 になると負け", Next.Key),
                new Step($"制限時間は {minutes} 分。{wanted}をめざそう！", Next.Key),
            };

            foreach (var step in steps)
            {
                Show(step);
                yield return WaitFor(step.next);
            }

            panel.gameObject.SetActive(false);
            PlayerPrefs.SetInt(DoneKey, 1);
            if (context.Timer != null && context.Timer.Remaining > 0f) context.Timer.StartTimer(context.Timer.Remaining);
        }

        private void Show(Step step)
        {
            label.text = step.text;
            hint.text = step.next switch
            {
                Next.Walk => "歩くと次へ",
                Next.Inventory => "Tab を押すと次へ",
                _ => "Enter / Space で次へ",
            };
            UiFx.Punch(this, panel, 0.06f, 0.25f);
        }

        private IEnumerator WaitFor(Next next)
        {
            yield return new WaitForSecondsRealtime(0.35f); // 連打で飛ばしすぎないように
            Vector3 start = context.Player != null ? context.Player.position : Vector3.zero;
            while (true)
            {
                var keyboard = Keyboard.current;
                switch (next)
                {
                    case Next.Walk:
                        if (context.Player != null && Vector3.Distance(start, context.Player.position) >= walkDistance) yield break;
                        break;
                    case Next.Inventory:
                        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) yield break;
                        break;
                    default:
                        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) yield break;
                        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) yield break;
                        break;
                }
                yield return null;
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            panel = (RectTransform)panelGo.transform;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 150f);
            panel.sizeDelta = new Vector2(1240f, 150f);
            var image = panelGo.GetComponent<Image>();
            image.sprite = panelSprite;
            image.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = panelSprite != null ? Color.white : new Color(0.18f, 0.17f, 0.2f, 0.9f);

            label = MakeText(panel, "Text", 40, new Vector2(40f, 40f), new Vector2(-40f, -16f), TextAnchor.MiddleCenter);
            hint = MakeText(panel, "Hint", 24, new Vector2(40f, 8f), new Vector2(-40f, -110f), TextAnchor.LowerRight);
            hint.color = new Color(1f, 0.78f, 0.24f);
        }

        private Text MakeText(Transform parent, string name, int size, Vector2 offsetMin, Vector2 offsetMax, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
    }
}
