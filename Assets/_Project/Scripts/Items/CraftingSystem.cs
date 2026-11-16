using System;
using Team5.Data;
using UnityEngine;

#pragma warning disable CS0067 // 雛形のため、まだ呼ばれないイベントの警告を止める（実装したら消す）

namespace Team5.Items
{
    public class CraftingSystem : MonoBehaviour
    {
        public bool IsCrafting { get; private set; }

        public bool CanCraft(RecipeDefinition recipe)
        {
            Debug.Log("TODO: CraftingSystem.CanCraft");
            return default;
        }

        // craftSeconds かけて作る
        public void StartCraft(RecipeDefinition recipe)
        {
            Debug.Log("TODO: CraftingSystem.StartCraft");
        }

        public void Cancel()
        {
            Debug.Log("TODO: CraftingSystem.Cancel");
        }

        public event Action<RecipeDefinition, float> OnCraftProgress; // (レシピ, 0〜1)
        public event Action<RecipeDefinition> OnCrafted;
        public event Action<RecipeDefinition> OnCraftInterrupted;     // 見つかって中断
    }
}
