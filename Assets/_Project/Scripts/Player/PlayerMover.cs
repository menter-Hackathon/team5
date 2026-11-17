using UnityEngine;

namespace Team5.Player
{
    // CharacterController でワールドの XZ 平面を移動する（カメラは回らないので W = 画面の上）
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;   // m/秒
        [SerializeField] private float turnSpeed = 720f; // 度/秒
        [SerializeField] private float groundedStickVelocity = -2f; // 接地中に地面へ押しつける速さ

        // インスペクターから確認できるようにフィールドを持たせる
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private bool canMove = true;

        // 個性が減ったときのデバフで使う（CD-13）
        public float SpeedMultiplier { get => speedMultiplier; set => speedMultiplier = value; }

        // クラフト中・会話中は false にする
        public bool CanMove { get => canMove; set => canMove = value; }

        private CharacterController controller;
        private PlayerInputReader input;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            Vector2 move = CanMove ? input.Move : Vector2.zero;
            Vector3 direction = new Vector3(move.x, 0f, move.y);
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            // 重力（ジャンプはしない）
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = groundedStickVelocity;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 velocity = direction * (moveSpeed * SpeedMultiplier);
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
