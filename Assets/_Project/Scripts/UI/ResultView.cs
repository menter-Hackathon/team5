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
        [SerializeField] private Sprite clearPicture;
        [SerializeField] private Sprite peacePicture;
        [SerializeField] private Sprite fadedPicture;
        [SerializeField] private Sprite timeUpPicture;

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
                    bool ramenChef = role != null && role.roleId == "ramen_chef";
                    Set(new Color(0.98f, 0.93f, 0.81f), clearPicture, Ink, Accent, Ink,
                        "これが、わたしらしさ。",
                        $"{imposed}になるはずだった。\n" + (ramenChef
                            ? "けれど、いま手にしているのは、自分で作った一杯だ。"
                            : "けれど、いまここにいるのは、自分で選んだわたしだ。"),
                        $"なりたかった自分　{wanted}");
                    SetNotice("らしさ達成", ramenChef
                        ? "……まあ、一杯くらいは、いただこうか。"
                        : "……それが、お前の選んだ道か。");
                    break;
                case GameResult.GameOverPeace:
                    Set(Fog, peacePicture, Ink, Warning, Color.white,
                        "あなたらしさを、確認しました。",
                        "あなたが何を望んでいるのか、村にもよく分かりました。\nその望みは、この村では認められていません。",
                        "平和度が0になりました。");
                    SetNotice("不適合", "好きにしてよいとは言ったが、そこまでとは言っておらんよ。");
                    break;
                case GameResult.GameOverTimeUp:
                    Set(new Color(0.88f, 0.79f, 0.65f), timeUpPicture, Ink, Dark, Color.white,
                        "明日も、あなたの役割が待っています。",
                        "なりたかった自分のことは、また明日考えましょう。\n明日も、今日と同じ仕事が待っています。",
                        "制限時間になりました。");
                    SetNotice("本日終了", "夢は逃げんよ。明日もまず、村の仕事を頼むぞ。");
                    break;
                case GameResult.GameOverFaded:
                    Set(Fog, fadedPicture, Ink, Stone, Color.white,
                        "立派な村人になりました。",
                        $"あなたは、期待されたとおりの{imposed}になりました。\nもう、自分が何になりたかったかで悩む必要はありません。",
                        "個性が0になりました。");
                    SetNotice("適応完了", "うむ。ようやく、お前らしくなったな。");
                    break;
                default:
                    Set(Shadow, timeUpPicture, Color.white, Dark, Color.white,
                        "結果がまだありません", "タイトルからゲームを開始してください。", "");
                    labelText.text = "未記録";
                    break;
            }

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
            PlayEntrance(clear);
        }

        private void SetNotice(string stamp, string chiefLine)
        {
            labelText.text = stamp;
            hint.text += "\n村長「" + chiefLine + "」";
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
            hint.rectTransform.sizeDelta = new Vector2(880, 80);
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
