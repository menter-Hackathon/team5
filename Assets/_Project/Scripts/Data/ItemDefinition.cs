using UnityEngine;

namespace Team5.Data
{
    [CreateAssetMenu(menuName = "Team5/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string itemId;          // "chicken_bone"
        public string displayName;     // "鶏ガラ"
        [TextArea] public string description;
        public ItemCategory category;
        public Sprite icon;
        public GameObject worldPrefab; // マップに置くときの見た目
        public bool suspiciousToCarry; // true: 持っているところを見られるとまずい（今は未使用）
    }
}
