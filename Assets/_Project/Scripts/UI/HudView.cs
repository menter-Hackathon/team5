using System;
using Team5.Core;
using Team5.Data;
using Team5.NPC;
using Team5.Player;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // ゲーム中の表示（ui-design.md 2 章）。仮 UI の TaskView・WatchedIndicatorView・InteractPromptView をまとめたもの
    public class HudView : MonoBehaviour
    {
        [Serializable]
        public class Gauge
        {
            public RectTransform fill;   // anchorMax.x を 0〜1 にする
            public RectTransform ghost;  // 下がった分を警告色で残す
            public Text value;
            public Text delta;           // 「−25」
            public Graphic icon;
            [NonSerialized] public float shown;
            [NonSerialized] public float target;
            [NonSerialized] public float ghostValue;
            [NonSerialized] public float ghostHoldUntil;
            [NonSerialized] public float deltaHideTime;
        }

        [Header("パラメーター")]
        [SerializeField] private Gauge individuality;
        [SerializeField] private Gauge peace;
        [SerializeField] private Gauge roleProgress;
        [SerializeField] private Text roleProgressLabel;

        [Header("お題")]
        [SerializeField] private GameObject taskBody;
        [SerializeField] private Text taskTitle;
        [SerializeField] private Text taskHint;
        [SerializeField] private Text taskReward;
        [SerializeField] private RectTransform taskBarFill;
        [SerializeField] private Image taskBarImage;
        [SerializeField] private Text taskSeconds;
        [SerializeField] private Text taskWaiting;

        [Header("残り時間")]
        [SerializeField] private Text timerText;
        [SerializeField] private Graphic timerIcon;

        [Header("見られているか")]
        [SerializeField] private Image watchedCircle;
        [SerializeField] private Image watchedEye;
        [SerializeField] private Text watchedTitle;
        [SerializeField] private Text watchedSub;
        [SerializeField] private Sprite eyeOpen;
        [SerializeField] private Sprite eyeClosed;
        [SerializeField] private Sprite circle;
        [SerializeField] private Sprite ring;
        [SerializeField] private CanvasGroup vignette;
        [SerializeField] private GameObject offRoad;
        [SerializeField] private Text offRoadText;

        [Header("見つかった通知")]
        [SerializeField] private RectTransform caughtToast;
        [SerializeField] private CanvasGroup caughtGroup;
        [SerializeField] private Text caughtPenalty;

        [Header("操作のヒント")]
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private Text promptText;

        [Header("なりたい姿")]
        [SerializeField] private Text roleName;

        private static readonly Color Warning = new(0.839f, 0.235f, 0.259f);       // #D63C42
        private static readonly Color WarningOnDark = new(1f, 0.541f, 0.557f);     // #FF8A8E
        private static readonly Color Ink = new(0.231f, 0.208f, 0.188f);           // #3B3530
        private static readonly Color OldWood = new(0.541f, 0.459f, 0.388f);       // #8A7563
        private static readonly Color Stone = new(0.604f, 0.588f, 0.561f);         // #9A968F

        private const float GaugeSeconds = 0.3f;
        private const float DeltaSeconds = 1.5f;
        private const float CaughtSeconds = 2f;

        private GameSceneContext context;
        private PlayerInteractor interactor;
        private int lastTimerSeconds = -1;
        private int lastTaskSeconds = -1;
        private float vignetteTarget;
        private float caughtTime = -100f;
        private float promptTarget;
        private float caughtToastY;

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context == null)
            {
                Debug.LogWarning("GameSceneContext がないので HUD を出せません。", this);
                enabled = false;
                return;
            }

            var role = context.Role;
            if (role != null)
            {
                roleProgressLabel.text = $"{role.displayName}への進み具合";
                roleName.text = role.displayName;
                individuality.icon.color = role.roleColor;
                individuality.fill.GetComponent<Image>().color = role.roleColor;
                roleProgress.icon.color = role.roleColor;
            }

            context.Parameters.OnChanged += HandleParameterChanged;
            context.Timer.OnTick += HandleTick;
            context.Tasks.OnTaskAssigned += HandleTaskChanged;
            context.Tasks.OnTaskCompleted += HandleTaskChanged;
            context.Tasks.OnTaskFailed += HandleTaskChanged;
            context.Surveillance.OnPhaseChanged += HandlePhaseChanged;
            context.Surveillance.OnCaught += HandleCaught;
            context.Inventory.OnChanged += HandleInventoryChanged;
            if (context.Player != null && context.Player.TryGetComponent(out interactor)) interactor.OnFocusChanged += HandleFocusChanged;

            InitGauge(individuality, ParameterType.Individuality);
            InitGauge(peace, ParameterType.Peace);
            InitGauge(roleProgress, ParameterType.RoleProgress);
            HandleTick(context.Timer.Remaining);
            HandleTaskChanged(context.Tasks.Current);
            HandlePhaseChanged(context.Surveillance.CurrentPhase);
            HandleFocusChanged(interactor != null ? interactor.Current : null);

            caughtToastY = caughtToast.anchoredPosition.y;
            caughtGroup.alpha = 0f;
            vignette.alpha = 0f;
            promptGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (context == null) return;
            context.Parameters.OnChanged -= HandleParameterChanged;
            context.Timer.OnTick -= HandleTick;
            context.Tasks.OnTaskAssigned -= HandleTaskChanged;
            context.Tasks.OnTaskCompleted -= HandleTaskChanged;
            context.Tasks.OnTaskFailed -= HandleTaskChanged;
            context.Surveillance.OnPhaseChanged -= HandlePhaseChanged;
            context.Surveillance.OnCaught -= HandleCaught;
            context.Inventory.OnChanged -= HandleInventoryChanged;
            if (interactor != null) interactor.OnFocusChanged -= HandleFocusChanged;
        }

        // ---------- パラメーター ----------

        private void InitGauge(Gauge gauge, ParameterType type)
        {
            gauge.target = gauge.shown = gauge.ghostValue = context.Parameters.GetNormalized(type);
            gauge.value.text = Mathf.RoundToInt(context.Parameters.Get(type)).ToString();
            gauge.delta.gameObject.SetActive(false);
            SetWidth(gauge.fill, gauge.shown);
            SetWidth(gauge.ghost, gauge.ghostValue);
            UpdateValueColor(gauge, type);
        }

        private Gauge GaugeOf(ParameterType type) => type switch
        {
            ParameterType.Individuality => individuality,
            ParameterType.Peace => peace,
            _ => roleProgress,
        };

        private void HandleParameterChanged(ParameterType type, float before, float after)
        {
            var gauge = GaugeOf(type);
            gauge.target = context.Parameters.GetNormalized(type);
            gauge.value.text = Mathf.RoundToInt(after).ToString();
            UpdateValueColor(gauge, type);

            float diff = after - before;
            gauge.delta.text = diff > 0 ? $"+{Mathf.RoundToInt(diff)}" : $"−{Mathf.RoundToInt(-diff)}";
            gauge.delta.color = diff > 0 ? new Color(0.557f, 0.608f, 0.498f) : WarningOnDark;
            gauge.delta.gameObject.SetActive(true);
            gauge.deltaHideTime = Time.time + DeltaSeconds;
            if (diff < 0) gauge.ghostHoldUntil = Time.time + GaugeSeconds; // 下がった分をしばらく残す
        }

        // 平和度・個性が 25 以下で数値を警告色に
        private void UpdateValueColor(Gauge gauge, ParameterType type)
        {
            bool danger = type != ParameterType.RoleProgress && context.Parameters.Get(type) <= 25f;
            gauge.value.color = danger ? WarningOnDark : Color.white;
        }

        private void UpdateGauge(Gauge gauge, bool blink)
        {
            float speed = Time.deltaTime / GaugeSeconds;
            gauge.shown = Mathf.MoveTowards(gauge.shown, gauge.target, speed);
            SetWidth(gauge.fill, gauge.shown);

            if (gauge.ghostValue < gauge.shown || Time.time >= gauge.ghostHoldUntil)
                gauge.ghostValue = Mathf.MoveTowards(gauge.ghostValue, gauge.shown, speed);
            SetWidth(gauge.ghost, Mathf.Max(gauge.ghostValue, gauge.shown));

            if (gauge.delta.gameObject.activeSelf && Time.time >= gauge.deltaHideTime) gauge.delta.gameObject.SetActive(false);

            // 個性が 20 未満（足が遅い）の間は 0.5 秒ごとに点滅
            var fillImage = gauge.fill.GetComponent<Image>();
            float alpha = blink && Mathf.Repeat(Time.time, 1f) < 0.5f ? 0.35f : 1f;
            fillImage.canvasRenderer.SetAlpha(alpha);
            gauge.icon.canvasRenderer.SetAlpha(alpha);
        }

        private static void SetWidth(RectTransform rect, float value)
        {
            var max = rect.anchorMax;
            max.x = Mathf.Clamp01(value);
            rect.anchorMax = max;
        }

        // ---------- 残り時間 ----------

        private void HandleTick(float remaining)
        {
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds == lastTimerSeconds) return; // 秒が変わったときだけ文字を作る
            lastTimerSeconds = seconds;
            timerText.text = $"{seconds / 60}:{seconds % 60:00}";
            var color = seconds <= 30 ? WarningOnDark : Color.white;
            timerText.color = color;
            timerIcon.color = color;
        }

        // ---------- お題 ----------

        private void HandleTaskChanged(TaskInstance task)
        {
            lastTaskSeconds = -1;
            bool active = task != null && task.State == TaskState.Active;
            taskBody.SetActive(active);
            taskWaiting.gameObject.SetActive(!active);
            if (!active)
            {
                taskWaiting.text = task == null ? "次のお題を待っています"
                    : task.State == TaskState.Completed ? $"達成！ {task.Definition.title}"
                    : $"失敗… {task.Definition.title}";
                return;
            }

            var definition = task.Definition;
            taskTitle.text = $"お題：{definition.title}";
            taskHint.text = HintOf(definition);
            taskReward.text = $"こなすと 平和度 +{definition.peaceReward:0} / 個性 −{definition.individualityCost:0}";
            if (interactor != null) HandleFocusChanged(interactor.Current); // 同じ場所でも出す文が変わる
        }

        // 持ち物が変わったら「薪 1/3」を更新する
        private void HandleInventoryChanged()
        {
            var task = context.Tasks.Current;
            if (task != null && task.State == TaskState.Active && task.Definition.type == TaskType.BringItem)
                taskHint.text = HintOf(task.Definition);
        }

        private string HintOf(TaskDefinition definition)
        {
            switch (definition.type)
            {
                case TaskType.ActionAtSpot:
                    return $"{PlaceName(definition.targetSpotId)}で [E] 長押し（{definition.actionSeconds:0} 秒）";
                case TaskType.BringItem:
                    int have = context.Inventory != null ? context.Inventory.Count(definition.requiredItem) : 0;
                    return $"村長の家へ [E] 渡す（{definition.requiredItem.displayName} {have}/{definition.requiredCount}）";
                default:
                    return $"{PlaceName(definition.targetSpotId)}に [E] 話しかける";
            }
        }

        // 場所IDとNPCのIDの日本語名（tasks.md の場所の一覧・npc.md）
        private static string PlaceName(string id) => id switch
        {
            "training_ground" => "訓練場",
            "canteen" => "食堂",
            "chief_house" => "村長の家",
            "well" => "井戸",
            "field" => "畑",
            "forest" => "森",
            "home" => "実家",
            "forge" => "鍛冶場",
            "plaza" => "広場",
            "chief" => "村長",
            "guard" => "見張り",
            "cook" => "食堂のおかみ",
            _ => id,
        };

        private void UpdateTask()
        {
            var task = context.Tasks.Current;
            if (task == null || task.State != TaskState.Active) return;
            var definition = task.Definition;

            float limit = definition.timeLimitSeconds;
            if (limit <= 0f)
            {
                SetWidth(taskBarFill, task.Progress);
                taskSeconds.text = "";
                return;
            }
            float ratio = Mathf.Clamp01(task.TimeRemaining / limit);
            SetWidth(taskBarFill, ratio);
            bool hurry = ratio <= 1f / 3f;
            taskBarImage.color = hurry ? Warning : OldWood;

            int seconds = Mathf.CeilToInt(task.TimeRemaining);
            if (seconds == lastTaskSeconds) return;
            lastTaskSeconds = seconds;
            taskSeconds.text = $"{seconds} 秒";
            taskSeconds.color = hurry ? Warning : Ink;
        }

        // ---------- 見られているか ----------

        private void HandlePhaseChanged(Phase phase)
        {
            bool watched = phase == Phase.Omote;
            watchedCircle.sprite = watched ? circle : ring;
            watchedCircle.color = watched ? Warning : Stone;
            watchedEye.sprite = watched ? eyeOpen : eyeClosed;
            watchedTitle.text = watched ? "見られている！" : "だれも見ていない";
            watchedTitle.color = watched ? WarningOnDark : Color.white;
            watchedSub.text = watched ? "らしく ふるまおう" : "今なら こっそり動ける";
            vignetteTarget = watched ? 1f : 0f;
        }

        private void HandleCaught(VisionSensor watcher)
        {
            caughtPenalty.text = $"平和度 −{context.Balance.caughtPeacePenalty:0}";
            caughtTime = Time.time;
        }

        private void UpdateWatched()
        {
            // 見られた瞬間は 0.2 秒、見られなくなったら 0.3 秒でフェード
            float speed = Time.deltaTime / (vignetteTarget > vignette.alpha ? 0.2f : 0.3f);
            vignette.alpha = Mathf.MoveTowards(vignette.alpha, vignetteTarget, speed);

            bool off = ForbiddenZone.IsPlayerInside;
            if (offRoad.activeSelf != off) offRoad.SetActive(off);
            if (off) offRoadText.color = context.Surveillance.IsPlayerWatched ? WarningOnDark : Color.white;

            // 見つかった通知：0.15 秒でおりてきて 2 秒出し、0.3 秒で消える
            float t = Time.time - caughtTime;
            float alpha = t < 0.15f ? t / 0.15f : t < CaughtSeconds ? 1f : 1f - (t - CaughtSeconds) / 0.3f;
            caughtGroup.alpha = Mathf.Clamp01(alpha);
            float drop = t < 0.15f ? (1f - t / 0.15f) * 40f : 0f;
            caughtToast.anchoredPosition = new Vector2(caughtToast.anchoredPosition.x, caughtToastY + drop);
        }

        // ---------- 操作のヒント ----------

        private void HandleFocusChanged(IInteractable target)
        {
            bool visible = target != null && !string.IsNullOrEmpty(target.Prompt);
            promptTarget = visible ? 1f : 0f;
            if (visible) promptText.text = target.Prompt;
        }

        private void Update()
        {
            UpdateGauge(individuality, context.Parameters.Get(ParameterType.Individuality) < context.Balance.debuffThreshold);
            UpdateGauge(peace, false);
            UpdateGauge(roleProgress, false);
            UpdateTask();
            UpdateWatched();

            promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, promptTarget, Time.deltaTime / 0.1f);
        }
    }
}
