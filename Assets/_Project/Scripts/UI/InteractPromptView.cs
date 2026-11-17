using Team5.Core;
using Team5.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 画面下に「[E] 拾う：鶏ガラ」と出す仮UI
    public class InteractPromptView : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor; // 空なら Player から探す
        [SerializeField] private GameObject panel;
        [SerializeField] private Text label;

        private void Start()
        {
            if (interactor == null) interactor = FindPlayerInteractor();
            if (interactor == null)
            {
                Debug.LogWarning("PlayerInteractor が見つかりません。", this);
                return;
            }
            interactor.OnFocusChanged += Show;
            Show(interactor.Current);
        }

        private void OnDestroy()
        {
            if (interactor != null) interactor.OnFocusChanged -= Show;
        }

        private void Show(IInteractable target)
        {
            bool visible = target != null && !string.IsNullOrEmpty(target.Prompt);
            panel.SetActive(visible);
            if (visible) label.text = $"[E] {target.Prompt}";
        }

        private static PlayerInteractor FindPlayerInteractor()
        {
            var context = GameSceneContext.Instance;
            Transform player = context != null && context.Player != null ? context.Player : GameObject.FindWithTag("Player")?.transform;
            return player != null ? player.GetComponent<PlayerInteractor>() : null;
        }
    }
}
