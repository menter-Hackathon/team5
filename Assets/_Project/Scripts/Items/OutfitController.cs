using System;
using System.Collections.Generic;
using Team5.Core;
using Team5.Data;
using Team5.Player;
using UnityEngine;

namespace Team5.Items
{
    // プレイヤーに付ける。手に入れた服を着て見た目を変える（CD-12・outfits.md）
    public class OutfitController : MonoBehaviour
    {
        [SerializeField] private Transform attachBody;
        [SerializeField] private Transform attachHead;
        [SerializeField] private Transform attachHand;

        private readonly Dictionary<string, (OutfitDefinition outfit, GameObject model)> worn = new Dictionary<string, (OutfitDefinition, GameObject)>();
        private readonly HashSet<OutfitDefinition> everWorn = new HashSet<OutfitDefinition>();
        private PlayerAppearance appearance;

        // 着ている服の stage の最大値（0：村人の制服だけ）
        public int CurrentStage { get; private set; }
        public event Action<OutfitDefinition> OnEquipped;
        public event Action<OutfitDefinition> OnEquipCaught; // 着替えようとして見つかった

        private void Awake() => appearance = GetComponent<PlayerAppearance>();

        public bool IsWearing(OutfitDefinition outfit) =>
            outfit != null && worn.TryGetValue(outfit.attachPoint, out var entry) && entry.outfit == outfit;

        public bool Owns(OutfitDefinition outfit)
        {
            var inventory = GameSceneContext.Instance != null ? GameSceneContext.Instance.Inventory : null;
            return outfit != null && outfit.item != null && inventory != null && inventory.Count(outfit.item) > 0;
        }

        // 段階の順番に着る：段階 2 は段階 1 を着てから（outfits.md 3章）
        public bool IsNextStage(OutfitDefinition outfit) => outfit != null && outfit.stage <= CurrentStage + 1;

        public bool CanEquip(OutfitDefinition outfit) => Owns(outfit) && !IsWearing(outfit) && IsNextStage(outfit);

        // 着替える。見られていたら着替えない（着替えは「らしくない行動」）。着替えたら true
        public bool Equip(OutfitDefinition outfit)
        {
            if (!CanEquip(outfit)) return false;

            var context = GameSceneContext.Instance;
            if (context != null && context.Surveillance != null && context.Surveillance.ReportAction(true))
            {
                OnEquipCaught?.Invoke(outfit);
                return false;
            }

            Unequip(outfit.attachPoint);
            GameObject model = null;
            if (outfit.prefab != null)
            {
                model = Instantiate(outfit.prefab, AttachPoint(outfit.attachPoint), false);
                model.name = outfit.prefab.name;
            }
            worn[outfit.attachPoint] = (outfit, model);
            UpdateStage();
            if (appearance != null) appearance.RefreshRenderers(); // 服も個性の濃さで色が変わる

            if (everWorn.Add(outfit) && context != null && context.Parameters != null)
                context.Parameters.Add(ParameterType.Individuality, outfit.individualityReward, $"着替え：{outfit.displayName}");
            OnEquipped?.Invoke(outfit);
            return true;
        }

        private void Unequip(string attachPoint)
        {
            if (!worn.TryGetValue(attachPoint, out var entry)) return;
            if (entry.model != null) Destroy(entry.model);
            worn.Remove(attachPoint);
        }

        private void UpdateStage()
        {
            int stage = 0;
            foreach (var entry in worn.Values) stage = Mathf.Max(stage, entry.outfit.stage);
            CurrentStage = stage;
        }

        private Transform AttachPoint(string name)
        {
            switch (name)
            {
                case "Head": return attachHead != null ? attachHead : transform;
                case "Hand": return attachHand != null ? attachHand : transform;
                default: return attachBody != null ? attachBody : transform;
            }
        }
    }
}
