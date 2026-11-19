using System;
using UnityEngine;

namespace Team5.Core
{
    // 制限時間。秒数は GameBalance.timeLimitSeconds から渡される
    public class GameTimer : MonoBehaviour
    {
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsRunning { get; private set; }
        public event Action<float> OnTick;      // 毎フレーム、残り秒数
        public event Action OnTimeUp;

        public void StartTimer(float seconds)
        {
            Remaining = seconds;
            Elapsed = 0f;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        // 一時停止中は Time.timeScale = 0 なので自動で止まる
        private void Update()
        {
            if (!IsRunning) return;

            float delta = Mathf.Min(Time.deltaTime, Remaining);
            Remaining -= delta;
            Elapsed += delta;
            OnTick?.Invoke(Remaining);

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                IsRunning = false;
                OnTimeUp?.Invoke();
            }
        }
    }
}
