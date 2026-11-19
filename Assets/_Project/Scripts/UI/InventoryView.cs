using System.Collections.Generic;
using Team5.Core;
using Team5.Items;
using Team5.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // Tab で開閉する持ち物の一覧（仮UI）。開いている間もゲームは止めない
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform listRoot;
        [SerializeField] private InventoryRowView rowTemplate; // 非アクティブにしておく見本の行
        [SerializeField] private GameObject emptyLabel;        // 何も持っていないとき

        [Header("タブ（持ち物 / 着替え）")]
        [SerializeField] private GameObject outfitsPanel;      // 着替えタブの窓（OutfitView）
        [SerializeField] private Button[] itemsTabs;           // 各窓の「持ち物」タブ
        [SerializeField] private Button[] outfitsTabs;         // 各窓の「着替え」タブ

        private readonly List<InventoryRowView> rows = new List<InventoryRowView>();
        private Inventory inventory;
        private PlayerInputReader input;
        private PlayerMover mover;
        private PlayerInteractor interactor;

        public bool IsOpen => panel.activeSelf || (outfitsPanel != null && outfitsPanel.activeSelf);

        private void Start()
        {
            var context = GameSceneContext.Instance;
            if (context == null)
            {
                Debug.LogWarning("GameSceneContext がないので持ち物を表示できません。", this);
                return;
            }
            inventory = context.Inventory;
            if (context.Player != null)
            {
                input = context.Player.GetComponent<PlayerInputReader>();
                mover = context.Player.GetComponent<PlayerMover>();
                interactor = context.Player.GetComponent<PlayerInteractor>();
            }

            inventory.OnChanged += Refresh;
            if (input != null)
            {
                input.OnInventory += Toggle;
                input.OnOutfit += ToggleOutfits;
            }
            foreach (var tab in itemsTabs) tab.onClick.AddListener(() => ShowTab(false));
            foreach (var tab in outfitsTabs) tab.onClick.AddListener(() => ShowTab(true));
            panel.SetActive(false);
            if (outfitsPanel != null) outfitsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnChanged -= Refresh;
            if (input != null)
            {
                input.OnInventory -= Toggle;
                input.OnOutfit -= ToggleOutfits;
            }
        }

        // Tab：持ち物タブで開く／閉じる
        public void Toggle()
        {
            if (IsOpen) Close();
            else Open(false);
        }

        // C：着替えタブで開く。着替えタブなら閉じ、持ち物タブなら着替えタブに切り替える
        public void ToggleOutfits()
        {
            if (outfitsPanel == null) return;
            if (!IsOpen) Open(true);
            else if (outfitsPanel.activeSelf) Close();
            else ShowTab(true);
        }

        // 開いている間はプレイヤーを止め、拾う・話すをしない（ゲームの時間は止めない）
        public void Open(bool outfits)
        {
            if (IsOpen || (mover != null && !mover.CanMove)) return; // クラフト中・お題の長押し中などは開かない
            if (mover != null) mover.CanMove = false;
            if (interactor != null) interactor.enabled = false;
            ShowTab(outfits);
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (outfitsPanel != null) outfitsPanel.SetActive(false);
            if (mover != null) mover.CanMove = true;
            if (interactor != null) interactor.enabled = true;
        }

        private void ShowTab(bool outfits)
        {
            panel.SetActive(!outfits);
            if (outfitsPanel != null) outfitsPanel.SetActive(outfits);
            if (!outfits) Refresh();
        }

        private void Refresh()
        {
            if (!panel.activeSelf) return;

            int index = 0;
            foreach (var pair in inventory.All)
            {
                if (index == rows.Count) rows.Add(Instantiate(rowTemplate, listRoot));
                rows[index].Show(pair.Key, pair.Value);
                rows[index].gameObject.SetActive(true);
                index++;
            }
            for (int i = index; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            emptyLabel.SetActive(index == 0);
        }
    }
}
