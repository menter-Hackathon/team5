using System;
using Team5.Core;
using Team5.Data;
using Team5.Player;
using UnityEngine;

namespace Team5.Tasks
{
    // ActionAtSpot のお題をこなす場所。E を押している間だけ進む
    public class TaskSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private string spotId;

        public string SpotId => spotId;
        public bool IsActing { get; private set; }
        public event Action<bool> OnActing; // 動作の開始 / 終了（AN-01 が使う）

        private PlayerInteractor actor;

        public string Prompt
        {
            get
            {
                var task = CurrentTask();
                return task != null ? $"[E長押し] {task.Definition.title}" : "";
            }
        }

        public bool IsSuspicious => false; // 表のお題は見られていてもよい

        public bool CanInteract(PlayerInteractor by) => CurrentTask() != null;

        public void Interact(PlayerInteractor by)
        {
            if (IsActing || CurrentTask() == null) return;
            actor = by;
            SetActing(true);
        }

        private void Update()
        {
            if (!IsActing) return;

            bool keepGoing = actor != null && actor.IsHoldingInteract && ReferenceEquals(actor.Current, this) && CurrentTask() != null;
            if (!keepGoing)
            {
                SetActing(false); // 離したら中断（進み具合は残る）
                return;
            }
            GameSceneContext.Instance.Tasks.ReportSpotAction(spotId, Time.deltaTime);
            if (CurrentTask() == null) SetActing(false); // 完了した
        }

        private void OnDisable()
        {
            if (IsActing) SetActing(false);
        }

        private void SetActing(bool acting)
        {
            IsActing = acting;
            if (actor != null && actor.Mover != null) actor.Mover.CanMove = !acting;
            if (!acting) actor = null;
            OnActing?.Invoke(acting);
        }

        // このお題の対象になっている、進行中の ActionAtSpot のお題
        private TaskInstance CurrentTask()
        {
            var tasks = GameSceneContext.Instance != null ? GameSceneContext.Instance.Tasks : null;
            var task = tasks != null ? tasks.Current : null;
            if (task == null || task.State != TaskState.Active) return null;
            return task.Definition.type == TaskType.ActionAtSpot && task.Definition.targetSpotId == spotId ? task : null;
        }
    }
}
