using System.Collections.Generic;
using System.Linq;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.Player;
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
        [SerializeField] private float moveThreshold = 0.5f;  // A/D（左スティック）で選ぶ

        private OutfitController controller;
        private List<OutfitDefinition> outfits = new List<OutfitDefinition>();
        private string defaultNote;
        private float messageRemaining;
        private PlayerInputReader input;
        private int selected;
        private int heldDirection;

        private void Awake()
        {
            defaultNote = noteLabel.text;
            foreach (var card in cards) card.OnWearClicked += c => { Select(System.Array.IndexOf(cards, c)); Wear(c.Outfit); };
        }

        private void OnEnable()
        {
            var context = GameSceneContext.Instance;
            if (context == null) return;
            if (controller == null && context.Player != null)
            {
                controller = context.Player.GetComponent<OutfitController>();
                if (controller != null) controller.OnEquipCaught += HandleCaught;
                input = context.Player.GetComponent<PlayerInputReader>();
            }
            if (input != null) input.OnInteract += WearSelected; // E で着る
            var role = context.Role;
            outfits = role != null && role.outfits != null ? role.outfits.Where(o => o != null).OrderBy(o => o.stage).ToList() : new List<OutfitDefinition>();
            if (role != null)
            {
                roleName.text = role.displayName;
                roleName.color = role.roleColor;
            }
            heldDirection = 0;
            Refresh();
            // 着られる服があればそれを、なければ左端を選ぶ
            int first = Enumerable.Range(0, outfits.Count).FirstOrDefault(i => cards[i].Button.interactable);
            Select(first);
        }

        private void OnDisable()
        {
            if (input != null) input.OnInteract -= WearSelected;
        }

        private void OnDestroy()
        {
            if (controller != null) controller.OnEquipCaught -= HandleCaught;
        }

        private void Update()
        {
            if (input != null)
            {
                float x = input.Move.x;
                int direction = Mathf.Abs(x) > moveThreshold ? (x > 0 ? 1 : -1) : 0;
                if (direction != 0 && direction != heldDirection) Select(Mathf.Clamp(selected + direction, 0, Mathf.Max(outfits.Count - 1, 0)));
                heldDirection = direction;
            }

            if (messageRemaining <= 0f) return;
            messageRemaining -= Time.deltaTime;
            if (messageRemaining <= 0f) noteLabel.text = defaultNote;
        }

        private void Select(int index)
        {
            if (index < 0 || index >= outfits.Count) return;
            selected = index;
            for (int i = 0; i < cards.Length; i++) cards[i].SetSelected(i == selected);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); // 選択は自前の枠で出す
        }

        private void WearSelected()
        {
            if (selected < outfits.Count) Wear(outfits[selected]);
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
