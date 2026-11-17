using System;
using System.Collections.Generic;
using Team5.Data;
using UnityEngine;

namespace Team5.Items
{
    // 持ち物。数に上限はない
    public class Inventory : MonoBehaviour
    {
        private readonly Dictionary<ItemDefinition, int> items = new Dictionary<ItemDefinition, int>();

        public int Count(ItemDefinition item)
        {
            return item != null && items.TryGetValue(item, out int count) ? count : 0;
        }

        public bool Has(IEnumerable<ItemAmount> items)
        {
            foreach (var amount in items)
            {
                if (Count(amount.item) < amount.count) return false;
            }
            return true;
        }

        public void Add(ItemDefinition item, int count = 1)
        {
            if (item == null || count <= 0) return;
            items[item] = Count(item) + count;
            OnChanged?.Invoke();
        }

        // 足りなければ何もせず false
        public bool Remove(ItemDefinition item, int count = 1)
        {
            if (item == null || count <= 0) return false;
            int current = Count(item);
            if (current < count) return false;

            if (current == count) items.Remove(item);
            else items[item] = current - count;
            OnChanged?.Invoke();
            return true;
        }

        public IReadOnlyDictionary<ItemDefinition, int> All => items;
        public event Action OnChanged;
    }
}
