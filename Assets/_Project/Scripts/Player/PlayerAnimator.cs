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

        private enum OneShot { None, Pickup, Caught, Equip, Celebrate, Slump, Give, Bow }

        // お題の動作の種類。お題の ID か、なければ場所で決める（同じ場所に新しいお題が増えても動きが付く）
        private enum Act { None, Swing, Eat, Hammer, Plow, Sweep, Generic }

        private static Act ActOf(string taskId, string spotId)
        {
            switch (taskId)
            {
                case "swing": return Act.Swing;
                case "eat_ramen": case "eat_porridge": return Act.Eat;
                case "hammer": return Act.Hammer;
                case "plow": return Act.Plow;
                case "sweep_home": return Act.Sweep;
            }
            switch (spotId)
            {
                case "training_ground": return Act.Swing;
                case "canteen": return Act.Eat;
                case "forge": return Act.Hammer;
                case "field": return Act.Plow;
                case "home": return Act.Sweep;
                default: return Act.Generic;
            }
        }

        // 道具の振りの位置。FillActMuscles と同じリズム
        private void ActPhase(float t, out float u, out float side)
        {
            float Wave(float speed) => 0.5f - 0.5f * Mathf.Cos(t * speed);
            side = Mathf.Sin(t * 3.2f);
            switch (act)
            {
                case Act.Swing: u = Mathf.Pow(Wave(6f), 0.6f); break;
                case Act.Eat: u = Wave(4.5f); break;
                case Act.Hammer: u = Mathf.Pow(Wave(4f), 0.5f); break;
                case Act.Plow: u = Mathf.Pow(Wave(3.6f), 0.6f); break;
                default: u = Wave(4f); break;
            }
        }

        private static PlayerActionProps.Prop PropOf(Act act)
        {
            switch (act)
            {
                case Act.Swing: return PlayerActionProps.Prop.Sword;
                case Act.Eat: return PlayerActionProps.Prop.Bowl;
                case Act.Hammer: return PlayerActionProps.Prop.Hammer;
                case Act.Plow: return PlayerActionProps.Prop.Hoe;
                case Act.Sweep: return PlayerActionProps.Prop.Broom;
                default: return PlayerActionProps.Prop.None;
            }
        }

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
        private Act act;
        private float actTime;        // 動作を始めてからの秒数（振りのリズムの起点）
        private float actWeight;      // 動作の姿勢にどれだけ寄せるか（0〜1、なめらかに切り替える）
        private PlayerActionProps props;
        private bool crafting;
        private bool ended; // ゲームが終わった（クリア・ゲームオーバーの姿勢を続ける）

        private HumanPoseHandler poseHandler;
        private HumanPose pose;
        private float[] idleMuscles;
        private float idleWeight;
        private int spineFB, chestFB, headTurn, headNod, leftArmFB, rightArmFB, leftArmDU, rightArmDU;
        private int spineTwist, chestTwist, leftForearm, rightForearm, leftUpperLeg, rightUpperLeg, leftLowerLeg, rightLowerLeg;
        private float[] actMuscles;   // その動作で決める筋肉の値（NaN は待機のまま）

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
            spineTwist = MuscleIndex("Spine Twist Left-Right");
            chestTwist = MuscleIndex("Chest Twist Left-Right");
            leftForearm = MuscleIndex("Left Forearm Stretch");
            rightForearm = MuscleIndex("Right Forearm Stretch");
            leftUpperLeg = MuscleIndex("Left Upper Leg Front-Back");
            rightUpperLeg = MuscleIndex("Right Upper Leg Front-Back");
            leftLowerLeg = MuscleIndex("Left Lower Leg Stretch");
            rightLowerLeg = MuscleIndex("Right Lower Leg Stretch");
            actMuscles = new float[HumanTrait.MuscleCount];
        }

        private void SetAct(int index, float value)
        {
            if (index >= 0) actMuscles[index] = value;
        }

        // お題ごとの腕・背骨・頭の動き。u は 0（構え）〜1（振り切り）をくり返す。
        // Arm Front-Back を大きくして腕を体の前に出し、Arm Down-Up で振りかぶり・振り下ろしを付ける
        private void FillActMuscles(float t)
        {
            for (int i = 0; i < actMuscles.Length; i++) actMuscles[i] = float.NaN;
            float Wave(float speed) => 0.5f - 0.5f * Mathf.Cos(t * speed);
            switch (act)
            {
                case Act.Swing:
                {
                    // 両手で剣を頭の上まで振りかぶり、前へ振り下ろす（下ろすほうを速く）
                    float u = Mathf.Pow(Wave(6f), 0.6f);
                    SetAct(leftArmDU, 0.85f - 1.2f * u); SetAct(rightArmDU, 0.85f - 1.2f * u);
                    SetAct(leftArmFB, 1f); SetAct(rightArmFB, 1f);
                    SetAct(leftForearm, 0.85f); SetAct(rightForearm, 0.85f);
                    SetAct(spineFB, 0.45f * u); SetAct(chestFB, 0.3f * u);
                    SetAct(headNod, -0.2f * u);
                    SetAct(leftUpperLeg, 0.3f); SetAct(leftLowerLeg, 0.7f);
                    break;
                }
                case Act.Eat:
                {
                    // 左手でお椀を胸の前に持ち、右手の箸を口元へ何度も運ぶ
                    float u = Wave(4.5f);
                    SetAct(leftArmDU, -0.15f); SetAct(leftArmFB, 0.9f); SetAct(leftForearm, -0.5f);
                    SetAct(rightArmDU, -0.05f + 0.25f * u); SetAct(rightArmFB, 0.95f); SetAct(rightForearm, 0.2f - 1.0f * u);
                    SetAct(spineFB, 0.3f); SetAct(chestFB, 0.15f);
                    SetAct(headNod, -0.35f + 0.15f * u);
                    break;
                }
                case Act.Hammer:
                {
                    // 右手でハンマーを頭の上まで振り上げ、前の台へ打ち下ろす。左手は台を押さえる
                    float u = Mathf.Pow(Wave(4f), 0.5f);
                    SetAct(rightArmDU, 0.9f - 1.3f * u); SetAct(rightArmFB, 0.85f); SetAct(rightForearm, 0.3f + 0.6f * u);
                    SetAct(leftArmDU, -0.3f); SetAct(leftArmFB, 0.8f); SetAct(leftForearm, 0.4f);
                    SetAct(spineFB, 0.45f * u); SetAct(chestTwist, -0.2f + 0.2f * u);
                    SetAct(headNod, -0.3f * u);
                    break;
                }
                case Act.Plow:
                {
                    // 両手でくわを肩の上まで振り上げ、前の地面へ打ち込む。腰を落とす
                    float u = Mathf.Pow(Wave(3.6f), 0.6f);
                    SetAct(leftArmDU, 0.75f - 1.15f * u); SetAct(rightArmDU, 0.75f - 1.15f * u);
                    SetAct(leftArmFB, 1f); SetAct(rightArmFB, 0.9f);
                    SetAct(leftForearm, 0.6f); SetAct(rightForearm, 0.6f);
                    SetAct(spineFB, 0.55f * u); SetAct(chestFB, 0.3f * u);
                    SetAct(leftUpperLeg, 0.4f); SetAct(rightUpperLeg, 0.15f);
                    SetAct(leftLowerLeg, 0.45f); SetAct(rightLowerLeg, 0.7f);
                    break;
                }
                case Act.Sweep:
                {
                    // ほうきを体の前で低く持ち、上体をひねって左右に掃く
                    float s = Mathf.Sin(t * 3.2f);
                    SetAct(leftArmDU, -0.5f); SetAct(rightArmDU, -0.45f);
                    SetAct(leftArmFB, 0.75f + 0.25f * s); SetAct(rightArmFB, 0.6f - 0.25f * s);
                    SetAct(leftForearm, 0.6f); SetAct(rightForearm, 0.6f);
                    SetAct(spineTwist, 0.35f * s); SetAct(chestTwist, 0.4f * s);
                    SetAct(spineFB, 0.35f);
                    SetAct(headTurn, 0.3f * s); SetAct(headNod, -0.3f);
                    break;
                }
                case Act.Generic:
                {
                    float u = Wave(4f);
                    SetAct(leftArmFB, 0.8f); SetAct(rightArmFB, 0.8f);
                    SetAct(leftArmDU, -0.3f + 0.3f * u); SetAct(rightArmDU, -0.3f + 0.3f * u);
                    SetAct(spineFB, 0.25f * u); SetAct(headNod, -0.2f);
                    break;
                }
            }

            // 一瞬の動作（渡す・話す）も腕と頭で見せる
            if (oneShot == OneShot.Give || oneShot == OneShot.Bow)
            {
                float k = Mathf.Clamp01(oneShotTime / oneShotLength);
                float arc = Mathf.Sin(k * Mathf.PI);
                if (oneShot == OneShot.Give)
                {
                    // 両手で前に差し出す
                    SetAct(leftArmFB, Mathf.Lerp(0.18f, 1f, arc)); SetAct(rightArmFB, Mathf.Lerp(0.18f, 1f, arc));
                    SetAct(leftArmDU, Mathf.Lerp(-0.62f, -0.1f, arc)); SetAct(rightArmDU, Mathf.Lerp(-0.62f, -0.1f, arc));
                    SetAct(leftForearm, 0.9f); SetAct(rightForearm, 0.9f);
                    SetAct(spineFB, 0.25f * arc);
                }
                else
                {
                    // 上体ごと深く会釈する
                    SetAct(headNod, -0.7f * arc);
                    SetAct(spineFB, 0.5f * arc); SetAct(chestFB, 0.4f * arc);
                    SetAct(leftArmFB, Mathf.Lerp(0.18f, 0.45f, arc)); SetAct(rightArmFB, Mathf.Lerp(0.18f, 0.45f, arc));
                }
            }
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
            bool hasAct = actWeight > 0.001f || oneShot == OneShot.Give || oneShot == OneShot.Bow;
            if (hasAct) FillActMuscles(t - actTime);
            float oneShotArmWeight = oneShot == OneShot.Give || oneShot == OneShot.Bow ? 1f : 0f;
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
                if (hasAct && !float.IsNaN(actMuscles[i])) target = Mathf.Lerp(target, actMuscles[i], Mathf.Max(actWeight, oneShotArmWeight));
                pose.muscles[i] = Mathf.Lerp(pose.muscles[i], target, idleWeight);
            }
            poseHandler.SetHumanPose(ref pose);
        }

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context != null && context.Surveillance != null) context.Surveillance.OnCaught += HandleCaught;
            spots = FindObjectsByType<TaskSpot>(FindObjectsSortMode.None);
            foreach (var spot in spots)
            {
                var s = spot;
                s.OnActing += value => HandleActing(s, value);
            }
            if (context != null && context.Tasks != null) context.Tasks.OnTaskCompleted += HandleTaskCompleted;
            props = GetComponent<PlayerActionProps>();
            if (props == null) props = gameObject.AddComponent<PlayerActionProps>();
            props.Setup(animator);
            if (TryGetComponent(out OutfitController outfits)) outfits.OnEquipped += _ => Play(OneShot.Equip, 0.6f);
        }

        private void OnDestroy()
        {
            poseHandler?.Dispose();
            if (context != null && context.Surveillance != null) context.Surveillance.OnCaught -= HandleCaught;
            if (context != null && context.Tasks != null) context.Tasks.OnTaskCompleted -= HandleTaskCompleted;
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
            if (props != null) props.Clear();
            Play(OneShot.Celebrate, 1.8f);
        }

        // ゲームオーバーのとき（PlayerVfx から呼ぶ）
        public void PlaySlump()
        {
            ended = true;
            if (props != null) props.Clear();
            Play(OneShot.Slump, 1.2f);
        }

        private void HandleCaught(Team5.NPC.VisionSensor watcher)
        {
            SetTrigger(CaughtId);
            Play(OneShot.Caught, 0.6f);
        }

        private void HandleActing(TaskSpot spot, bool value)
        {
            acting = value;
            if (value)
            {
                var task = context != null && context.Tasks != null ? context.Tasks.Current : null;
                act = ActOf(task != null ? task.Definition.taskId : null, spot != null ? spot.SpotId : null);
                actTime = Time.time;
            }
            if (props != null) props.Show(value && !ended ? PropOf(act) : PlayerActionProps.Prop.None);
            SetBool(ActingId, value);
        }

        // 渡す・話すは一瞬で終わるので、終わったときに一度だけ動く
        private void HandleTaskCompleted(TaskInstance task)
        {
            if (task == null || task.Definition == null) return;
            if (task.Definition.type == Team5.Data.TaskType.BringItem) Play(OneShot.Give, 0.7f);
            else if (task.Definition.type == Team5.Data.TaskType.TalkToNpc) Play(OneShot.Bow, 0.8f);
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

            actWeight = Mathf.MoveTowards(actWeight, acting && !ended ? 1f : 0f, Time.deltaTime / 0.15f);
            if (acting && !ended)
            {
                float at = t - actTime;
                if (poseHandler == null)
                {
                    // Humanoid でないとき：体全体を前後に振る
                    float swing = Mathf.Sin(at * 9f);
                    euler.x += 14f * swing;
                    offset.y += 0.04f * Mathf.Abs(swing);
                }
                else
                {
                    // 腕の動きに合わせて、体全体も動かす（前かがみ・沈み込み・ひねり）
                    float W(float speed, float pow) => Mathf.Pow(0.5f - 0.5f * Mathf.Cos(at * speed), pow);
                    switch (act)
                    {
                        case Act.Swing: { float u = W(6f, 0.6f); euler.x += 10f * u; offset.y -= 0.04f * u; break; }
                        case Act.Hammer: { float u = W(4f, 0.5f); euler.x += 12f * u; offset.y -= 0.05f * Mathf.Pow(u, 4f); break; }
                        case Act.Plow: { float u = W(3.6f, 0.6f); euler.x += 14f * u; offset.y -= 0.08f * u; break; }
                        case Act.Sweep: euler.y += 12f * Mathf.Sin(at * 3.2f); euler.x += 6f; break;
                        case Act.Eat: euler.x += 8f; offset.y += 0.015f * Mathf.Sin(at * 9f); break;
                        default: euler.x += 6f * W(4f, 1f); break;
                    }
                }
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
                    case OneShot.Give: // 前に差し出す（腕は FillActMuscles）
                        euler.x += 20f * arc;
                        offset += Vector3.forward * 0.08f * arc; // プレイヤーから見た前へ少し出る
                        break;
                    case OneShot.Bow: // 会釈（頭と腕は FillActMuscles）
                        euler.x += 40f * arc;
                        break;
                }
                // 終わりの姿勢を残すもの以外は元に戻す
                if (k >= 1f && oneShot != OneShot.Slump) oneShot = OneShot.None;
            }

            model.localPosition = basePosition + offset;
            model.localRotation = baseRotation * Quaternion.Euler(euler);
            model.localScale = Vector3.Scale(baseScale, scale);
            if (props != null && acting) // 姿勢が決まったあとで道具を動かす
            {
                ActPhase(Time.time - actTime, out float u, out float side);
                props.Place(u, side);
            }
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
