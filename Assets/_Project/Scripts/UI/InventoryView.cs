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

        private readonly List<InventoryRowView> rows = new List<InventoryRowView>();
        private Inventory inventory;
        private PlayerInputReader input;

        public bool IsOpen => panel.activeSelf;

        private void Start()
        {
            var context = GameSceneContext.Instance;
            if (context == null)
            {
                Debug.LogWarning("GameSceneContext がないので持ち物を表示できません。", this);
                return;
            }
            inventory = context.Inventory;
            if (context.Player != null) input = context.Player.GetComponent<PlayerInputReader>();

            inventory.OnChanged += Refresh;
            if (input != null) input.OnInventory += Toggle;
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnChanged -= Refresh;
            if (input != null) input.OnInventory -= Toggle;
        }

        public void Toggle()
        {
            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf) Refresh();
        }

        private void Refresh()
        {
            if (!IsOpen) return;

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
