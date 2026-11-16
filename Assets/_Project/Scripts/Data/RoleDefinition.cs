using System.Collections.Generic;
using UnityEngine;

namespace Team5.Data
{
    [CreateAssetMenu(menuName = "Team5/Role")]
    public class RoleDefinition : ScriptableObject
    {
        public string roleId;                  // "ramen_chef"
        public string displayName;             // "鶏白湯職人"（なりたいロール）
        public string imposedRoleName;         // "勇者"（押し付けられるらしさ）
        [TextArea] public string imposedIntro; // 冒頭で村長が言うセリフ
        public Color roleColor = Color.white;  // 個性が濃くなったときの色
        public List<TaskDefinition> imposedTasks;   // 表のお題
        public List<RecipeDefinition> recipes;      // 裏で作れるもの
        public ItemDefinition goalItem;             // 完成させるとクリアに近づく推しアイテム
        public List<OutfitDefinition> outfits;      // 段階ごとの衣装
    }
}
