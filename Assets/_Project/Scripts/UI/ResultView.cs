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
            var result = manager != null ? manager.LastResult : GameResult.Clear;
            var summary = manager != null ? manager.LastSummary : default;
            var role = manager != null && manager.CurrentRole != null ? manager.CurrentRole : fallbackRole;
            string wanted = role != null ? role.displayName : "なりたい自分";
            string imposed = role != null ? role.imposedRoleName : "勇者";

            bool clear = result == GameResult.Clear;
            labelText.text = clear ? "クリア" : "ゲームオーバー";

            switch (result)
            {
                case GameResult.Clear:
                    Set(role != null ? role.roleColor : Accent, clearPicture, Ink, Accent, Ink,
                        $"{wanted}になれた！", "これが わたしらしさ。", "言われたこともこなしつつ、自分の『好き』を守れた！");
                    break;
                case GameResult.GameOverPeace:
                    Set(Dark, peacePicture, Color.white, Warning, Color.white,
                        "村にばれてしまった…", $"『{imposed}らしくない！』", "らしくない行動は、だれも見ていないときに。お題をこなすと平和度が戻るよ");
                    break;
                case GameResult.GameOverFaded:
                    Set(Fog, fadedPicture, Ink, Stone, Color.white,
                        "色が消えてしまった…", $"言われたとおりの{imposed}になった。", "お題ばかりだと色が消えちゃう。こっそりクラフトして個性を取り戻そう");
                    break;
                default:
                    Set(Shadow, timeUpPicture, Color.white, Dark, Color.white,
                        "時間切れ…", $"{wanted}には、まだなれなかった。", "素材を先に集めておくと、クラフトが早く終わるよ");
                    break;
            }

            float individualityMax = balance != null ? balance.individualityMax : 100f;
            float peaceMax = balance != null ? balance.peaceMax : 100f;
            individualityValue.text = $"{Mathf.RoundToInt(summary.individuality)}";
            peaceValue.text = $"{Mathf.RoundToInt(summary.peace)}";
            SetWidth(individualityFill, summary.individuality / individualityMax);
            SetWidth(peaceFill, summary.peace / peaceMax);
            tasksValue.text = $"{summary.tasksCompleted} 回";
            caughtValue.text = $"{summary.timesCaught} 回";

            int seconds = Mathf.RoundToInt(summary.elapsedSeconds);
            extra.text = clear
                ? $"かかった時間  {seconds / 60}:{seconds % 60:00}"
                : $"{wanted}への進み具合  {Mathf.RoundToInt(summary.roleProgress)} / 100";
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
