using System.Linq;
using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Team5.UI
{
    // 仮のタイトル画面。見た目の作り込みは CD-15、ロール選択は CD-20
    public class TitleView : MonoBehaviour
    {
        [SerializeField] private RoleDefinition defaultRole; // MVP は Role_Sample
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private UnityEvent<bool> onEntryRequested = new();
        [SerializeField] private RoleDefinition[] roles; // 選べるロール（CD-20）。2 つ以上あれば入村の前に選ぶ

        private GameObject rolePanel;

        // 結果画面の「もう一度あそぶ」から戻ったときは、職業選びから始める
        public static bool OpenRolesOnLoad;

        private void Start()
        {
            // ロゴ → ボタン → チェック → 補足 の順にすべり込む
            // 操作キーの案内（↑↓ 選ぶ / Enter 決める）はマウスでも分かるので出さない
            if (titlePanel != null && titlePanel.transform.Find("Keys") is Transform keys) keys.gameObject.SetActive(false);
            string[] order = { "Logo", "StartButton", "Tutorial", "Note", "Team" };
            for (int i = 0; i < order.Length; i++)
            {
                if (titlePanel != null && titlePanel.transform.Find(order[i]) is RectTransform rect)
                    UiFx.SlideIn(this, rect, new Vector2(-70f, 0f), 0.5f, 0.2f + i * 0.09f);
            }
            if (titlePanel != null && titlePanel.transform.Find("StartButton") is RectTransform start) StartCoroutine(Breathe(start));
            SelectStart();
            if (OpenRolesOnLoad) { OpenRolesOnLoad = false; OnStartPressed(); }
        }

        // 「入村してみる」をゆっくり脈打たせて、押すところだと分かるようにする
        private System.Collections.IEnumerator Breathe(RectTransform target)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            while (true)
            {
                UiFx.Punch(this, target, 0.04f, 0.9f);
                yield return new WaitForSecondsRealtime(1.6f);
            }
        }

        // 「入村してみる」ボタンから呼ぶ
        public void OnStartPressed()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager がありません。Title シーンから再生してください。", this);
                return;
            }
            // 「体験入村してみる」にチェック、または初めて遊ぶときはチュートリアルから（CD-16）
            var valid = roles != null ? System.Array.FindAll(roles, r => r != null) : new RoleDefinition[0];
            if (valid.Length > 1) { ShowRoles(valid); return; }
            StartWith(defaultRole);
        }

        private void StartWith(RoleDefinition role)
        {
            var toggle = titlePanel != null ? titlePanel.GetComponentInChildren<UnityEngine.UI.Toggle>(true) : null;
            TutorialView.Requested = (toggle != null && toggle.isOn) || TutorialView.ShouldShowOnFirstPlay;
            GameManager.Instance.StartGame(role);
        }

        // ---------- ロールを選ぶ（CD-20 の簡易版） ----------

        private static readonly System.Collections.Generic.Dictionary<string, string> RoleNotes = new()
        {
            { "ramen_chef", "剣より寸胴鍋。最高の鶏白湯ラーメンを作りたい" },
            { "band_man", "家を継ぐより、自分の曲を作って演奏したい" },
            { "librarian", "ハンマーより本。村に小さな図書館を開きたい" },
            { "progamer", "畑よりゲームの世界。夢は世界チャンピオン" },
        };

        private void ShowRoles(RoleDefinition[] list)
        {
            if (rolePanel == null) rolePanel = BuildRolePanel(list);
            rolePanel.SetActive(true);
            titlePanel.SetActive(false);
            var first = rolePanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (first != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private void HideRoles()
        {
            if (rolePanel != null) rolePanel.SetActive(false);
            titlePanel.SetActive(true);
            SelectStart();
        }

        // 矢印キー・ゲームパッドで選べるように、最初から「入村してみる」を選んでおく
        private void SelectStart()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null || titlePanel == null) return;
            var start = titlePanel.transform.Find("StartButton");
            var button = start != null ? start.GetComponent<UnityEngine.UI.Selectable>() : titlePanel.GetComponentInChildren<UnityEngine.UI.Selectable>();
            if (button != null) es.SetSelectedGameObject(button.gameObject);
        }

        // 職業カード：いま選んでいるカードを、枠と大きさではっきり見せる
        private readonly System.Collections.Generic.List<(RectTransform card, GameObject frame, GameObject hint)> roleCards = new System.Collections.Generic.List<(RectTransform, GameObject, GameObject)>();

        private void Update()
        {
            // マウスでどこかを押して選択が外れても、矢印キーでまた選べるように
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && rolePanel != null && rolePanel.activeSelf)
            {
                var current = es.currentSelectedGameObject;
                foreach (var (card, frame, hint) in roleCards)
                {
                    bool on = current == card.gameObject;
                    float target = on ? 1.07f : 0.97f;
                    card.localScale = Vector3.one * Mathf.MoveTowards(card.localScale.x, target, Time.unscaledDeltaTime * 1.2f);
                    if (frame.activeSelf != on) frame.SetActive(on);
                    if (hint.activeSelf != on) hint.SetActive(on);
                }
            }
            if (es == null || es.currentSelectedGameObject != null) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool arrow = kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame);
            if (!arrow) return;
            if (rolePanel != null && rolePanel.activeSelf)
            {
                var first = rolePanel.GetComponentInChildren<UnityEngine.UI.Button>();
                if (first != null) es.SetSelectedGameObject(first.gameObject);
            }
            else if (titlePanel != null && titlePanel.activeSelf) SelectStart();
        }

        private GameObject BuildRolePanel(RoleDefinition[] list)
        {
            var canvas = titlePanel.GetComponentInParent<Canvas>();
            var anyText = titlePanel.GetComponentInChildren<UnityEngine.UI.Text>(true);
            Font font = anyText != null ? anyText.font : null;

            var root = NewRect("RolePanel", canvas.transform);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var dim = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0.12f, 0.07f, 0.03f, 0.7f);

            var heading = NewText(root, font, "どの「なりたい自分」を目指す？", 56, new Color(1f, 0.82f, 0.24f));
            heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            heading.rectTransform.anchoredPosition = new Vector2(0f, 300f);
            heading.rectTransform.sizeDelta = new Vector2(1600f, 90f);

            float width = 400f, gap = 30f;
            float total = list.Length * width + (list.Length - 1) * gap;
            for (int i = 0; i < list.Length; i++)
            {
                var role = list[i];
                var card = NewRect("Role_" + role.roleId, root);
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(width, 420f);
                card.anchoredPosition = new Vector2(-total * 0.5f + width * 0.5f + i * (width + gap), -20f);
                var bg = card.gameObject.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0.26f, 0.17f, 0.11f, 0.96f);
                // Button より先に付けて、押した瞬間に「もう選ばれていたか」を覚える
                var press = card.gameObject.AddComponent<RoleCardPress>();
                var button = card.gameObject.AddComponent<UnityEngine.UI.Button>();
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
                colors.selectedColor = new Color(1f, 0.92f, 0.7f);
                colors.pressedColor = new Color(0.85f, 0.8f, 0.7f);
                button.colors = colors;
                var picked = role;
                // マウスは 1 回目で選ぶだけ、選んだカードをもう一度押したら決定（Enter はそのまま決定）
                button.onClick.AddListener(() =>
                {
                    var mouse = UnityEngine.InputSystem.Mouse.current;
                    bool byMouse = mouse != null && mouse.leftButton.wasReleasedThisFrame;
                    if (byMouse && !press.WasSelected) return;
                    StartWith(picked);
                });

                var bar = NewRect("Bar", card);
                bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = new Vector2(1f, 1f);
                bar.pivot = new Vector2(0.5f, 1f);
                bar.sizeDelta = new Vector2(0f, 16f);
                var barImage = bar.gameObject.AddComponent<UnityEngine.UI.Image>();
                barImage.color = role.roleColor;
                barImage.raycastTarget = false;

                var name = NewText(card, font, role.displayName, 48, role.roleColor);
                Place(name.rectTransform, 0f, -70f, 80f);
                var imposed = NewText(card, font, $"押し付けられる「らしさ」\n<size=30>{role.imposedRoleName}</size>", 22, new Color(0.85f, 0.83f, 0.8f));
                Place(imposed.rectTransform, 0f, -150f, 70f);
                string note = role.roleId != null && RoleNotes.TryGetValue(role.roleId.Trim(), out var n) ? n : "";
                var desc = NewText(card, font, note, 28, Color.white);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(desc.rectTransform, 0f, -260f, 120f);
                string goal = role.goalItem != null ? role.goalItem.displayName : role.displayName;
                var goalText = NewText(card, font, $"目標：{goal}", 26, new Color(1f, 0.82f, 0.24f));
                Place(goalText.rectTransform, 0f, -360f, 50f);

                // 選んでいるときだけ出す：ロールの色の太い枠と「Enter で決定」
                var frame = NewRect("Frame", card);
                frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
                frame.offsetMin = new Vector2(-8f, -8f); frame.offsetMax = new Vector2(8f, 8f);
                // 枠は 4 本の線で描く（Outline だとカード全体がロールの色で塗られて、職業名が見えなくなる）
                for (int edge = 0; edge < 4; edge++)
                {
                    var line = NewRect("Edge", frame);
                    bool vertical = edge >= 2;
                    line.anchorMin = vertical ? new Vector2(edge == 2 ? 0f : 1f, 0f) : new Vector2(0f, edge == 0 ? 0f : 1f);
                    line.anchorMax = vertical ? new Vector2(edge == 2 ? 0f : 1f, 1f) : new Vector2(1f, edge == 0 ? 0f : 1f);
                    line.pivot = new Vector2(edge == 3 ? 1f : 0f, edge == 1 ? 1f : 0f);
                    line.sizeDelta = vertical ? new Vector2(8f, 0f) : new Vector2(0f, 8f);
                    line.anchoredPosition = Vector2.zero;
                    var lineImage = line.gameObject.AddComponent<UnityEngine.UI.Image>();
                    lineImage.color = role.roleColor; lineImage.raycastTarget = false;
                }
                frame.SetAsFirstSibling();
                var glow = NewRect("Glow", frame);
                glow.anchorMin = Vector2.zero; glow.anchorMax = Vector2.one; glow.offsetMin = glow.offsetMax = Vector2.zero;
                var glowImage = glow.gameObject.AddComponent<UnityEngine.UI.Image>();
                var c = role.roleColor; glowImage.color = new Color(c.r, c.g, c.b, 0.12f); glowImage.raycastTarget = false;
                glow.SetAsFirstSibling();
                var hint = NewText(card, font, "▶ もう一度クリック（Enter）で決定", 26, Color.white);
                hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                hint.rectTransform.sizeDelta = new Vector2(480f, 40f);
                hint.rectTransform.anchoredPosition = new Vector2(0f, -34f);
                frame.gameObject.SetActive(false); hint.gameObject.SetActive(false);
                roleCards.Add((card, frame.gameObject, hint.gameObject));
            }

            var back = NewRect("Back", root);
            back.anchorMin = back.anchorMax = new Vector2(0.5f, 0.5f);
            back.sizeDelta = new Vector2(260f, 70f);
            back.anchoredPosition = new Vector2(0f, -320f);
            var backImage = back.gameObject.AddComponent<UnityEngine.UI.Image>();
            backImage.color = new Color(0.4f, 0.27f, 0.17f, 0.95f);
            var backButton = back.gameObject.AddComponent<UnityEngine.UI.Button>();
            backButton.onClick.AddListener(HideRoles);
            var backText = NewText(back, font, "もどる", 32, Color.white);
            backText.rectTransform.anchorMin = Vector2.zero; backText.rectTransform.anchorMax = Vector2.one;
            backText.rectTransform.offsetMin = backText.rectTransform.offsetMax = Vector2.zero;

            // 矢印キー：左右でカード、下で「もどる」、上でカードへ
            var cards = root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b => b != backButton).ToList();
            for (int i = 0; i < cards.Count; i++)
            {
                var nav = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit };
                nav.selectOnLeft = cards[(i + cards.Count - 1) % cards.Count];
                nav.selectOnRight = cards[(i + 1) % cards.Count];
                nav.selectOnDown = backButton;
                nav.selectOnUp = backButton;
                cards[i].navigation = nav;
            }
            backButton.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = cards.FirstOrDefault(), selectOnDown = cards.FirstOrDefault() };

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static void Place(RectTransform rect, float x, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(-40f, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static UnityEngine.UI.Text NewText(Transform parent, Font font, string value, int size, Color color)
        {
            var rect = NewRect("Text", parent);
            var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
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

        public void EnterVillage()
        {
            onEntryRequested.Invoke(false);
        }

        public void EnterTrialVillage()
        {
            onEntryRequested.Invoke(true);
        }

        public void OpenSettings()
        {
            titlePanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            settingsPanel.SetActive(false);
            titlePanel.SetActive(true);
        }
    }

    // 職業カードを押した瞬間に、そのカードがもう選ばれていたかを覚えておく（1 回目は選ぶだけ、2 回目で決定）
    internal class RoleCardPress : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler
    {
        public bool WasSelected { get; private set; }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            WasSelected = es != null && es.currentSelectedGameObject == gameObject;
        }
    }
}
