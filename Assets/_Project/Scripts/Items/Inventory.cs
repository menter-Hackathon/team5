using System;
using System.Collections.Generic;
using Team5.Data;
using UnityEngine;

#pragma warning disable CS0067 // 雛形のため、まだ呼ばれないイベントの警告を止める（実装したら消す）

namespace Team5.Items
{
    public class Inventory : MonoBehaviour
    {
        public int Count(ItemDefinition item)
        {
            Debug.Log("TODO: Inventory.Count");
            return default;
        }

        public bool Has(IEnumerable<ItemAmount> items)
        {
            Debug.Log("TODO: Inventory.Has");
            return default;
        }

        public void Add(ItemDefinition item, int count = 1)
        {
            Debug.Log("TODO: Inventory.Add");
        }

        public bool Remove(ItemDefinition item, int count = 1)
        {
            Debug.Log("TODO: Inventory.Remove");
            return default;
        }

        public IReadOnlyDictionary<ItemDefinition, int> All => default;
        public event Action OnChanged;
    }
}
