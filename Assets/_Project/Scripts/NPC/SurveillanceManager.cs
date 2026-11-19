using System;
using System.Collections.Generic;
using Team5.Core;
using UnityEngine;

namespace Team5.NPC
{
    // すべての NPC の視界をまとめて、今見られているか（表/裏）を決める。
    // 見られている間に「らしくない行動」をしたら平和度を下げる
    public class SurveillanceManager : MonoBehaviour
    {
        [SerializeField] private float caughtPauseSeconds = 1.5f; // 見つけた NPC が止まってこちらを向く時間

        public bool IsPlayerWatched { get; private set; }
        public Phase CurrentPhase { get; private set; } = Phase.Ura;  // 見られていれば Omote
        public int TimesCaught { get; private set; }
        public event Action<Phase> OnPhaseChanged;
        public event Action<VisionSensor> OnCaught;     // らしくない行動を見られた

        private readonly List<VisionSensor> sensors = new();
        private float lastCaughtTime = float.NegativeInfinity;

        public void Register(VisionSensor sensor)
        {
            if (sensor != null && !sensors.Contains(sensor)) sensors.Add(sensor);
        }

        public void Unregister(VisionSensor sensor)
        {
            sensors.Remove(sensor);
        }

        private void Update()
        {
            bool watched = FindWatcher() != null;
            if (watched == IsPlayerWatched) return;

            IsPlayerWatched = watched;
            CurrentPhase = watched ? Phase.Omote : Phase.Ura;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        // 行動を報告。見つかったら true を返す
        public bool ReportAction(bool suspicious)
        {
            if (!suspicious) return false;
            var watcher = FindWatcher();
            if (watcher == null) return false;

            var context = GameSceneContext.Instance;
            if (context == null || context.Balance == null) return false;
            var balance = context.Balance;

            // 連続で減りすぎないように
            if (Time.time - lastCaughtTime < balance.caughtCooldownSeconds) return false;
            lastCaughtTime = Time.time;

            if (context.Parameters != null)
                context.Parameters.Add(ParameterType.Peace, -balance.caughtPeacePenalty, "caught");
            TimesCaught++;
            OnCaught?.Invoke(watcher);

            StopAndFacePlayer(watcher, context.Player);
            return true;
        }

        private VisionSensor FindWatcher()
        {
            foreach (var sensor in sensors)
            {
                if (sensor != null && sensor.isActiveAndEnabled && sensor.CanSeePlayer) return sensor;
            }
            return null;
        }

        private void StopAndFacePlayer(VisionSensor watcher, Transform player)
        {
            bool hasPatrol = watcher.TryGetComponent(out NpcPatrol patrol);
            if (hasPatrol) patrol.Pause(caughtPauseSeconds);
            if (player == null) return;
            if (hasPatrol)
            {
                patrol.FaceTowards(player.position); // くるっと一瞬で向かず、素早く振り向く
                return;
            }

            Vector3 toPlayer = player.position - watcher.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f) watcher.transform.rotation = Quaternion.LookRotation(toPlayer);
        }
    }
}
