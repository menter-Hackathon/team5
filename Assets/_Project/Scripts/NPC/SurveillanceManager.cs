using System;
using Team5.Core;
using UnityEngine;

#pragma warning disable CS0067 // 雛形のため、まだ呼ばれないイベントの警告を止める（実装したら消す）

namespace Team5.NPC
{
    public class SurveillanceManager : MonoBehaviour
    {
        public bool IsPlayerWatched { get; private set; }
        public Phase CurrentPhase { get; private set; }               // 見られていれば Omote
        public int TimesCaught { get; private set; }
        public event Action<Phase> OnPhaseChanged;
        public event Action<VisionSensor> OnCaught;     // らしくない行動を見られた

        public void Register(VisionSensor sensor)
        {
            Debug.Log("TODO: SurveillanceManager.Register");
        }

        public void Unregister(VisionSensor sensor)
        {
            Debug.Log("TODO: SurveillanceManager.Unregister");
        }

        // 行動を報告。見つかったら true を返す
        public bool ReportAction(bool suspicious)
        {
            Debug.Log("TODO: SurveillanceManager.ReportAction");
            return default;
        }
    }
}
