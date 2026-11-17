using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Team5.DebugTools
{
    // 開発用のパネル。ビルドでも F1 で開ける
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] private RoleDefinition fallbackRole; // GameManager がいないときに材料を配るロール
        [SerializeField] private bool visible;

        private static readonly ParameterType[] Types =
            { ParameterType.Individuality, ParameterType.Peace, ParameterType.RoleProgress };

        private GUIStyle labelStyle;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            var context = GameSceneContext.Instance;
            if (context == null) return;

            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16 };

            GUILayout.BeginArea(new Rect(10, 10, 360, 420), "デバッグ（F1）", GUI.skin.window);

            var parameters = context.Parameters;
            var balance = context.Balance;
            if (parameters != null && balance != null)
            {
                foreach (var type in Types)
                {
                    float current = parameters.Get(type);
                    float max = MaxOf(type, balance);
                    GUILayout.Label($"{type}：{current:0}/{max:0}", labelStyle);
                    float next = GUILayout.HorizontalSlider(current, 0f, max);
                    if (!Mathf.Approximately(next, current)) parameters.Add(type, next - current, "debug");
                    GUILayout.Space(6);
                }
            }

            if (context.Timer != null)
            {
                GUILayout.Label($"残り時間：{context.Timer.Remaining:0.0} 秒", labelStyle);
                if (GUILayout.Button("残り時間を 10 秒にする")) context.Timer.StartTimer(10f);
            }

            // CD-08c ができたら SurveillanceManager の「見つかった」処理に置き換える
            if (GUILayout.Button("見つかる") && parameters != null && balance != null)
                parameters.Add(ParameterType.Peace, -balance.caughtPeacePenalty, "debug caught");

            if (GUILayout.Button("全アイテムを1個ずつもらう")) GiveAllItems(context);

            GUILayout.EndArea();
        }

        private void GiveAllItems(GameSceneContext context)
        {
            var manager = GameManager.Instance;
            var role = manager != null && manager.CurrentRole != null ? manager.CurrentRole : fallbackRole;
            if (role == null || role.recipes == null || context.Inventory == null) return;

            foreach (var recipe in role.recipes)
            {
                if (recipe == null || recipe.inputs == null) continue;
                foreach (var input in recipe.inputs)
                {
                    if (input.item != null) context.Inventory.Add(input.item, 1);
                }
            }
        }

        private static float MaxOf(ParameterType type, GameBalance balance)
        {
            return type switch
            {
                ParameterType.Individuality => balance.individualityMax,
                ParameterType.Peace => balance.peaceMax,
                ParameterType.RoleProgress => balance.roleProgressMax,
                _ => 0f,
            };
        }
    }
}
