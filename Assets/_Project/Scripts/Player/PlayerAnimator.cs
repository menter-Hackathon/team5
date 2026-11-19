using Team5.Core;
using Team5.Items;
using Team5.Tasks;
using UnityEngine;

namespace Team5.Player
{
    // プレイヤーの動き（AN-01）。歩きはモデルのモーション、それ以外の動作はモデル全体の姿勢を動かして見せる。
    // Animator に下のパラメーターがあれば値も入れる（Speed / Pickup / Acting / Crafting / Caught）。モーションを足したらそのまま使える
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;          // 空なら子から探す
        [SerializeField] private float moveThreshold = 0.1f; // これより速く動いていたら歩きモーション
        [SerializeField] private float walkSpeedForAnim = 4f; // この速さで Speed = 1

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int PickupId = Animator.StringToHash("Pickup");
        private static readonly int ActingId = Animator.StringToHash("Acting");
        private static readonly int CraftingId = Animator.StringToHash("Crafting");
        private static readonly int CaughtId = Animator.StringToHash("Caught");

        private enum OneShot { None, Pickup, Caught, Equip, Celebrate, Slump }

        private CharacterController controller;
        private GameSceneContext context;
        private TaskSpot[] spots;
        private Transform model;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Vector3 baseScale;

        private OneShot oneShot;
        private float oneShotTime;
        private float oneShotLength;
        private bool acting;
        private bool crafting;
        private bool ended; // ゲームが終わった（クリア・ゲームオーバーの姿勢を続ける）

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            model = animator != null ? animator.transform : null;
            if (model != null)
            {
                basePosition = model.localPosition;
                baseRotation = model.localRotation;
                baseScale = model.localScale;
            }
        }

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context != null && context.Surveillance != null) context.Surveillance.OnCaught += HandleCaught;
            spots = FindObjectsByType<TaskSpot>(FindObjectsSortMode.None);
            foreach (var spot in spots) spot.OnActing += HandleActing;
            if (TryGetComponent(out OutfitController outfits)) outfits.OnEquipped += _ => Play(OneShot.Equip, 0.6f);
        }

        private void OnDestroy()
        {
            if (context != null && context.Surveillance != null) context.Surveillance.OnCaught -= HandleCaught;
            if (spots != null) foreach (var spot in spots) if (spot != null) spot.OnActing -= HandleActing;
        }

        // ItemPickup から呼ぶ
        public void PlayPickup()
        {
            SetTrigger(PickupId);
            Play(OneShot.Pickup, 0.45f);
        }

        // クリアしたとき（PlayerVfx から呼ぶ）
        public void PlayCelebrate()
        {
            ended = true;
            Play(OneShot.Celebrate, 1.8f);
        }

        // ゲームオーバーのとき（PlayerVfx から呼ぶ）
        public void PlaySlump()
        {
            ended = true;
            Play(OneShot.Slump, 1.2f);
        }

        private void HandleCaught(Team5.NPC.VisionSensor watcher)
        {
            SetTrigger(CaughtId);
            Play(OneShot.Caught, 0.6f);
        }

        private void HandleActing(bool value)
        {
            acting = value;
            SetBool(ActingId, value);
        }

        private void Play(OneShot kind, float seconds)
        {
            if (ended && kind != OneShot.Celebrate && kind != OneShot.Slump) return;
            oneShot = kind;
            oneShotTime = 0f;
            oneShotLength = seconds;
        }

        private void Update()
        {
            if (animator == null) return;

            float speed = 0f;
            if (controller != null)
            {
                Vector3 velocity = controller.velocity;
                velocity.y = 0f;
                speed = velocity.magnitude;
            }
            bool moving = speed > moveThreshold && !ended;
            animator.speed = moving ? Mathf.Clamp(speed / walkSpeedForAnim, 0.6f, 1.4f) : 0f;
            SetFloat(SpeedId, Mathf.Clamp01(speed / walkSpeedForAnim));

            bool nowCrafting = context != null && context.Crafting != null && context.Crafting.IsCrafting;
            if (nowCrafting != crafting)
            {
                crafting = nowCrafting;
                SetBool(CraftingId, crafting);
            }
        }

        // モーションの後で、モデル全体の姿勢を重ねる
        private void LateUpdate()
        {
            if (model == null) return;

            float t = Time.time;
            Vector3 offset = Vector3.zero;
            Vector3 euler = Vector3.zero; // x：前に傾く　y：ひねる　z：左右に傾く
            Vector3 scale = Vector3.one;

            bool still = animator != null && animator.speed == 0f;
            if (still && !ended) scale.y += 0.015f * Mathf.Sin(t * 2.2f); // 待機：呼吸

            if (acting && !ended)
            {
                // お題の動作：素振りのように前後に大きく振る
                float swing = Mathf.Sin(t * 9f);
                euler.x += 14f * swing;
                offset.y += 0.04f * Mathf.Abs(swing);
            }
            else if (crafting && !ended)
            {
                // 料理・作業：前かがみで鍋をかき混ぜるように揺れる
                euler.x += 12f;
                euler.y += 10f * Mathf.Sin(t * 6f);
                euler.z += 4f * Mathf.Sin(t * 6f + 1f);
            }

            if (oneShot != OneShot.None)
            {
                oneShotTime += Time.deltaTime;
                float k = Mathf.Clamp01(oneShotTime / oneShotLength);
                float arc = Mathf.Sin(k * Mathf.PI);
                switch (oneShot)
                {
                    case OneShot.Pickup: // しゃがんで拾う
                        euler.x += 30f * arc;
                        scale.y -= 0.16f * arc;
                        break;
                    case OneShot.Caught: // びくっと跳ねて震える
                        offset.y += 0.35f * Mathf.Sin(Mathf.Min(1f, k * 2f) * Mathf.PI);
                        scale.y += 0.12f * arc;
                        euler.y += 12f * Mathf.Sin(k * 50f) * (1f - k);
                        break;
                    case OneShot.Equip: // くるっと回って見せる
                        euler.y += 360f * Mathf.SmoothStep(0f, 1f, k);
                        offset.y += 0.12f * arc;
                        break;
                    case OneShot.Celebrate: // 2 回跳ねて、最後に 1 回転
                        offset.y += 0.45f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f));
                        euler.y += 360f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.5f) * 2f));
                        scale.y += 0.06f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f));
                        break;
                    case OneShot.Slump: // がっくりうなだれて、しぼむ
                        euler.x += 35f * Mathf.SmoothStep(0f, 1f, k);
                        scale *= Mathf.Lerp(1f, 0.85f, k);
                        break;
                }
                // 終わりの姿勢を残すもの以外は元に戻す
                if (k >= 1f && oneShot != OneShot.Slump) oneShot = OneShot.None;
            }

            model.localPosition = basePosition + offset;
            model.localRotation = baseRotation * Quaternion.Euler(euler);
            model.localScale = Vector3.Scale(baseScale, scale);
        }

        // ---------- Animator のパラメーター（あるときだけ入れる） ----------

        private bool Has(int id, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            foreach (var p in animator.parameters) if (p.nameHash == id && p.type == type) return true;
            return false;
        }

        private void SetFloat(int id, float value) { if (Has(id, AnimatorControllerParameterType.Float)) animator.SetFloat(id, value); }
        private void SetBool(int id, bool value) { if (Has(id, AnimatorControllerParameterType.Bool)) animator.SetBool(id, value); }
        private void SetTrigger(int id) { if (Has(id, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(id); }
    }
}
