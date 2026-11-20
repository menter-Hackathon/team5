using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 結果画面（ui-design.md 4 章・win-lose.md）。4 つの結果で背景・見出し・一言・絵・ヒントを変える
    public class ResultView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image picture;
        [SerializeField] private Image labelChip;
        [SerializeField] private Text labelText;
        [SerializeField] private Text heading;
        [SerializeField] private Text line;
        [SerializeField] private Text hint;
        [SerializeField] private Text extra;

        [Header("数値のカード")]
        [SerializeField] private Text individualityValue;
        [SerializeField] private RectTransform individualityFill;
        [SerializeField] private Text peaceValue;
        [SerializeField] private RectTransform peaceFill;
        [SerializeField] private Text tasksValue;
        [SerializeField] private Text caughtValue;

        [Header("結果の絵")]
        [SerializeField] private Sprite clearBackground;
        [Header("クリア専用の通知書と3Dキャラクター")]
        [SerializeField] private Sprite clearNoticeBackground;
        [SerializeField] private Sprite clearWoodButton;
        [SerializeField] private GameObject clearPlayerPrefab;
        [SerializeField] private Shader clearPortraitShader;
        [SerializeField] private Sprite clearPicture;
        [SerializeField] private Sprite peacePicture;
        [SerializeField] private Sprite fadedPicture;
        [SerializeField] private Sprite timeUpPicture;

        [Header("ゲームオーバーの場面に並べる村人（村長・見張り・村人の順）")]
        [SerializeField] private GameObject[] villagerModels;
        [SerializeField] private Material[] villagerMaterials;
        [SerializeField] private RuntimeAnimatorController[] villagerControllers;

        [SerializeField] private RoleDefinition fallbackRole; // Result シーンを直接再生したとき
        [SerializeField] private GameBalance balance;          // 数値カードの最大値

        private static readonly Color Ink = new(0.231f, 0.208f, 0.188f);
        private static readonly Color Accent = new(1f, 0.784f, 0.239f);
        private static readonly Color Warning = new(0.839f, 0.235f, 0.259f);
        private static readonly Color Dark = new(0.18f, 0.169f, 0.2f);
        private static readonly Color Fog = new(0.851f, 0.827f, 0.78f);
        private static readonly Color Stone = new(0.604f, 0.588f, 0.561f);
        private static readonly Color Shadow = new(0.431f, 0.416f, 0.525f);

        private void Start()
        {
            var manager = GameManager.Instance;
            var result = manager != null ? manager.LastResult : GameResult.None;
            var summary = manager != null ? manager.LastSummary : default;
            var role = manager != null && manager.CurrentRole != null ? manager.CurrentRole : fallbackRole;
            string wanted = role != null && !string.IsNullOrWhiteSpace(role.displayName) ? role.displayName : "なりたい自分";
            string imposed = role != null && !string.IsNullOrWhiteSpace(role.imposedRoleName) ? role.imposedRoleName : "勇者";

            bool clear = result == GameResult.Clear;
            labelText.text = clear ? "クリア" : "ゲームオーバー";

            ConfigureTextLayout();

            switch (result)
            {
                case GameResult.Clear:
                    Set(new Color(0.98f, 0.93f, 0.81f), clearPicture, Ink, Accent, Ink,
                        "これが、わたしらしさ。",
                        $"{imposed}になるはずだった。\n" + ClearResultText.Story(role),
                        $"なりたかった自分　{wanted}");
                    SetNotice("らしさ達成", ClearResultText.ChiefLineOneLine(role));
                    break;
                case GameResult.GameOverPeace:
                    Set(Fog, peacePicture, Ink, Warning, Color.white, "見つかった……", "", "");
                    SetNotice("追放", "", $"除籍通知　本日をもって、村の名簿から除籍します。");
                    break;
                case GameResult.GameOverTimeUp:
                    Set(new Color(0.88f, 0.79f, 0.65f), timeUpPicture, Ink, Dark, Color.white, "日が沈んだ……", "", "");
                    SetNotice("現状維持", "", $"現状維持通知　引き続き、{imposed}として登録します。");
                    break;
                case GameResult.GameOverFaded:
                    Set(Fog, fadedPicture, Ink, Stone, Color.white, "色が消えた……", "", "");
                    SetNotice("適合", "", $"適合証明　『{imposed}らしさ』のすべてに適合しました。");
                    break;
                default:
                    Set(Shadow, timeUpPicture, Color.white, Dark, Color.white,
                        "結果がまだありません", "タイトルからゲームを開始してください。", "");
                    labelText.text = "未記録";
                    break;
            }

            // 背景を白で描画し、単色用の色が画像に乗らないようにする。
            // 未設定のシーンでは、従来の単色背景をそのまま使う。
            background.sprite = clear ? clearBackground : null;
            background.type = Image.Type.Simple;
            if (background.sprite != null) background.color = Color.white;

            float individualityMax = balance != null ? balance.individualityMax : 100f;
            float peaceMax = balance != null ? balance.peaceMax : 100f;
            individualityValue.text = $"{Mathf.RoundToInt(summary.individuality)}";
            peaceValue.text = $"{Mathf.RoundToInt(summary.peace)}";
            SetWidth(individualityFill, summary.individuality / Mathf.Max(1f, individualityMax));
            SetWidth(peaceFill, summary.peace / Mathf.Max(1f, peaceMax));
            tasksValue.text = $"{summary.tasksCompleted} 回";
            caughtValue.text = $"{summary.timesCaught} 回";

            int seconds = Mathf.RoundToInt(summary.elapsedSeconds);
            extra.text = result == GameResult.None ? "" : clear
                ? $"かかった時間  {seconds / 60}:{seconds % 60:00}"
                : $"{wanted}への進み具合  {Mathf.RoundToInt(summary.roleProgress)} / 100";
            if (clear && clearNoticeBackground != null && clearPlayerPrefab != null)
            {
                gameObject.AddComponent<ClearResultPresentation>().Show(clearNoticeBackground,
                    clearWoodButton, clearPlayerPrefab, role, summary, balance, heading.font, clearPortraitShader);
                return;
            }
            Simplify(result, summary);
            ShowStage(role, result);
            PlayEntrance(clear);
        }

        // 小見出し・説明の文・ラベルはやめて、見出しの一言と数字だけにする
        private void Simplify(GameResult result, GameResultSummary summary)
        {
            labelChip.gameObject.SetActive(false);
            line.gameObject.SetActive(false);
            extra.gameObject.SetActive(false);
            SetTitle("Individuality", "個性");
            SetTitle("Peace", "平和度");
            SetTitle("Tasks", "お題");
            SetTitle("Caught", "発覚");
            heading.resizeTextMaxSize = 64;
            heading.fontSize = 64;
        }

        private void SetTitle(string card, string title)
        {
            if (transform.Find(card + "/Title") is Transform child && child.TryGetComponent(out Text text)) text.text = title;
        }

        // 平面の絵のかわりに、ゲームのモデルを並べた場面を出す
        private void ShowStage(RoleDefinition role, GameResult result)
        {
            if (clearPlayerPrefab == null || picture == null || result == GameResult.None) return;
            var villagers = new ResultCharacterPreview.Villager[villagerModels != null ? villagerModels.Length : 0];
            for (int i = 0; i < villagers.Length; i++)
            {
                villagers[i].model = villagerModels[i];
                villagers[i].material = villagerMaterials != null && i < villagerMaterials.Length ? villagerMaterials[i] : null;
                villagers[i].controller = villagerControllers != null && i < villagerControllers.Length ? villagerControllers[i] : null;
            }
            picture.enabled = false;
            var view = new GameObject("Stage3D", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)view.transform;
            rect.SetParent(picture.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            view.GetComponent<RawImage>().raycastTarget = false;
            view.AddComponent<ResultCharacterPreview>().Initialize(clearPlayerPrefab, role, clearPortraitShader, result, villagers);
        }

        // 結果の一行（クリアは村長の一言、ゲームオーバーは村役場の通知を 1 行で）
        private void SetNotice(string stamp, string chiefLine, string notice = null)
        {
            labelText.text = stamp;
            hint.text = !string.IsNullOrEmpty(notice) ? notice : "村長「" + chiefLine + "」";
            hint.color = Ink;
        }

        private void ConfigureTextLayout()
        {
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 28;
            heading.resizeTextMaxSize = 46;
            line.fontSize = 25;
            line.resizeTextForBestFit = true;
            line.resizeTextMinSize = 20;
            line.resizeTextMaxSize = 25;
            line.rectTransform.anchoredPosition = new Vector2(930, -325);
            line.rectTransform.sizeDelta = new Vector2(900, 90);
            hint.fontSize = 22;
            hint.resizeTextForBestFit = true;
            hint.resizeTextMinSize = 18;
            hint.resizeTextMaxSize = 22;
            // hint は横に伸縮する子Text。sizeDeltaに幅を入れると親の幅に加算される。
            if (hint.transform.parent is RectTransform hintPanel)
                hintPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80f);
            hint.rectTransform.offsetMin = new Vector2(24f, 8f);
            hint.rectTransform.offsetMax = new Vector2(-24f, -8f);
        }

        // 絵 → ラベル → 見出し → 一言 → 数値 → ヒント → ボタン の順に出す
        private void PlayEntrance(bool clear)
        {
            string[] order = { "Label", "Heading", "Line", "Individuality", "Peace", "Tasks", "Caught", "Extra", "Hint", "RetryButton", "TitleButton" };
            for (int i = 0; i < order.Length; i++)
            {
                if (transform.Find(order[i]) is RectTransform rect) UiFx.SlideIn(this, rect, new Vector2(60f, 0f), 0.4f, 0.15f + i * 0.07f);
            }
            if (transform.Find("Picture") is RectTransform art)
            {
                UiFx.SlideIn(this, art, new Vector2(0f, -40f), 0.6f);
                if (clear) StartCoroutine(Celebrate(art));
            }
        }

        // クリアのときだけ、絵がゆっくり弾み続ける
        private System.Collections.IEnumerator Celebrate(RectTransform art)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            while (true)
            {
                UiFx.Punch(this, art, 0.04f, 0.6f);
                yield return new WaitForSecondsRealtime(1.4f);
            }
        }

        private void Set(Color back, Sprite art, Color textColor, Color chipColor, Color chipTextColor, string head, string sub, string tip)
        {
            background.color = back;
            if (art != null) picture.sprite = art;
            labelChip.color = chipColor;
            labelText.color = chipTextColor;
            heading.text = head;
            heading.color = textColor;
            line.text = sub;
            line.color = textColor;
            extra.color = textColor;
            hint.text = tip;
        }

        private static void SetWidth(RectTransform rect, float value)
        {
            var max = rect.anchorMax;
            max.x = Mathf.Clamp01(value);
            rect.anchorMax = max;
        }

        // 「もう一度あそぶ」ボタンから呼ぶ
        public void OnRetryPressed()
        {
            var manager = GameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("GameManager がありません。Title シーンから再生してください。", this);
                return;
            }
            manager.StartGame(manager.CurrentRole != null ? manager.CurrentRole : fallbackRole);
        }

        // 「タイトルへ」ボタンから呼ぶ
        public void OnBackToTitlePressed()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager がありません。Title シーンから再生してください。", this);
                return;
            }
            GameManager.Instance.ReturnToTitle();
        }
    }
}
