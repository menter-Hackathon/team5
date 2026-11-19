using Team5.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 持ち物一覧の1行。アイコンがなければ名前の文字を出す
    public class InventoryRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text countLabel;

        public void Show(ItemDefinition item, int count)
        {
            icon.sprite = item.icon;
            icon.gameObject.SetActive(item.icon != null);
            nameLabel.text = item.displayName;
            countLabel.text = $"×{count}";
        }
    }
}
