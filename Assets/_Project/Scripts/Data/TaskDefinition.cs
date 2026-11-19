using UnityEngine;

namespace Team5.Data
{
    [CreateAssetMenu(menuName = "Team5/Task")]
    public class TaskDefinition : ScriptableObject
    {
        public string taskId;              // "swing"
        public string title;               // "素振りをする"
        [TextArea] public string request;  // NPCのセリフ「勇者らしく素振りをしなさい」
        public TaskType type;
        public string targetSpotId;        // ActionAtSpot / TalkToNpc の対象ID
        public ItemDefinition requiredItem; // BringItem のとき
        public int requiredCount = 1;
        public float actionSeconds = 3f;   // ActionAtSpot でかかる時間
        public float timeLimitSeconds = 30f; // この時間内にやらないと失敗（0なら無制限）
        public float peaceReward = 10f;          // 完了で平和度 +
        public float individualityCost = 10f;    // 完了で個性 -
        public float failPeacePenalty = 15f;     // 失敗で平和度 -
        public ItemDefinition rewardItem;        // こなすともらえる素材（なければ空）。言いなりにも得があるように
        public int rewardCount = 1;
    }
}
