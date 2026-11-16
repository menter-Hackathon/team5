using System;
using Team5.Data;
using UnityEngine;

#pragma warning disable CS0067 // 雛形のため、まだ呼ばれないイベントの警告を止める（実装したら消す）

namespace Team5.Tasks
{
    public class TaskManager : MonoBehaviour
    {
        public TaskInstance Current { get; private set; }
        public int CompletedCount { get; private set; }
        public event Action<TaskInstance> OnTaskAssigned;
        public event Action<TaskInstance> OnTaskCompleted;
        public event Action<TaskInstance> OnTaskFailed;

        public void Begin(RoleDefinition role)
        {
            Debug.Log("TODO: TaskManager.Begin");
        }

        // ActionAtSpot の進行
        public void ReportSpotAction(string spotId, float seconds)
        {
            Debug.Log("TODO: TaskManager.ReportSpotAction");
        }

        // BringItem
        public bool TryDeliver(ItemDefinition item)
        {
            Debug.Log("TODO: TaskManager.TryDeliver");
            return default;
        }

        // TalkToNpc
        public void ReportTalk(string npcId)
        {
            Debug.Log("TODO: TaskManager.ReportTalk");
        }
    }
}
