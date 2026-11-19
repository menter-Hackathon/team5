using Team5.Data;
using Team5.Items;
using Team5.NPC;
using Team5.Player;
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
        public RoleDefinition Role { get; private set; } // 今回のロール（Start で決まる）

        [SerializeField] private RoleDefinition fallbackRole; // Game シーンを直接再生したとき（GameManager がいないとき）のロール

        private PlayerInputReader input;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("GameSceneContext が2つあります。後から来たほうを無視します。", this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (Instance != this) return;

            var manager = GameManager.Instance;
            Role = manager != null && manager.CurrentRole != null ? manager.CurrentRole : fallbackRole;

            if (Player != null && Player.TryGetComponent(out input)) input.OnPause += TogglePause;

            Timer.StartTimer(Balance.timeLimitSeconds);
            Tasks.Begin(Role);
        }

        private void OnDestroy()
        {
            if (input != null) input.OnPause -= TogglePause;
            if (Instance == this) Instance = null;
        }

        private void TogglePause()
        {
            var manager = GameManager.Instance;
            if (manager == null) return;
            manager.SetPaused(manager.State != GameState.Paused);
        }
    }
}
