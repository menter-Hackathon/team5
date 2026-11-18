using Team5.Core;
using Team5.Data;
using Team5.Player;
using Team5.UI;
using UnityEngine;

namespace Team5.Items
{
    // マップに置いた素材。E で拾うと持ち物に入る
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int count = 1;
        [SerializeField] private bool suspicious = true; // 見られているときに拾うと「らしくない行動」

        public string Prompt => $"拾う：{item.displayName}";
        public bool IsSuspicious => suspicious;

        public bool CanInteract(PlayerInteractor by) => item != null;

        public void Interact(PlayerInteractor by)
        {
            var context = GameSceneContext.Instance;
            if (context != null && context.Inventory != null) context.Inventory.Add(item, count);
            else Debug.LogWarning($"GameSceneContext か Inventory がないので {item.displayName} を持ち物に入れられません。", this);
            if (WorldFx.Instance != null) WorldFx.Instance.Pickup(transform.position, item);
            Destroy(gameObject);
        }
    }
}
