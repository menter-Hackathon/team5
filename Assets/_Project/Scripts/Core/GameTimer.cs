using System;
using UnityEngine;

#pragma warning disable CS0067 // 雛形のため、まだ呼ばれないイベントの警告を止める（実装したら消す）

namespace Team5.Core
{
    public class GameTimer : MonoBehaviour
    {
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsRunning { get; private set; }
        public event Action<float> OnTick;      // 毎フレーム、残り秒数
        public event Action OnTimeUp;

        public void StartTimer(float seconds)
        {
            Debug.Log("TODO: GameTimer.StartTimer");
        }

        public void Stop()
        {
            Debug.Log("TODO: GameTimer.Stop");
        }
    }
}
