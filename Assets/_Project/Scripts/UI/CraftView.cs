using System.Collections.Generic;
using System.Linq;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Team5.UI
{
    // 作れるレシピの一覧（材料が足りないものは押せない）と、作っている間の進み具合（仮UI）
    public class CraftView : MonoBehaviour
    {
        [SerializeField] private GameObject listPanel;
        [SerializeField] private Transform listRoot;
        [SerializeField] private Button recipeButtonTemplate; // 非アクティブにしておく見本
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject progressPanel;
        [SerializeField] private Text progressLabel;
        [SerializeField] private RectTransform progressFill;   // anchorMax.x を 0〜1 にする
        [SerializeField] private float messageSeconds = 2f;    // 結果の文字を出しておく秒数

        private readonly List<Button> buttons = new List<Button>();
        private CraftingSystem crafting;
        private PlayerInputReader input;
        private PlayerMover mover;
        private float messageRemaining;
        private int openedFrame = -1;

        public bool IsOpen => listPanel.activeSelf;

        private void Start()
        {
            listPanel.SetActive(false);
            progressPanel.SetActive(false);
            closeButton.onClick.AddListener(Close);

            var context = GameSceneContext.Instance;
            if (context == null) return;
            crafting = context.Crafting;
            crafting.OnCraftProgress += ShowProgress;
            crafting.OnCrafted += HandleCrafted;
            crafting.OnCraftInterrupted += HandleInterrupted;
            if (context.Player != null)
            {
                input = context.Player.GetComponent<PlayerInputReader>();
                mover = context.Player.GetComponent<PlayerMover>();
                input.OnInteract += HandleInteract;
            }
        }

        private void OnDestroy()
        {
            if (crafting != null)
            {
                crafting.OnCraftProgress -= ShowProgress;
                crafting.OnCrafted -= HandleCrafted;
                crafting.OnCraftInterrupted -= HandleInterrupted;
            }
            if (input != null) input.OnInteract -= HandleInteract;
        }

        public void Open(IReadOnlyList<RecipeDefinition> recipes)
        {
            if (crafting == null || crafting.IsCrafting) return;

            for (int i = 0; i < recipes.Count; i++)
            {
                if (i == buttons.Count) buttons.Add(Instantiate(recipeButtonTemplate, listRoot));
                var button = buttons[i];
                var recipe = recipes[i];
                button.GetComponentInChildren<Text>().text = Describe(recipe);
                button.interactable = crafting.CanCraft(recipe);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Craft(recipe));
                button.gameObject.SetActive(true);
            }
            for (int i = recipes.Count; i < buttons.Count; i++) buttons[i].gameObject.SetActive(false);
            closeButton.transform.SetAsLastSibling();

            listPanel.SetActive(true);
            progressPanel.SetActive(false);
            openedFrame = Time.frameCount;
            if (mover != null) mover.CanMove = false;

            var first = buttons.Take(recipes.Count).FirstOrDefault(b => b.interactable);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : closeButton.gameObject);
        }

        public void Close()
        {
            if (!IsOpen) return;
            listPanel.SetActive(false);
            if (mover != null && !crafting.IsCrafting) mover.CanMove = true;
        }

        private void Craft(RecipeDefinition recipe)
        {
            crafting.StartCraft(recipe);
            if (crafting.IsCrafting) listPanel.SetActive(false); // CanMove は CraftingSystem が止めている
        }

        // 一覧を開いているときに E でもう一度押したら閉じる（開いたときの E は無視）
        private void HandleInteract()
        {
            if (IsOpen && Time.frameCount != openedFrame) Close();
        }

        private void Update()
        {
            if (messageRemaining > 0f)
            {
                messageRemaining -= Time.deltaTime;
                if (messageRemaining <= 0f) progressPanel.SetActive(false);
            }
            else if (progressPanel.activeSelf && crafting != null && !crafting.IsCrafting)
            {
                progressPanel.SetActive(false); // Cancel されたとき
            }
        }

        private void ShowProgress(RecipeDefinition recipe, float progress)
        {
            messageRemaining = 0f;
            progressPanel.SetActive(true);
            progressLabel.text = $"作っている：{recipe.displayName}";
            progressFill.anchorMax = new Vector2(progress, 1f);
        }

        private void HandleCrafted(RecipeDefinition recipe)
        {
            string name = recipe.output != null ? recipe.output.displayName : recipe.displayName;
            ShowMessage($"できた！：{name}", 1f);
        }

        private void HandleInterrupted(RecipeDefinition recipe) => ShowMessage("見つかった！ 作るのをやめた…", 0f);

        private void ShowMessage(string message, float progress)
        {
            progressPanel.SetActive(true);
            progressLabel.text = message;
            progressFill.anchorMax = new Vector2(progress, 1f);
            messageRemaining = messageSeconds;
        }

        private static string Describe(RecipeDefinition recipe)
        {
            string inputs = string.Join(" + ", recipe.inputs.Select(i => $"{(i.item != null ? i.item.displayName : "?")}×{i.count}"));
            return $"{recipe.displayName}　←　{inputs}（{recipe.craftSeconds:0.#}秒）";
        }
    }
}
