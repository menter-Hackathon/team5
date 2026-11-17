using Team5.Core;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 画面右上に「お題：素振りをする（残り 25 秒）」と進み具合のバーを出す仮UI
    public class TaskView : MonoBehaviour
    {
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text requestLabel;
        [SerializeField] private RectTransform progressFill; // anchorMax.x を 0〜1 にする

        private TaskManager tasks;
        private string resultMessage = "";

        private void Start()
        {
            var context = GameSceneContext.Instance;
            if (context == null || context.Tasks == null)
            {
                Debug.LogWarning("GameSceneContext か TaskManager がないのでお題を表示できません。", this);
                enabled = false;
                return;
            }
            tasks = context.Tasks;
            tasks.OnTaskAssigned += HandleAssigned;
            tasks.OnTaskCompleted += HandleCompleted;
            tasks.OnTaskFailed += HandleFailed;
        }

        private void OnDestroy()
        {
            if (tasks == null) return;
            tasks.OnTaskAssigned -= HandleAssigned;
            tasks.OnTaskCompleted -= HandleCompleted;
            tasks.OnTaskFailed -= HandleFailed;
        }

        private void HandleAssigned(TaskInstance task) => resultMessage = "";
        private void HandleCompleted(TaskInstance task) => resultMessage = $"達成！：{task.Definition.title}";
        private void HandleFailed(TaskInstance task) => resultMessage = $"失敗…：{task.Definition.title}";

        private void Update()
        {
            var task = tasks.Current;
            if (task == null || task.State != TaskState.Active)
            {
                titleLabel.text = string.IsNullOrEmpty(resultMessage) ? "次のお題を待っています" : resultMessage;
                requestLabel.text = "";
                SetProgress(task != null && task.State == TaskState.Completed ? 1f : 0f);
                return;
            }

            var definition = task.Definition;
            titleLabel.text = definition.timeLimitSeconds > 0f
                ? $"お題：{definition.title}（残り {Mathf.CeilToInt(task.TimeRemaining)} 秒）"
                : $"お題：{definition.title}";
            requestLabel.text = definition.request;
            SetProgress(task.Progress);
        }

        private void SetProgress(float value)
        {
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }
    }
}
