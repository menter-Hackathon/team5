using UnityEngine;

namespace Team5.Player
{
    // お題をしている間だけ、手に道具を持たせる（素振り＝剣、食べる＝お椀と箸、ハンマー、くわ、ほうき）。
    // 道具は手の骨の位置に毎フレーム置き直すので、どのモーションでも手から離れない
    public class PlayerActionProps : MonoBehaviour
    {
        public enum Prop { None, Sword, Bowl, Hammer, Hoe, Broom, Pencil, Tuner, Guitar }

        [SerializeField] private float scale = 1.3f; // モデルの大きさに合わせる（頭が大きい体型なので少し大きめ）

        private Animator animator;
        private Transform rightHand;
        private Transform root;      // 道具をまとめる親
        private Transform main;      // 右手に持つもの（剣・ハンマー・箸・くわ・ほうき）
        private Transform bowl;      // 左手に持つお椀
        private Prop current;

        private static Material wood, metal, bowlMat, soup, straw, paper, dark;

        public void Setup(Animator target)
        {
            animator = target;
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return;
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        public void Show(Prop prop)
        {
            if (prop == current) return;
            Clear();
            current = prop;
            if (prop == Prop.None || rightHand == null) return;
            EnsureMaterials();
            root = new GameObject("ActionProps").transform;
            root.SetParent(transform, false);

            main = new GameObject(prop.ToString()).transform;
            main.SetParent(root, false);
            float s = scale;
            switch (prop)
            {
                case Prop.Sword: // 握り → つば → 刃
                    Part(PrimitiveType.Cylinder, main, new Vector3(0, 0.05f, 0), new Vector3(0.035f, 0.07f, 0.035f), wood);
                    Part(PrimitiveType.Cube, main, new Vector3(0, 0.13f, 0), new Vector3(0.16f, 0.025f, 0.04f), metal);
                    Part(PrimitiveType.Cube, main, new Vector3(0, 0.42f, 0), new Vector3(0.05f, 0.56f, 0.015f), metal);
                    break;
                case Prop.Hammer: // 柄 → 頭
                    Part(PrimitiveType.Cylinder, main, new Vector3(0, 0.16f, 0), new Vector3(0.03f, 0.2f, 0.03f), wood);
                    Part(PrimitiveType.Cube, main, new Vector3(0, 0.37f, 0), new Vector3(0.2f, 0.09f, 0.09f), metal);
                    break;
                case Prop.Hoe: // 長い柄 → 刃（下向き）
                    Part(PrimitiveType.Cylinder, main, new Vector3(0, 0.25f, 0), new Vector3(0.03f, 0.45f, 0.03f), wood);
                    Part(PrimitiveType.Cube, main, new Vector3(0, 0.68f, 0.07f), new Vector3(0.16f, 0.03f, 0.16f), metal);
                    break;
                case Prop.Broom: // 長い柄 → 穂
                    Part(PrimitiveType.Cylinder, main, new Vector3(0, 0.3f, 0), new Vector3(0.028f, 0.5f, 0.028f), wood);
                    Part(PrimitiveType.Cube, main, new Vector3(0, 0.8f, 0), new Vector3(0.22f, 0.16f, 0.05f), straw);
                    break;
                case Prop.Pencil: // 右手に鉛筆、体の前に紙（bowl の枠を紙に使う）
                    Part(PrimitiveType.Cylinder, main, new Vector3(0, 0.06f, 0), new Vector3(0.018f, 0.06f, 0.018f), straw);
                    Part(PrimitiveType.Cube, main, new Vector3(0, 0.005f, 0), new Vector3(0.012f, 0.012f, 0.012f), dark);
                    bowl = new GameObject("Paper").transform;
                    bowl.SetParent(root, false);
                    Part(PrimitiveType.Cube, bowl, Vector3.zero, new Vector3(0.24f, 0.004f, 0.17f), paper);
                    break;
                case Prop.Tuner: // 手のひらサイズの機材と画面・つまみ
                    Part(PrimitiveType.Cube, main, Vector3.zero, new Vector3(0.16f, 0.1f, 0.05f), dark);
                    Part(PrimitiveType.Cube, main, new Vector3(-0.025f, 0.01f, 0.026f), new Vector3(0.07f, 0.045f, 0.004f), soup);
                    Part(PrimitiveType.Cylinder, main, new Vector3(0.05f, 0f, 0.03f), new Vector3(0.03f, 0.01f, 0.03f), metal);
                    break;
                case Prop.Guitar: // 胴（丸）・ネック・ヘッド。ローカル X がネックの向き、Z が表面の向き
                    Part(PrimitiveType.Cylinder, main, Vector3.zero, new Vector3(0.3f, 0.035f, 0.3f), bowlMat).transform.localEulerAngles = new Vector3(90f, 0f, 0f);
                    Part(PrimitiveType.Cylinder, main, new Vector3(0f, 0f, 0.02f), new Vector3(0.08f, 0.004f, 0.08f), dark).transform.localEulerAngles = new Vector3(90f, 0f, 0f);
                    Part(PrimitiveType.Cube, main, new Vector3(0.3f, 0f, 0f), new Vector3(0.36f, 0.045f, 0.025f), wood);
                    Part(PrimitiveType.Cube, main, new Vector3(0.52f, 0f, 0f), new Vector3(0.09f, 0.06f, 0.03f), wood);
                    break;
                case Prop.Bowl: // 右手に箸、左手にお椀
                    Part(PrimitiveType.Cube, main, new Vector3(-0.012f, 0.1f, 0), new Vector3(0.01f, 0.22f, 0.01f), wood);
                    Part(PrimitiveType.Cube, main, new Vector3(0.012f, 0.1f, 0), new Vector3(0.01f, 0.22f, 0.01f), wood);
                    bowl = new GameObject("Bowl").transform;
                    bowl.SetParent(root, false);
                    Part(PrimitiveType.Cylinder, bowl, new Vector3(0, 0.045f, 0), new Vector3(0.26f, 0.045f, 0.26f), bowlMat);
                    Part(PrimitiveType.Cylinder, bowl, new Vector3(0, 0.09f, 0), new Vector3(0.22f, 0.004f, 0.22f), soup);
                    break;
            }
            main.localScale = Vector3.one * s;
            if (bowl != null) bowl.localScale = Vector3.one * s;
            Place(0f, 0f);
        }

        public void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = main = bowl = null;
            current = Prop.None;
        }

