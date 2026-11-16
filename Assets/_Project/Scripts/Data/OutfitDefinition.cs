using UnityEngine;

namespace Team5.Data
{
    [CreateAssetMenu(menuName = "Team5/Outfit")]
    public class OutfitDefinition : ScriptableObject
    {
        public string outfitId;
        public string displayName;
        public ItemDefinition item;          // 持ち物としての服
        public int stage;                    // 0:初期（制服/霧） 1:色がつく 2:ロールの衣装
        public GameObject prefab;            // プレイヤーに付けるモデル
        public string attachPoint = "Body";  // 付ける場所（Body / Head / Hand）
        public float individualityReward = 10f; // 初めて着たときの個性 +
    }
}
