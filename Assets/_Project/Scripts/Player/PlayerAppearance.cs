using Team5.Core;
using Team5.Data;
using UnityEngine;

namespace Team5.Player
{
    // 個性の濃さに合わせて、子の Renderer すべての _Individuality と _RoleColor を変える
    public class PlayerAppearance : MonoBehaviour
    {
        [SerializeField] private float transitionSeconds = 0.5f; // 0→1 に変わるのにかかる秒数
        [SerializeField] private RoleDefinition fallbackRole;    // GameManager がいないときの色

        private static readonly int IndividualityId = Shader.PropertyToID("_Individuality");
        private static readonly int RoleColorId = Shader.PropertyToID("_RoleColor");

        private PlayerParameters parameters;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private float displayed;
        private float target;
        private Color roleColor = Color.white;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            RefreshRenderers();
        }

        private void Start()
        {
            var manager = GameManager.Instance;
            var role = manager != null && manager.CurrentRole != null ? manager.CurrentRole : fallbackRole;
            if (role != null) roleColor = role.roleColor;

            var context = GameSceneContext.Instance;
            if (context != null && context.Parameters != null)
            {
                parameters = context.Parameters;
                parameters.OnChanged += HandleChanged;
                target = parameters.GetNormalized(ParameterType.Individuality);
            }
            displayed = target;
            Apply();
        }

        private void OnDestroy()
        {
            if (parameters != null) parameters.OnChanged -= HandleChanged;
        }

        private void HandleChanged(ParameterType type, float before, float after)
        {
            if (type != ParameterType.Individuality) return;
            target = parameters.GetNormalized(ParameterType.Individuality);
        }

        private void Update()
        {
            if (Mathf.Approximately(displayed, target)) return;
            float step = transitionSeconds > 0f ? Time.deltaTime / transitionSeconds : 1f;
            displayed = Mathf.MoveTowards(displayed, target, step);
            Apply();
        }

        // 衣装を付け替えたとき（CD-12）に呼ぶ
        public void RefreshRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            if (block != null) Apply();
        }

        private void Apply()
        {
            foreach (var item in renderers)
            {
                if (item == null) continue;
                item.GetPropertyBlock(block);
                block.SetFloat(IndividualityId, displayed);
                block.SetColor(RoleColorId, roleColor);
                item.SetPropertyBlock(block);
            }
        }
    }
}
