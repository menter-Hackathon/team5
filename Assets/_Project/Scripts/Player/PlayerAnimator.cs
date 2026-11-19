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
        [SerializeField] private float idleBlendSeconds = 0.25f; // 止まってから待機の姿勢になるまで

        // 待機の姿勢（Humanoid の筋肉の値）。走りの 1 コマ目のままだと足を開いたまま固まるので、まっすぐ立たせて息づかいを足す
        private static readonly (string muscle, float value)[] IdlePose =
        {
            ("Left Arm Down-Up", -0.62f), ("Right Arm Down-Up", -0.62f),
            ("Left Arm Front-Back", 0.18f), ("Right Arm Front-Back", 0.18f),
            ("Left Forearm Stretch", 0.75f), ("Right Forearm Stretch", 0.75f),
            ("Left Upper Leg Front-Back", 0.02f), ("Right Upper Leg Front-Back", 0.02f),
            ("Left Upper Leg In-Out", 0.05f), ("Right Upper Leg In-Out", 0.05f),
            ("Left Lower Leg Stretch", 0.98f), ("Right Lower Leg Stretch", 0.98f),
        };

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

        private HumanPoseHandler poseHandler;
        private HumanPose pose;
        private float[] idleMuscles;
        private float idleWeight;
        private int spineFB, chestFB, headTurn, headNod, leftArmFB, rightArmFB, leftArmDU, rightArmDU;

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
            SetupIdlePose();
        }

        private void SetupIdlePose()
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return;
            poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
            idleMuscles = new float[HumanTrait.MuscleCount];
            foreach (var (muscle, value) in IdlePose)
            {
                int i = MuscleIndex(muscle);
                if (i >= 0) idleMuscles[i] = value;
            }
            spineFB = MuscleIndex("Spine Front-Back");
            chestFB = MuscleIndex("Chest Front-Back");
            headTurn = MuscleIndex("Head Turn Left-Right");
            headNod = MuscleIndex("Head Nod Down-Up");
            leftArmFB = MuscleIndex("Left Arm Front-Back");
            rightArmFB = MuscleIndex("Right Arm Front-Back");
            leftArmDU = MuscleIndex("Left Arm Down-Up");
            rightArmDU = MuscleIndex("Right Arm Down-Up");
        }

        private static int MuscleIndex(string name)
        {
            for (int i = 0; i < HumanTrait.MuscleCount; i++) if (HumanTrait.MuscleName[i] == name) return i;
            return -1;
        }

        // 待機のモーション：まっすぐ立って、息をする・ときどき左右を見る・腕が少し揺れる
        private void ApplyIdlePose()
        {
            if (poseHandler == null || idleWeight <= 0.001f) return;
            poseHandler.GetHumanPose(ref pose);
            float t = Time.time;
            float breath = Mathf.Sin(t * 2.2f);
            // ゆっくり左右を見る（止まる時間を長めに）
            float look = Mathf.Sin(t * 0.45f);
            look = Mathf.Sign(look) * Mathf.SmoothStep(0f, 1f, Mathf.Abs(look));
            for (int i = 0; i < pose.muscles.Length && i < idleMuscles.Length; i++)
            {
                float target = idleMuscles[i];
                if (i == spineFB) target += 0.04f * breath;
                else if (i == chestFB) target += 0.05f * breath;
                else if (i == headTurn) target += 0.28f * look;
                else if (i == headNod) target += 0.04f * Mathf.Sin(t * 2.2f + 0.6f);
                else if (i == leftArmFB) target += 0.04f * Mathf.Sin(t * 1.1f);
                else if (i == rightArmFB) target += 0.04f * Mathf.Sin(t * 1.1f + 1.4f);
                else if (i == leftArmDU || i == rightArmDU) target += 0.025f * breath;
                pose.muscles[i] = Mathf.Lerp(pose.muscles[i], target, idleWeight);
            }
            poseHandler.SetHumanPose(ref pose);
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
            poseHandler?.Dispose();
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
            if (!moving && animator.speed > 0f)
            {
                // 止まったら 1 コマ目の姿に戻す（走りの途中で固まらないように）
                animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
                animator.speed = 0f;
                animator.Update(0f);
            }
            else if (moving) animator.speed = Mathf.Clamp(speed / walkSpeedForAnim, 0.6f, 1.4f);
            float idleTarget = moving || ended ? 0f : 1f;
            idleWeight = Mathf.MoveTowards(idleWeight, idleTarget, Time.deltaTime / Mathf.Max(0.01f, idleBlendSeconds));
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
            ApplyIdlePose();

            float t = Time.time;
            Vector3 offset = Vector3.zero;
            Vector3 euler = Vector3.zero; // x：前に傾く　y：ひねる　z：左右に傾く
            Vector3 scale = Vector3.one;

            bool still = animator != null && animator.speed == 0f;
            if (still && !ended && poseHandler == null) scale.y += 0.015f * Mathf.Sin(t * 2.2f); // 待機：呼吸（Humanoid でないとき）

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
