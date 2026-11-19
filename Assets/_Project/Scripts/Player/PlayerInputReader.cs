using System;
using Team5.Core;
using Team5.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Team5.Player
{
    // PlayerControls の入力を読み、ほかのスクリプトに渡す
    public class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public bool InteractHeld => !IsInputBlocked() && controls.Player.Interact.IsPressed(); // 長押しの判定用（CD-09b）
        public event Action OnInteract;
        public event Action OnInventory;
        public event Action OnOutfit; // C（ゲームパッド West）：持ち物の着替えタブ
        public event Action OnPause;

        // 説明のカードを出している間など、ゲームの操作を一時的に止める（TutorialView が使う）
        public static bool GuideBlocking { get; set; }

        private PlayerControls controls;

        private void Awake()
        {
            controls = new PlayerControls();
            controls.Player.Interact.performed += HandleInteract;
            controls.Player.Inventory.performed += HandleInventory;
            controls.Player.Outfit.performed += HandleOutfit;
            controls.Player.Pause.performed += HandlePause;
        }

        private void OnEnable() => controls.Player.Enable();

        private void OnDisable() => controls.Player.Disable();

        private void OnDestroy() => controls.Dispose();

        private void Update()
        {
            Move = IsInputBlocked() ? Vector2.zero : controls.Player.Move.ReadValue<Vector2>();
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            if (!IsInputBlocked()) OnInteract?.Invoke();
        }

        private void HandleInventory(InputAction.CallbackContext context)
        {
            if (!IsInputBlocked()) OnInventory?.Invoke();
        }

        private void HandleOutfit(InputAction.CallbackContext context)
        {
            if (!IsInputBlocked()) OnOutfit?.Invoke();
        }

        // 一時停止中でも解除できるように、Pause は Result のときだけ止める
        private void HandlePause(InputAction.CallbackContext context)
        {
            if (CurrentState() != GameState.Result && !GuideBlocking) OnPause?.Invoke();
        }

        private static bool IsInputBlocked()
        {
            if (GuideBlocking) return true;
            var state = CurrentState();
            return state == GameState.Paused || state == GameState.Result;
        }

        // Game シーンを直接再生したとき（GameManager がいないとき）は Playing 扱い
        private static GameState CurrentState()
        {
            return GameManager.Instance != null ? GameManager.Instance.State : GameState.Playing;
        }
    }
}
