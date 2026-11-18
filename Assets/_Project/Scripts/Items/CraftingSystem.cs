using System;
using Team5.Core;
using Team5.Data;
using Team5.Player;
using UnityEngine;

namespace Team5.Items
{
    // 集めた素材からレシピに沿って物を作る。作っている間は見つかると中断する
    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField] private float reportInterval = 0.5f;       // 作っている間、何秒ごとに監視へ報告するか
        [SerializeField] private float cancelInputThreshold = 0.5f; // 移動入力がこれを超えたら中止

        public bool IsCrafting { get; private set; }

        private RecipeDefinition recipe;
        private float elapsed;
        private float reportRemaining;
        private PlayerMover mover;
        private PlayerInputReader input;

        private void Start()
        {
            var context = GameSceneContext.Instance;
            if (context == null || context.Player == null) return;
            mover = context.Player.GetComponent<PlayerMover>();
            input = context.Player.GetComponent<PlayerInputReader>();
            if (input != null) input.OnPause += Cancel;
        }

        private void OnDestroy()
        {
            if (input != null) input.OnPause -= Cancel;
        }

        public bool CanCraft(RecipeDefinition recipe)
        {
            if (recipe == null || IsCrafting) return false;
            var inventory = GameSceneContext.Instance != null ? GameSceneContext.Instance.Inventory : null;
            return inventory != null && inventory.Has(recipe.inputs);
        }

        // craftSeconds かけて作る
        public void StartCraft(RecipeDefinition recipe)
        {
            if (!CanCraft(recipe)) return;

            this.recipe = recipe;
            elapsed = 0f;
            reportRemaining = 0f; // 始めた瞬間にも報告する
            IsCrafting = true;
            if (mover != null) mover.CanMove = false;
            OnCraftProgress?.Invoke(recipe, 0f);
        }

        // 途中でやめる（材料は減らさない）。Esc か移動キーで呼ばれる
        public void Cancel()
        {
            if (!IsCrafting) return;
            End();
        }

        public event Action<RecipeDefinition, float> OnCraftProgress; // (レシピ, 0〜1)
        public event Action<RecipeDefinition> OnCrafted;
        public event Action<RecipeDefinition> OnCraftInterrupted;     // 見つかって中断

        private void Update()
        {
            if (!IsCrafting) return;

            if (input != null && input.Move.magnitude > cancelInputThreshold)
            {
                Cancel();
                return;
            }

            reportRemaining -= Time.deltaTime;
            if (reportRemaining <= 0f)
            {
                reportRemaining += reportInterval;
                var surveillance = GameSceneContext.Instance != null ? GameSceneContext.Instance.Surveillance : null;
                if (surveillance != null && surveillance.ReportAction(true))
                {
                    var interrupted = recipe;
                    End();
                    OnCraftInterrupted?.Invoke(interrupted);
                    return;
                }
            }

            elapsed += Time.deltaTime;
            float progress = recipe.craftSeconds > 0f ? Mathf.Clamp01(elapsed / recipe.craftSeconds) : 1f;
            OnCraftProgress?.Invoke(recipe, progress);
            if (progress >= 1f) Finish();
        }

        private void Finish()
        {
            var crafted = recipe;
            var context = GameSceneContext.Instance;
            var inventory = context.Inventory;
            foreach (var amount in crafted.inputs) inventory.Remove(amount.item, amount.count);
            if (!crafted.consumeOnCraft && crafted.output != null) inventory.Add(crafted.output, crafted.outputCount);
            if (context.Parameters != null)
            {
                context.Parameters.Add(ParameterType.RoleProgress, crafted.roleProgressReward, $"クラフト：{crafted.displayName}");
                context.Parameters.Add(ParameterType.Individuality, crafted.individualityReward, $"クラフト：{crafted.displayName}");
            }
            End();
            OnCrafted?.Invoke(crafted);
        }

        private void End()
        {
            IsCrafting = false;
            recipe = null;
            if (mover != null) mover.CanMove = true;
        }
    }
}
