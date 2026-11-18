using Team5.Core;
using Team5.Data;
using Team5.Player;
using Team5.Tasks;
using UnityEngine;

namespace Team5.NPC
{
    // TalkToNpc のお題で話しかける相手
    public class NpcTalkable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcId;
        [SerializeField] private float talkPauseSeconds = 2f; // 話しかけたら NPC が止まる時間

        public string NpcId => npcId;

        public string Prompt => CurrentTask() != null ? "話しかける" : "";

        public bool IsSuspicious => false;

        public bool CanInteract(PlayerInteractor by) => CurrentTask() != null;

        public void Interact(PlayerInteractor by)
        {
            if (CurrentTask() == null) return;
            GameSceneContext.Instance.Tasks.ReportTalk(npcId);
            var patrol = GetComponentInParent<NpcPatrol>();
            if (patrol != null) patrol.Pause(talkPauseSeconds);
        }

        private TaskInstance CurrentTask()
        {
            var tasks = GameSceneContext.Instance != null ? GameSceneContext.Instance.Tasks : null;
            var task = tasks != null ? tasks.Current : null;
            if (task == null || task.State != TaskState.Active) return null;
            return task.Definition.type == TaskType.TalkToNpc && task.Definition.targetSpotId == npcId ? task : null;
        }
    }
}