        // モデルの姿勢が決まったあと（PlayerAnimator.LateUpdate の最後）に呼ぶ。
        // u：0（構え）〜1（振り切り）、side：-1〜1（掃く向き）。道具の軌道は体の向きを基準に決める
        // （頭が大きく腕が短い体型なので、腕の向きだけに合わせると前に振れて見えないため）
        public void Place(float u, float side)
        {
            if (main == null) return;
            Vector3 basePos = transform.position;
            Vector3 fwd = transform.forward, right = transform.right, up = Vector3.up;
            switch (current)
            {
                case Prop.Sword:
                {
                    // 頭の上・少し後ろから、体の前・下へ振り下ろす
                    Vector3 grip = basePos + up * Mathf.Lerp(1.2f, 0.8f, u) + fwd * Mathf.Lerp(0.05f, 0.32f, u);
                    Vector3 dir = Quaternion.AngleAxis(Mathf.Lerp(-40f, 100f, u), right) * up;
                    main.SetPositionAndRotation(grip, Quaternion.LookRotation(Vector3.Cross(right, dir), dir));
                    break;
                }
                case Prop.Hammer:
                {
                    // 右手で高く振り上げ、前の台へ打ち下ろす
                    Vector3 grip = basePos + right * 0.15f + up * Mathf.Lerp(1.15f, 0.75f, u) + fwd * Mathf.Lerp(0f, 0.3f, u);
                    Vector3 dir = Quaternion.AngleAxis(Mathf.Lerp(-60f, 110f, u), right) * up;
                    main.SetPositionAndRotation(grip, Quaternion.LookRotation(Vector3.Cross(right, dir), dir));
                    break;
                }
                case Prop.Hoe:
                {
                    // 両手で肩の上まで振り上げ、刃を前の地面へ打ち込む（柄の途中を握る）
                    Vector3 hold = basePos + up * Mathf.Lerp(1.05f, 0.7f, u) + fwd * Mathf.Lerp(0.05f, 0.25f, u);
                    Vector3 dir = Quaternion.AngleAxis(Mathf.Lerp(-30f, 125f, u), right) * up;
                    main.SetPositionAndRotation(hold - dir * 0.25f * scale, Quaternion.LookRotation(Vector3.Cross(right, dir), dir));
                    break;
                }
                case Prop.Broom:
                {
                    // 穂先を前の地面につけたまま、左右に掃く
                    Vector3 hold = basePos + up * 0.7f + fwd * 0.2f;
                    Vector3 dir = Quaternion.AngleAxis(35f * side, up) * (fwd * 0.55f + Vector3.down).normalized;
                    main.SetPositionAndRotation(hold - dir * 0.2f * scale, Quaternion.LookRotation(Vector3.Cross(right, dir), dir));
                    break;
                }
                case Prop.Pencil:
                {
                    // 体の前の紙に、鉛筆の先で書き込む（side で細かく、u で行を進める）
                    Vector3 paperPos = basePos + up * 0.66f + fwd * 0.36f;
                    if (bowl != null) bowl.SetPositionAndRotation(paperPos, Quaternion.LookRotation(fwd, up));
                    Vector3 tip = paperPos + up * 0.01f + right * (0.05f * side) + fwd * Mathf.Lerp(0.05f, -0.05f, u);
                    Vector3 dir = (up * 0.85f - fwd * 0.3f + right * 0.25f).normalized; // 先 → 握り
                    main.SetPositionAndRotation(tip, Quaternion.LookRotation(Vector3.Cross(right, dir), dir));
                    break;
                }
                case Prop.Tuner:
                {
                    Vector3 pos = basePos + up * 0.72f + fwd * 0.34f - right * 0.04f;
                    main.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, up) * Quaternion.Euler(-20f, 0f, 4f * side));
                    break;
                }
                case Prop.Guitar:
                {
                    // 胴は体の前で右寄り、ネックは左上へ。かき鳴らしに合わせて少し揺れる
                    Vector3 body = basePos + up * (0.6f + 0.01f * side) + fwd * 0.3f + right * 0.06f;
                    Vector3 neck = (-right * 0.9f + up * 0.35f).normalized;
                    Vector3 face = Quaternion.AngleAxis(3f * side, neck) * fwd;
                    main.SetPositionAndRotation(body, Quaternion.LookRotation(face, Vector3.Cross(face, neck)));
                    break;
                }
                case Prop.Bowl:
                {
                    // お椀は胸の前、箸はお椀と口のあいだを往復する
                    Vector3 bowlPos = basePos + up * 0.68f + fwd * 0.34f - right * 0.04f;
                    if (bowl != null) bowl.SetPositionAndRotation(bowlPos, Quaternion.LookRotation(fwd, up));
                    Vector3 mouth = basePos + up * 1.0f + fwd * 0.26f;
                    Vector3 tip = Vector3.Lerp(bowlPos + up * 0.1f, mouth, u);
                    Vector3 dir = (fwd * 0.5f + Vector3.down * 0.85f + right * 0.2f).normalized; // 握り → 先
                    main.SetPositionAndRotation(tip - dir * 0.22f * scale, Quaternion.LookRotation(Vector3.Cross(right, dir), dir));
                    break;
                }
            }
        }

        private void OnDestroy() => Clear();

        private static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static void EnsureMaterials()
        {
            if (wood != null) return;
            wood = Mat(new Color(0.55f, 0.38f, 0.22f));
            metal = Mat(new Color(0.78f, 0.8f, 0.84f));
            bowlMat = Mat(new Color(0.85f, 0.3f, 0.22f));
            soup = Mat(new Color(0.93f, 0.55f, 0.2f));
            straw = Mat(new Color(0.86f, 0.72f, 0.38f));
            paper = Mat(new Color(0.96f, 0.94f, 0.88f));
            dark = Mat(new Color(0.18f, 0.18f, 0.2f));
        }

        private static Material Mat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.2f);
            return m;
        }
    }
}
