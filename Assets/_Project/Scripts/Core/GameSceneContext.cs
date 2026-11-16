using Team5.Data;
using Team5.Items;
using Team5.NPC;
using Team5.Tasks;
using UnityEngine;

namespace Team5.Core
{
    // Game シーンに1つ置く。参照の窓口。
    public class GameSceneContext : MonoBehaviour
    {
        public static GameSceneContext Instance { get; private set; }
        public GameBalance Balance;            // インスペクターで設定
        public Transform Player;
        public PlayerParameters Parameters;
        public GameTimer Timer;
        public SurveillanceManager Surveillance;
        public TaskManager Tasks;
        public Inventory Inventory;
        public CraftingSystem Crafting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("GameSceneContext が2つあります。後から来たほうを無視します。", this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
