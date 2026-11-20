using System;
using Team5.Core;
using Team5.Data;
using UnityEngine;

namespace Team5.Tasks
{
    // 表のお題を順番に出し、成功・失敗を管理する
    public class TaskManager : MonoBehaviour
    {
        public TaskInstance Current { get; private set; }
        public int CompletedCount { get; private set; }
        public event Action<TaskInstance> OnTaskAssigned;
        public event Action<TaskInstance> OnTaskCompleted;
        public event Action<TaskInstance> OnTaskFailed;

        private RoleDefinition role;
        private int nextIndex;
        private float waitRemaining; // 次のお題が出るまでの秒数
        private float lastSpotActionTime = -1f;
        private bool running;

        public void Begin(RoleDefinition role)
        {
            this.role = role;
            nextIndex = 0;
            CompletedCount = 0;
            Current = null;
            running = role != null && role.imposedTasks != null && role.imposedTasks.Count > 0;
            if (running) AssignNext();
            else Debug.LogWarning("お題が1つもないロールです。", this);
        }

        private void Update()
        {
            if (!running) return;

            if (IsActive())
            {
                if (Current.Definition.timeLimitSeconds <= 0f) return; // 0 なら無制限
                if (Time.time - lastSpotActionTime < 0.15f) return;   // その場でお題をしている間（食べる・素振りなど）は時間を止める
                Current.TimeRemaining -= Time.deltaTime;
                if (Current.TimeRemaining <= 0f) Fail();
            }
            else
            {
                waitRemaining -= Time.deltaTime;
                if (waitRemaining <= 0f) AssignNext();
            }
        }

        // ActionAtSpot の進行
        public void ReportSpotAction(string spotId, float seconds)
        {
            if (!IsActive(TaskType.ActionAtSpot) || Current.Definition.targetSpotId != spotId) return;

            lastSpotActionTime = Time.time;
            float actionSeconds = Mathf.Max(Current.Definition.actionSeconds, 0.01f);
            Current.Progress = Mathf.Min(Current.Progress + seconds / actionSeconds, 1f);
            if (Current.Progress >= 1f) Complete();
        }

        // BringItem
        public bool TryDeliver(ItemDefinition item)
        {
            if (!IsActive(TaskType.BringItem) || item == null || Current.Definition.requiredItem != item) return false;

            var inventory = GameSceneContext.Instance != null ? GameSceneContext.Instance.Inventory : null;
            if (inventory == null || !inventory.Remove(item, Current.Definition.requiredCount)) return false;
            Complete();
            return true;
        }

        // TalkToNpc
        public void ReportTalk(string npcId)
        {
            if (!IsActive(TaskType.TalkToNpc) || Current.Definition.targetSpotId != npcId) return;
            Complete();
        }

        // お題をどれだけ「らしく」こなせたか（報酬の倍率）。判定方法は PL-05 で決まるので、今は常に 1
        public float EvaluateScore(TaskInstance task)
        {
            return 1f;
        }

        private bool IsActive() => Current != null && Current.State == TaskState.Active;

        private bool IsActive(TaskType type) => IsActive() && Current.Definition.type == type;

        private void AssignNext()
        {
            var definition = role.imposedTasks[nextIndex];
            nextIndex = (nextIndex + 1) % role.imposedTasks.Count; // 全部出し終わったら最初に戻る
            Current = new TaskInstance
            {
                Definition = definition,
                State = TaskState.Active,
                TimeRemaining = definition.timeLimitSeconds,
            };
            OnTaskAssigned?.Invoke(Current);
        }

        private void Complete()
        {
            var definition = Current.Definition;
            Current.State = TaskState.Completed;
            Current.Progress = 1f;
            float score = EvaluateScore(Current);
            AddParameter(ParameterType.Peace, definition.peaceReward * score, $"お題完了：{definition.title}");
            AddParameter(ParameterType.Individuality, -definition.individualityCost, $"お題完了：{definition.title}");
            var inventory = GameSceneContext.Instance != null ? GameSceneContext.Instance.Inventory : null;
            if (definition.rewardItem != null && inventory != null) inventory.Add(definition.rewardItem, definition.rewardCount);
            CompletedCount++;
            StartWaiting();
            OnTaskCompleted?.Invoke(Current);
        }

        private void Fail()
        {
            var definition = Current.Definition;
            Current.State = TaskState.Failed;
            Current.TimeRemaining = 0f;
            AddParameter(ParameterType.Peace, -definition.failPeacePenalty, $"お題失敗：{definition.title}");
            StartWaiting();
            OnTaskFailed?.Invoke(Current);
        }

        private void StartWaiting()
        {
            var context = GameSceneContext.Instance;
            waitRemaining = context != null && context.Balance != null ? context.Balance.taskIntervalSeconds : 0f;
        }

        private static void AddParameter(ParameterType type, float delta, string reason)
        {
            var context = GameSceneContext.Instance;
            if (context != null && context.Parameters != null) context.Parameters.Add(type, delta, reason);
        }
    }
}
