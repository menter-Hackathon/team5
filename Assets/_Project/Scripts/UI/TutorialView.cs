using System.Collections;
using System.Collections.Generic;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.Player;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team5.UI
{
    // 物語の導入と、初めて遊ぶ人へのガイド（CD-16。台本は tutorial.md）。Game シーンに置く
    // 1. 始まる前に、物語とゴールを数枚のカードで見せる（時間は止める。Esc で飛ばせる）
    // 2. 遊んでいる途中、初めて起きたこと（お題が出た・見られた・素材を拾った・材料がそろった・見つかった…）を、
    //    その場で時間を止めて 1 枚のカードで説明する
    // 2 回目からは（「体験入村してみる」にチェックしなければ）ゴールのカード 1 枚だけ出す
    public class TutorialView : MonoBehaviour
    {
        private const string DoneKey = "tutorial_done";

        // TitleView が入村するときに入れる
        public static bool Requested { get; set; }
        public static bool ShouldShowOnFirstPlay => PlayerPrefs.GetInt(DoneKey, 0) == 0;

        [SerializeField] private Font font;
        [SerializeField] private Sprite panelSprite; // UI_Panel_Dark

        private struct Card
        {
            public string title;
            public string body;
            public Card(string title, string body) { this.title = title; this.body = body; }
        }

        private GameSceneContext context;
        private RoleDefinition role;
        private string imposed;
        private string wanted;
        private string goalName;
        private bool guide;          // 途中の説明を出すか
        private bool introDone;
        private readonly Queue<Card> queue = new Queue<Card>();
        private readonly HashSet<string> shown = new HashSet<string>();
        private bool showing;
        private bool wasWatched;

        private RectTransform root;
        private Image dim;
        private RectTransform card;
        private Text titleText;
        private Text bodyText;
        private Text hintText;
        private Text pageText;
        private GameObject pauseRoot; // Esc の一時停止中に出す（何も出ないと固まったように見えるので）

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context == null) return;

            // 目標パネルもここで作る（シーンに置かなくてよいように）
            if (GetComponent<ObjectiveView>() == null) gameObject.AddComponent<ObjectiveView>().Init(font, panelSprite);

            guide = Requested || ShouldShowOnFirstPlay;
            Requested = false;
            BuildUi();
            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            PlayerInputReader.GuideBlocking = false;
            Unsubscribe();
        }

        private IEnumerator Run()
        {
            yield return null; // GameSceneContext がロールを決めてから
            role = context.Role != null ? context.Role : GameManager.Instance != null ? GameManager.Instance.CurrentRole : null;
            imposed = role != null && !string.IsNullOrEmpty(role.imposedRoleName) ? role.imposedRoleName : "勇者";
            wanted = role != null && !string.IsNullOrEmpty(role.displayName) ? role.displayName : "鶏白湯職人";
            goalName = role != null && role.goalItem != null ? role.goalItem.displayName : wanted;
            int minutes = context.Balance != null ? Mathf.RoundToInt(context.Balance.timeLimitSeconds / 60f) : 5;
            string intro = role != null && !string.IsNullOrEmpty(role.imposedIntro) ? role.imposedIntro : $"「お前は{imposed}の家に生まれた。{imposed}らしく生きなさい」";

            var cards = new List<Card>();
            if (guide)
            {
                cards.Add(new Card("らしさの村", "この村では、生まれた家で\n「なるべき自分」が決められている。\nみんな、言われたとおりの「らしさ」で生きている。"));
                cards.Add(new Card("村長", $"{intro}"));
                cards.Add(new Card("でも、本当は……", $"剣より寸胴鍋。\n本当は、{wanted}になりたい。\n村いちばんの「{goalName}」を作るのが夢だ。"));
                cards.Add(new Card("ただし", $"「{imposed}らしくない」ところを村人に見られたら、\nすぐに村じゅうのうわさになってしまう。"));
            }
            cards.Add(new Card("今日の目標",
                $"日が沈むまで（{minutes}分）に、\nだれにも見られずに <color=#FFD23F>{goalName}</color> を完成させよう！\n\n" +
                "<size=30>① 村長のお題をこなして、村を安心させる\n② 見られていない隙に、素材を集める\n③ 台所の作業台で、こっそり料理する</size>"));
            if (guide)
            {
                cards.Add(new Card("操作",
                    "<size=32>WASD / 矢印　歩く\nE　拾う・話す・作業台を使う（長押しでお題の動作）\nTab　持ち物　　M / 地図をクリック　村の地図\nEsc　一時停止</size>\n\n" +
                    "<size=28>右の「次：」と地図の <color=#FA80C7>★</color> が、次に行く場所だよ</size>"));
            }

            yield return ShowSequence(cards, true);
            introDone = true;
            if (guide)
            {
                Subscribe();
                // 最初のお題はもう出ているので、ここで説明する
                if (context.Tasks != null && context.Tasks.Current != null) OnTaskAssigned(context.Tasks.Current);
                PlayerPrefs.SetInt(DoneKey, 1);
            }
        }

        // ---------- 途中の説明 ----------

        private void Subscribe()
        {
            if (context.Tasks != null)
            {
                context.Tasks.OnTaskAssigned += OnTaskAssigned;
                context.Tasks.OnTaskCompleted += OnTaskCompleted;
                context.Tasks.OnTaskFailed += OnTaskFailed;
            }
            if (context.Surveillance != null) context.Surveillance.OnCaught += OnCaught;
            if (context.Inventory != null) context.Inventory.OnChanged += OnInventoryChanged;
            if (context.Crafting != null) context.Crafting.OnCrafted += OnCrafted;
            if (context.Parameters != null) context.Parameters.OnChanged += OnParameterChanged;
        }

        private void Unsubscribe()
        {
            if (context == null) return;
            if (context.Tasks != null)
            {
                context.Tasks.OnTaskAssigned -= OnTaskAssigned;
                context.Tasks.OnTaskCompleted -= OnTaskCompleted;
                context.Tasks.OnTaskFailed -= OnTaskFailed;
            }
            if (context.Surveillance != null) context.Surveillance.OnCaught -= OnCaught;
            if (context.Inventory != null) context.Inventory.OnChanged -= OnInventoryChanged;
            if (context.Crafting != null) context.Crafting.OnCrafted -= OnCrafted;
            if (context.Parameters != null) context.Parameters.OnChanged -= OnParameterChanged;
        }

        private void Update()
        {
            if (pauseRoot != null)
            {
                bool paused = GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;
                if (pauseRoot.activeSelf != paused) pauseRoot.SetActive(paused);
            }
            if (!guide || !introDone || context == null) return;
            var surveillance = context.Surveillance;
            if (surveillance != null)
            {
                if (surveillance.IsPlayerWatched && !wasWatched)
                    Tip("watched", "見られている！",
                        "村人の足元の扇形が<color=#FF6A5A>赤</color>くなり、右上の目が開いたら、見られているしるし。\n\n" +
                        "見られている間に <color=#FFD23F>素材を拾う・料理する・道から外れる</color> と、\n「らしくない」ことがばれて <color=#FF6A5A>平和度 −25</color>。\n" +
                        "お題をこなすのは、見られていても大丈夫。");
                wasWatched = surveillance.IsPlayerWatched;
            }
            if (context.Timer != null && context.Timer.IsRunning && context.Timer.Remaining <= 60f && context.Timer.Remaining > 1f)
                Tip("last_minute", "日が沈むまで、あと1分！", $"{goalName}はできそう？\n右の「次：」を見て、急ごう。");
        }

        private void OnTaskAssigned(TaskInstance task)
        {
            if (task == null || task.Definition == null) return;
            var def = task.Definition;
            string how = def.type switch
            {
                TaskType.ActionAtSpot => $"地図の<color=#FFD23F>！</color>の場所へ行き、E を長押し。",
                TaskType.TalkToNpc => "地図の<color=#FFD23F>！</color>の村人に近づいて、E で話しかける。",
                _ => $"「{(def.requiredItem != null ? def.requiredItem.displayName : "頼まれた物")}」を{def.requiredCount}つ集めて、地図の<color=#FFD23F>！</color>の場所で E。",
            };
            Tip("task", "村長のお題",
                $"左のカード：<color=#FFD23F>{def.title}</color>\n{how}\n\n" +
                "こなすと村は安心する（<color=#8EE59A>平和度 ＋</color>）。でも、自分の色はうすくなる（<color=#FF9A6A>個性 −</color>）。\n" +
                "時間切れになると平和度が下がるよ。");
        }

        private void OnTaskCompleted(TaskInstance task)
        {
            string reward = task != null && task.Definition != null && task.Definition.rewardItem != null
                ? $"\nごほうびに「{task.Definition.rewardItem.displayName}」をもらった！ 料理の材料になる。" : "";
            Tip("task_done", "お題をこなした",
                $"村長は満足そうだ。平和度が上がった。\nでも、{imposed}らしくしたぶん、自分の色が少しうすくなった。{reward}\n\n" +
                $"次は、だれも見ていない隙に、{wanted}への準備を進めよう。");
        }

        private void OnTaskFailed(TaskInstance task)
        {
            Tip("task_fail", "お題に間に合わなかった……",
                "村長はがっかりしている（<color=#FF6A5A>平和度 −</color>）。\n平和度が 0 になると、村から追い出されてゲームオーバー。\n次のお題はなるべくこなそう。");
        }

        private void OnCaught(NPC.VisionSensor by)
        {
            Tip("caught", "見つかってしまった！",
                $"「{imposed}らしくない」ところを見られた（<color=#FF6A5A>平和度 −25</color>）。\n\n" +
                "平和度が 0 になると、村から追い出されてゲームオーバー。\n扇形の外に出てから、もう一度やろう。");
        }

        private void OnInventoryChanged()
        {
            if (context.Inventory == null || role == null || role.recipes == null) return;
            foreach (var recipe in role.recipes)
            {
                if (recipe == null || recipe.inputs == null) continue;
                foreach (var input in recipe.inputs)
                    if (input.item != null && context.Inventory.Count(input.item) > 0)
                    {
                        Tip("pickup", "素材を手に入れた！",
                            "右の「目標」に、集めた数が出るよ（Tab で持ち物も見られる）。\n\n" +
                            "料理の材料は、見られていない隙に集めよう。");
                        goto checkReady;
                    }
            }
            checkReady:
            foreach (var recipe in role.recipes)
                if (recipe != null && context.Inventory.Has(recipe.inputs))
                {
                    Tip("ready", "材料がそろった！",
                        $"台所の作業台へ行き、E で「{recipe.displayName}」を作ろう。\n\n" +
                        "作っている間に見られると、中断して<color=#FF6A5A>平和度 −25</color>。\nまわりに村人がいないのを確かめてから！");
                    break;
                }
        }

        private void OnCrafted(RecipeDefinition recipe)
        {
            if (role != null && recipe != null && recipe.output == role.goalItem) return; // 完成のときはクリアの演出に任せる
            Tip("crafted", "こっそり料理できた！",
                $"{wanted}に近づいた（左上の<color=#FFD23F>進み具合 ＋</color>）。\n自分の色も濃くなった（<color=#8EE59A>個性 ＋</color>）。\n\n" +
                $"進み具合が 100 になれば、{wanted}になれる！");
        }

        private void OnParameterChanged(ParameterType type, float before, float after)
        {
            if (type == ParameterType.Individuality && after < 25f && after < before)
                Tip("faded", "自分の色がうすくなってきた……",
                    $"{imposed}らしくしすぎると、本当の自分を見失ってしまう。\n個性が 0 になるとゲームオーバー（20 未満だと足も遅くなる）。\n\n" +
                    "こっそり料理すると、色が戻るよ。");
        }

        // 同じ説明は 1 回だけ。カードを出している間に来たものは順番待ち
        private void Tip(string key, string title, string body)
        {
            if (!guide || !introDone || shown.Contains(key)) return;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Result) return;
            shown.Add(key);
            queue.Enqueue(new Card(title, body));
            if (!showing) StartCoroutine(Drain());
        }

        private IEnumerator Drain()
        {
            showing = true;
            // 少しだけ間をおいて、何が起きたか画面で見えてから止める
            yield return new WaitForSecondsRealtime(0.35f);
            while (queue.Count > 0)
            {
                // 一時停止中・作業中は待つ
                while (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused) yield return null;
                if (GameManager.Instance != null && GameManager.Instance.State == GameState.Result) { queue.Clear(); break; }
                var list = new List<Card>();
                while (queue.Count > 0) list.Add(queue.Dequeue());
                yield return ShowSequence(list, false);
            }
            showing = false;
        }

        // ---------- カードを出す ----------

        private IEnumerator ShowSequence(List<Card> cards, bool skippable)
        {
            if (cards.Count == 0) yield break;
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            PlayerInputReader.GuideBlocking = true;
            root.gameObject.SetActive(true);

            for (int i = 0; i < cards.Count; i++)
            {
                titleText.text = cards[i].title;
                bodyText.text = cards[i].body;
                pageText.text = cards.Count > 1 ? $"{i + 1} / {cards.Count}" : "";
                hintText.text = skippable && cards.Count > 1 && i < cards.Count - 1 ? "Enter / Space / クリックで次へ　　Esc でとばす" : "Enter / Space / クリックで続ける";
                UiFx.Punch(this, card, 0.05f, 0.25f);

                yield return new WaitForSecondsRealtime(0.4f); // 連打で飛ばしすぎないように
                bool skip = false;
                while (true)
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) break;
                    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) break;
                    if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) break;
                    if (skippable && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { skip = true; break; }
                    yield return null;
                }
                if (skip) i = Mathf.Max(i, cards.Count - 2); // 最後のカード（目標・操作）は必ず見せる
            }

            root.gameObject.SetActive(false);
            yield return null; // 決定キーがそのままゲームに入らないように 1 フレーム待つ
            PlayerInputReader.GuideBlocking = false;
            Time.timeScale = previousScale > 0f ? previousScale : 1f;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused) Time.timeScale = 0f;
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("GuideCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            root = (RectTransform)new GameObject("Root", typeof(RectTransform)).transform;
            root.SetParent(canvasGo.transform, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            dim = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            dim.transform.SetParent(root, false);
            var dimRect = dim.rectTransform;
            dimRect.anchorMin = Vector2.zero; dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
            dim.color = new Color(0.03f, 0.02f, 0.05f, 0.55f);

            // 後ろが透けて読みにくくならないように、濃い下地を敷く
            var back = new GameObject("CardBack", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            back.transform.SetParent(root, false);
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            back.rectTransform.sizeDelta = new Vector2(1160f, 540f);
            back.color = new Color(0.09f, 0.07f, 0.11f, 0.94f);
            back.raycastTarget = false;

            var cardImage = new GameObject("Card", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            cardImage.transform.SetParent(root, false);
            card = cardImage.rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(1180f, 560f);
            cardImage.sprite = panelSprite;
            cardImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            cardImage.color = panelSprite != null ? Color.white : new Color(0.18f, 0.17f, 0.2f, 0.95f);

            titleText = MakeText(card, "Title", 52, new Vector2(60f, -120f), new Vector2(-60f, -30f), TextAnchor.MiddleCenter);
            titleText.color = new Color(1f, 0.82f, 0.24f);
            bodyText = MakeText(card, "Body", 36, new Vector2(70f, -470f), new Vector2(-70f, -130f), TextAnchor.MiddleCenter);
            bodyText.lineSpacing = 1.15f;
            hintText = MakeText(card, "Hint", 24, new Vector2(40f, -540f), new Vector2(-40f, -490f), TextAnchor.MiddleRight);
            hintText.color = new Color(1f, 0.78f, 0.24f);
            pageText = MakeText(card, "Page", 24, new Vector2(40f, -540f), new Vector2(-40f, -490f), TextAnchor.MiddleLeft);
            pageText.color = new Color(1f, 1f, 1f, 0.6f);

            root.gameObject.SetActive(false);

            // 一時停止
            var pause = new GameObject("Pause", typeof(RectTransform), typeof(Image));
            pause.transform.SetParent(canvasGo.transform, false);
            var pauseRect = (RectTransform)pause.transform;
            pauseRect.anchorMin = Vector2.zero; pauseRect.anchorMax = Vector2.one;
            pauseRect.offsetMin = pauseRect.offsetMax = Vector2.zero;
            pause.GetComponent<Image>().color = new Color(0.03f, 0.02f, 0.05f, 0.6f);
            var pauseTitle = MakeText(pauseRect, "Title", 72, new Vector2(0f, -520f), new Vector2(0f, -400f), TextAnchor.MiddleCenter);
            pauseTitle.text = "一時停止中";
            pauseTitle.color = new Color(1f, 0.82f, 0.24f);
            var pauseHint = MakeText(pauseRect, "Hint", 34, new Vector2(0f, -600f), new Vector2(0f, -530f), TextAnchor.MiddleCenter);
            pauseHint.text = "Esc でゲームに戻る";
            pauseRoot = pause;
            pauseRoot.SetActive(false);
        }

        private Text MakeText(RectTransform parent, string name, int size, Vector2 offsetMin, Vector2 offsetMax, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
