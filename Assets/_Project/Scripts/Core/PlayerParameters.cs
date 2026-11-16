using System;
using UnityEngine;

#pragma warning disable CS0067 // 雛形のため、まだ呼ばれないイベントの警告を止める（実装したら消す）

namespace Team5.Core
{
    public class PlayerParameters : MonoBehaviour
    {
        public float Get(ParameterType type)
        {
            Debug.Log("TODO: PlayerParameters.Get");
            return default;
        }

        // 0〜1
        public float GetNormalized(ParameterType type)
        {
            Debug.Log("TODO: PlayerParameters.GetNormalized");
            return default;
        }

        // 0〜最大値に丸める
        public void Add(ParameterType type, float delta, string reason)
        {
            Debug.Log("TODO: PlayerParameters.Add");
        }

        public event Action<ParameterType, float, float> OnChanged; // (種類, 変更前, 変更後)
    }
}
