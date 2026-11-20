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
        private GameObject pauseRoot;
        private RectTransform banner;
        private Text bannerText; // Esc の一時停止中に出す（何も出ないと固まったように見えるので）

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context == null) return;

            // 目標パネルもここで作る（シーンに置かなくてよいように）
            if (GetComponent<ObjectiveView>() == null) gameObject.AddComponent<ObjectiveView>().Init(font, panelSprite);
            if (GetComponent<GuideArrowView>() == null) gameObject.AddComponent<GuideArrowView>().Init(font);
            if (GetComponent<NpcNameplateView>() == null) gameObject.AddComponent<NpcNameplateView>().Init(font, panelSprite);

            guide = Requested || ShouldShowOnFirstPlay;
            if (context.Player != null && context.Player.TryGetComponent(out OutfitController outfits)) outfits.OnEquipped += OnEquipped;
            Requested = false;
            BuildUi();
            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            if (context != null && context.Player != null && context.Player.TryGetComponent(out OutfitController outfits)) outfits.OnEquipped -= OnEquipped;
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
                cards.Add(new Card("でも、本当は……", Dream(role, wanted, goalName)));
                cards.Add(new Card("ただし", $"「{imposed}らしくない」ところを村人に見られたら、\nすぐに村じゅうのうわさになってしまう。"));
            }
            cards.Add(new Card("今日の目標",
                $"{minutes}分で <color=#FFD23F>{goalName}</color> を完成させよう\n\n" +
                $"<size=30><color=#FFD23F>！</color> お題　　<color=#7CF08A>◆</color> 素材　　<color=#FA80C7>★</color> {StationArea()}の作業台</size>"));
            if (guide)
            {
            }

            yield return ShowSequence(cards, true);
            if (guide)
            {
                // 操作は止めずに、画面の下で説明する。まず歩いてもらう
                yield return ControlsBanner();
                introDone = true;
                Subscribe();
                // 最初のお題はもう出ているので、歩けたあとで説明する
                if (context.Tasks != null && context.Tasks.Current != null) OnTaskAssigned(context.Tasks.Current);
                PlayerPrefs.SetInt(DoneKey, 1);
            }
            else introDone = true;
        }

        // 画面の下に操作を出す（時間は止めない）。3m 歩いたら次の説明へ。残りの操作は少しの間だけ出す
        private IEnumerator ControlsBanner()
        {
            banner.gameObject.SetActive(true);
            bannerText.text = "<color=#FFD23F>WASD</color> で歩こう";
            UiFx.Punch(this, banner, 0.06f, 0.25f);
            Vector3 start = context.Player != null ? context.Player.position : Vector3.zero;
            float t = 0f;
            while (context.Player != null && Vector3.Distance(start, context.Player.position) < 3f && t < 20f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            bannerText.text = "<color=#FFD23F>Esc</color> で操作確認";
            UiFx.Punch(this, banner, 0.06f, 0.25f);
            StartCoroutine(HideBannerLater(4f));
            yield return new WaitForSeconds(1.2f);
        }

        private IEnumerator HideBannerLater(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (banner != null) banner.gameObject.SetActive(false);
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
                if (pauseRoot.activeSelf != paused)
                {
                    pauseRoot.SetActive(paused);
                    var events = UnityEngine.EventSystems.EventSystem.current;
                    if (paused && events != null && resumeButton != null) events.SetSelectedGameObject(resumeButton.gameObject);
                }
            }
            if (!guide || !introDone || context == null) return;
            var surveillance = context.Surveillance;
            if (surveillance != null)
            {
                if (surveillance.IsPlayerWatched && !wasWatched)
                    Tip("watched", "見られている！",
                        "<color=#FF6A5A>赤い扇形</color>の中で\n拾う・作る・道から外れる → <color=#FF6A5A>平和度 −25</color>");
                wasWatched = surveillance.IsPlayerWatched;
            }
            if (context.Timer != null && context.Timer.IsRunning && context.Timer.Remaining <= 60f && context.Timer.Remaining > 1f)
                Tip("last_minute", "あと1分！", $"<color=#FFD23F>{goalName}</color>");
        }

        private void OnTaskAssigned(TaskInstance task)
        {
            if (task == null || task.Definition == null) return;
            var def = task.Definition;
            Tip("task", "村長のお題",
                $"<color=#FFD23F>！ {def.title}</color>\n\n" +
                "<color=#8EE59A>平和度 ＋</color>　<color=#FF9A6A>個性 −</color>");
        }

        private void OnTaskCompleted(TaskInstance task)
        {
            string reward = task != null && task.Definition != null && task.Definition.rewardItem != null
                ? $"\n<color=#7CF08A>◆</color> {task.Definition.rewardItem.displayName}" : "";
            Tip("task_done", "お題をこなした",
                $"<color=#8EE59A>平和度 ＋</color>　<color=#FF9A6A>個性 −</color>{reward}" + NextLine());
        }

        private void OnTaskFailed(TaskInstance task)
        {
            Tip("task_fail", "間に合わなかった……",
                "<color=#FF6A5A>平和度 −</color>" + NextLine());
        }

        private void OnCaught(NPC.VisionSensor by)
        {
            Tip("caught", "見つかった！",
                "<color=#FF6A5A>平和度 −25</color>" + NextLine());
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
                        Tip("pickup", "素材を手に入れた！", NextLine().TrimStart('\n'));
                        goto checkReady;
                    }
            }
            checkReady:
            if (role.outfits != null)
                foreach (var o in role.outfits)
                    if (o != null && o.item != null && context.Inventory.Count(o.item) > 0)
                    {
                        Tip("outfit", "服を手に入れた！",
                            $"<color=#FFD23F>{o.displayName}</color>　<color=#FFD23F>C</color> で着替え（<color=#8EE59A>個性 ＋</color>）");
                        break;
                    }
            foreach (var recipe in role.recipes)
                if (recipe != null && context.Inventory.Has(recipe.inputs))
                {
                    Tip("ready", "材料がそろった！",
                        $"<color=#FA80C7>★</color> {StationArea()}の作業台で「{recipe.displayName}」");
                    break;
                }
        }

        // 着替えたら、画面の下に大きく出す（ガイドの有無に関係なく）
        private void OnEquipped(OutfitDefinition outfit)
        {
            if (outfit == null || banner == null) return;
            string wantedName = role != null ? role.displayName : "なりたい自分";
            var controller = context.Player != null ? context.Player.GetComponent<OutfitController>() : null;
            int max = role != null && role.outfits != null ? role.outfits.Count : 0;
            int stage = controller != null ? controller.CurrentStage : 0;
            banner.gameObject.SetActive(true);
            bannerText.text = $"<size=38><color=#FFD23F>{outfit.displayName}</color></size>　{stage} / {max}";
            UiFx.Punch(this, banner, 0.12f, 0.35f);
            StopCoroutine(nameof(HideBannerLater));
            StartCoroutine(HideBannerLater(3.5f));
        }

        private void OnCrafted(RecipeDefinition recipe)
        {
            if (role != null && recipe != null && recipe.output == role.goalItem) return; // 完成のときはクリアの演出に任せる
            Tip("crafted", "こっそり作れた！",
                $"<color=#FFD23F>{wanted} ＋</color>　<color=#8EE59A>個性 ＋</color>" + NextLine());
        }

        private void OnParameterChanged(ParameterType type, float before, float after)
        {
            if (type == ParameterType.Individuality && after < 25f && after < before)
                Tip("faded", "個性がうすい……",
                    $"<color=#FF9A6A>個性 0</color> でおしまい");
        }

        private static string Dream(RoleDefinition role, string wanted, string goalName)
        {
            switch (role != null ? role.roleId?.Trim() : "")
            {
                case "ramen_chef": return $"剣より寸胴鍋。\n本当は、{wanted}になりたい。\n村いちばんの「{goalName}」を作るのが夢だ。";
                case "band_man": return $"家を継ぐより、音楽がやりたい。\n本当は、{wanted}になりたい。\n自分の曲を作って、仲間と演奏するのが夢だ。";
                case "librarian": return $"ハンマーより本。\n本当は、{wanted}になりたい。\n村に小さな図書館を開くのが夢だ。";
                case "progamer": return $"畑より、ゲームの世界。\n本当は、{wanted}になりたい。\n世界大会に出るのが夢だ。";
                default: return $"本当は、{wanted}になりたい。\n「{goalName}」を完成させるのが夢だ。";
            }
        }

        private string Craft() => role != null && role.roleId?.Trim() == "ramen_chef" ? "料理する" : "作る";

        private static string StationArea()
        {
            var station = FindAnyObjectByType<CraftStation>();
            return ObjectiveView.AreaOf(station != null ? station.transform : null);
        }

        private int Interval() => context != null && context.Balance != null ? Mathf.RoundToInt(context.Balance.taskIntervalSeconds) : 20;

        private string ItemWhere(ItemDefinition item)
        {
            if (item == null) return "";
            foreach (var pickup in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
                if (pickup.Item == item) return $"{item.displayName}は{ObjectiveView.AreaOf(pickup.transform)}にあるよ。";
            return "";
        }

        private static string NextLine()
        {
            return string.IsNullOrEmpty(ObjectiveView.NextText) ? "" : $"\n\n<color=#FA80C7>次：{ObjectiveView.NextText}</color>";
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
                hintText.text = skippable && cards.Count > 1 && i < cards.Count - 1 ? "▶ Enter　　Esc スキップ" : "▶ Enter";
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
            card.sizeDelta = new Vector2(1400f, 560f);
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

            // 操作の帯（画面の下・時間は止めない）
            var bannerImage = new GameObject("ControlsBanner", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bannerImage.transform.SetParent(canvasGo.transform, false);
            banner = bannerImage.rectTransform;
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 0f);
            banner.pivot = new Vector2(0.5f, 0f);
            banner.anchoredPosition = new Vector2(30f, 40f);
            banner.sizeDelta = new Vector2(1100f, 110f);
            bannerImage.color = new Color(0.09f, 0.07f, 0.11f, 0.88f);
            bannerImage.raycastTarget = false;
            bannerText = MakeText(banner, "Text", 26, new Vector2(20f, -106f), new Vector2(-20f, -4f), TextAnchor.MiddleCenter);
            banner.gameObject.SetActive(false);

            // 一時停止（Esc）。操作の一覧と、再開・タイトルへ
            pauseRoot = BuildPauseMenu(canvasGo.transform);
            pauseRoot.SetActive(false);
        }

        private static readonly Color Gold = new Color(1f, 0.82f, 0.24f);
        private static readonly Color Parchment = new Color(0.96f, 0.91f, 0.8f);
        private static readonly Color Wood = new Color(0.4f, 0.27f, 0.17f, 0.95f);

        private static readonly string[,] Controls =
        {
            { "WASD / 矢印", "移動" },
            { "E", "拾う・話す・作業台" },
            { "E 長押し", "お題" },
            { "Tab", "持ち物" },
            { "C", "着替え" },
            { "M", "地図" },
            { "Esc", "一時停止" },
        };

        private Button resumeButton;

        private GameObject BuildPauseMenu(Transform parent)
        {
            var pause = new GameObject("Pause", typeof(RectTransform), typeof(Image));
            pause.transform.SetParent(parent, false);
            var pauseRect = (RectTransform)pause.transform;
            pauseRect.anchorMin = Vector2.zero; pauseRect.anchorMax = Vector2.one;
            pauseRect.offsetMin = pauseRect.offsetMax = Vector2.zero;
            pause.GetComponent<Image>().color = new Color(0.12f, 0.07f, 0.03f, 0.7f);

            int rows = Controls.GetLength(0);
            float rowHeight = 58f;
            float height = 150f + rows * rowHeight + 150f;
            var panelImage = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            panelImage.transform.SetParent(pauseRect, false);
            var panel = panelImage.rectTransform;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(760f, height);
            panelImage.sprite = panelSprite;
            panelImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.color = panelSprite != null ? Color.white : new Color(0.26f, 0.17f, 0.11f, 0.96f);

            var title = MakeText(panel, "Title", 60, new Vector2(40f, -120f), new Vector2(-40f, -30f), TextAnchor.MiddleCenter);
            title.text = "一時停止";
            title.color = Gold;

            for (int i = 0; i < rows; i++)
            {
                float top = -140f - i * rowHeight;
                var key = MakeText(panel, "Key", 32, new Vector2(90f, top - rowHeight), new Vector2(-400f, top), TextAnchor.MiddleRight);
                key.text = Controls[i, 0];
                key.color = Gold;
                var action = MakeText(panel, "Action", 32, new Vector2(400f, top - rowHeight), new Vector2(-60f, top), TextAnchor.MiddleLeft);
                action.text = Controls[i, 1];
                action.color = Parchment;
            }

            float buttonsY = -150f - rows * rowHeight - 50f;
            resumeButton = MakeButton(panel, "再開", new Vector2(-150f, buttonsY), () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            });
            var titleButton = MakeButton(panel, "タイトルへ", new Vector2(150f, buttonsY), () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
            });
            resumeButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = titleButton, selectOnLeft = titleButton };
            titleButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = resumeButton, selectOnLeft = resumeButton };

            pause.SetActive(false);
            return pause;
        }

        private Button MakeButton(RectTransform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var image = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(260f, 76f);
            image.color = Wood;
            var button = image.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
            colors.selectedColor = new Color(1f, 0.92f, 0.7f);
            colors.pressedColor = new Color(0.85f, 0.8f, 0.7f);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            var text = MakeText(rect, "Text", 34, new Vector2(0f, -76f), new Vector2(0f, 0f), TextAnchor.MiddleCenter);
            text.text = label;
            text.color = Parchment;
            return button;
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
