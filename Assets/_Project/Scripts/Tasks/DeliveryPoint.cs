using Team5.Core;
using Team5.Data;
using Team5.Player;
using UnityEngine;

namespace Team5.Tasks
{
    // BringItem のお題で、頼まれた物を渡す場所（村長の家の戸口など）
    public class DeliveryPoint : MonoBehaviour, IInteractable
    {
        public string Prompt
        {
            get
            {
                var task = CurrentTask();
                return task != null ? $"渡す：{task.Definition.requiredItem.displayName}" : "";
            }
        }

        public bool IsSuspicious => false;

        // 頼まれた数だけ持っているときだけ渡せる
        public bool CanInteract(PlayerInteractor by)
        {
            var task = CurrentTask();
            var inventory = GameSceneContext.Instance != null ? GameSceneContext.Instance.Inventory : null;
            return task != null && inventory != null && inventory.Count(task.Definition.requiredItem) >= task.Definition.requiredCount;
        }

        public void Interact(PlayerInteractor by)
        {
            var task = CurrentTask();
            if (task != null) GameSceneContext.Instance.Tasks.TryDeliver(task.Definition.requiredItem);
        }

        private static TaskInstance CurrentTask()
        {
            var tasks = GameSceneContext.Instance != null ? GameSceneContext.Instance.Tasks : null;
            var task = tasks != null ? tasks.Current : null;
            if (task == null || task.State != TaskState.Active) return null;
            return task.Definition.type == TaskType.BringItem && task.Definition.requiredItem != null ? task : null;
        }
    }
}
