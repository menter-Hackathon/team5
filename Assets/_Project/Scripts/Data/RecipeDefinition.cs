using System.Collections.Generic;
using UnityEngine;

namespace Team5.Data
{
    [CreateAssetMenu(menuName = "Team5/Recipe")]
    public class RecipeDefinition : ScriptableObject
    {
        public string recipeId;
        public string displayName;           // "鶏白湯スープ"
        public List<ItemAmount> inputs;
        public ItemDefinition output;
        public int outputCount = 1;
        public float craftSeconds = 3f;
        public float roleProgressReward = 20f;
        public float individualityReward = 15f;
        public bool consumeOnCraft = true;   // 料理など、作ったらその場で使うものは true
    }
}
