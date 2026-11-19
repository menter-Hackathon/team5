using System;
using Team5.Data;
using UnityEngine;

namespace Team5.Core
{
    // 個性の濃さ・村の平和度・ロールへの進み具合を管理する。初期値と最大値は GameBalance から読む
    public class PlayerParameters : MonoBehaviour
    {
        private readonly float[] values = new float[3];
        private bool initialized;

        public float Get(ParameterType type)
        {
            EnsureInitialized();
            return values[(int)type];
        }

        // 0〜1
        public float GetNormalized(ParameterType type)
        {
            float max = GetMax(type);
            return max > 0f ? Get(type) / max : 0f;
        }

        // 0〜最大値に丸める
        public void Add(ParameterType type, float delta, string reason)
        {
            EnsureInitialized();
            float old = values[(int)type];
            float next = Mathf.Clamp(old + delta, 0f, GetMax(type));
            if (Mathf.Approximately(old, next)) return;

            values[(int)type] = next;
            Debug.Log($"[Param] {type} {old}->{next} ({reason})");
            OnChanged?.Invoke(type, old, next);
        }

        public event Action<ParameterType, float, float> OnChanged; // (種類, 変更前, 変更後)

        private void Start()
        {
            EnsureInitialized();
        }

        // GameSceneContext.Awake より先に呼ばれても困らないよう、最初に使われたときに初期化する
        private void EnsureInitialized()
        {
            if (initialized) return;
            var balance = Balance;
            if (balance == null) return;

            values[(int)ParameterType.Individuality] = balance.individualityInitial;
            values[(int)ParameterType.Peace] = balance.peaceInitial;
            values[(int)ParameterType.RoleProgress] = 0f;
            initialized = true;
        }

        private float GetMax(ParameterType type)
        {
            var balance = Balance;
            if (balance == null) return 0f;
            return type switch
            {
                ParameterType.Individuality => balance.individualityMax,
                ParameterType.Peace => balance.peaceMax,
                ParameterType.RoleProgress => balance.roleProgressMax,
                _ => 0f,
            };
        }

        private GameBalance Balance
        {
            get
            {
                var context = GameSceneContext.Instance;
                return context != null ? context.Balance : null;
            }
        }
    }
}
