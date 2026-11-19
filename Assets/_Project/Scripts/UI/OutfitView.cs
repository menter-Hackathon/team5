using System.Collections.Generic;
using System.Linq;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Team5.UI
{
    // 持ち物の「着替え」タブ（ui-design.md 5章・ui_inventory_outfit.png）。持っていない服もシルエットで出す（outfits.md 6章）
    public class OutfitView : MonoBehaviour
    {
        [SerializeField] private OutfitCardView[] cards;      // 段階の順に左から（足りない分は非表示）
        [SerializeField] private Text roleName;               // なりたい姿：ロール名（ロールの色）
        [SerializeField] private Text stageLabel;             // 見た目 1 / 2
        [SerializeField] private Text noteLabel;              // 着替えは「らしくない行動」…／見つかった！
        [SerializeField] private float messageSeconds = 2f;

        private OutfitController controller;
        private List<OutfitDefinition> outfits = new List<OutfitDefinition>();
        private string defaultNote;
        private float messageRemaining;

        private void Awake()
        {
            defaultNote = noteLabel.text;
            foreach (var card in cards) card.OnWearClicked += c => Wear(c.Outfit);
        }

        private void OnEnable()
        {
            var context = GameSceneContext.Instance;
            if (context == null) return;
            if (controller == null && context.Player != null)
            {
                controller = context.Player.GetComponent<OutfitController>();
                if (controller != null) controller.OnEquipCaught += HandleCaught;
            }
            var role = context.Role;
            outfits = role != null && role.outfits != null ? role.outfits.Where(o => o != null).OrderBy(o => o.stage).ToList() : new List<OutfitDefinition>();
            if (role != null)
            {
                roleName.text = role.displayName;
                roleName.color = role.roleColor;
            }
            Refresh();
            var first = cards.Take(outfits.Count).FirstOrDefault(c => c.Button.interactable);
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.Button.gameObject);
        }

        private void OnDestroy()
        {
            if (controller != null) controller.OnEquipCaught -= HandleCaught;
        }

        private void Update()
        {
            if (messageRemaining <= 0f) return;
            messageRemaining -= Time.deltaTime;
            if (messageRemaining <= 0f) noteLabel.text = defaultNote;
        }

        private void Wear(OutfitDefinition outfit)
        {
            if (controller == null || outfit == null) return;
            if (controller.Equip(outfit)) ShowMessage($"{outfit.displayName}を着た！");
            Refresh();
        }

        private void HandleCaught(OutfitDefinition outfit) => ShowMessage("<color=#D63C42>見つかった！</color> 着替えられなかった");

        private void ShowMessage(string message)
        {
            noteLabel.text = message;
            messageRemaining = messageSeconds;
        }

        private void Refresh()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                bool visible = i < outfits.Count;
                cards[i].gameObject.SetActive(visible);
                if (!visible) continue;
                var outfit = outfits[i];
                string previous = outfits.Where(o => o.stage == outfit.stage - 1).Select(o => o.displayName).FirstOrDefault() ?? "前の服";
                cards[i].Show(outfit, StateOf(outfit), previous);
            }
            int maxStage = outfits.Count > 0 ? outfits.Max(o => o.stage) : 0;
            stageLabel.text = $"見た目 {(controller != null ? controller.CurrentStage : 0)} / {maxStage}";
        }

        private OutfitCardView.State StateOf(OutfitDefinition outfit)
        {
            if (controller == null || !controller.Owns(outfit)) return OutfitCardView.State.NotFound;
            if (controller.IsWearing(outfit)) return OutfitCardView.State.Wearing;
            return controller.IsNextStage(outfit) ? OutfitCardView.State.CanWear : OutfitCardView.State.Locked;
        }
    }
}
