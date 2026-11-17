using Team5.Core;
using Team5.NPC;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 画面上部に「見られている！」、見つかったら「見つかった！ 平和度 -25」を出す仮UI（目のアイコンは CD-14）
    public class WatchedIndicatorView : MonoBehaviour
    {
        [SerializeField] private Text watchedLabel;
        [SerializeField] private Text caughtLabel;
        [SerializeField] private float caughtDisplaySeconds = 1.5f;

        private SurveillanceManager surveillance;
        private float caughtHideTime;

        private void Start()
        {
            var context = GameSceneContext.Instance;
            if (context == null || context.Surveillance == null)
            {
                Debug.LogWarning("GameSceneContext か SurveillanceManager がないので見張りの表示を出せません。", this);
                enabled = false;
                return;
            }
            surveillance = context.Surveillance;
            surveillance.OnPhaseChanged += HandlePhaseChanged;
            surveillance.OnCaught += HandleCaught;
            HandlePhaseChanged(surveillance.CurrentPhase);
            caughtLabel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (surveillance == null) return;
            surveillance.OnPhaseChanged -= HandlePhaseChanged;
            surveillance.OnCaught -= HandleCaught;
        }

        private void HandlePhaseChanged(Phase phase)
        {
            watchedLabel.text = phase == Phase.Omote ? "見られている！" : "見られていない";
        }

        private void HandleCaught(VisionSensor watcher)
        {
            var balance = GameSceneContext.Instance.Balance;
            caughtLabel.text = $"見つかった！ 平和度 -{balance.caughtPeacePenalty:0}";
            caughtLabel.gameObject.SetActive(true);
            caughtHideTime = Time.time + caughtDisplaySeconds;
        }

        private void Update()
        {
            if (caughtLabel.gameObject.activeSelf && Time.time >= caughtHideTime) caughtLabel.gameObject.SetActive(false);
        }
    }
}
