using Team5.Core;
using Team5.Player;
using Team5.UI;
using UnityEngine;

namespace Team5.Items
{
    // 作業台。E でレシピの一覧（CraftView）を開く
    public class CraftStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private CraftView view;

        public string Prompt => "作る";
        public bool IsSuspicious => false; // 開くだけならよい。作っている間は CraftingSystem が 0.5 秒ごとに報告する（npc.md）

        public bool CanInteract(PlayerInteractor by)
        {
            var crafting = GameSceneContext.Instance != null ? GameSceneContext.Instance.Crafting : null;
            return view != null && !view.IsOpen && (crafting == null || !crafting.IsCrafting);
        }

        public void Interact(PlayerInteractor by)
        {
            var role = GameSceneContext.Instance != null ? GameSceneContext.Instance.Role : null;
            if (role == null)
            {
                Debug.LogWarning("ロールが決まっていないのでレシピを出せません。", this);
                return;
            }
            view.Open(role.recipes);
        }
    }
}
