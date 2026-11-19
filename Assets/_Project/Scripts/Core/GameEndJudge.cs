using System;
using System.Collections;
using Team5.Player;
using UnityEngine;

namespace Team5.Core
{
    // イベントを購読して勝敗を判定し GameManager.EndGame を呼ぶ。個性が減りすぎたときのデバフもここで入れる
    public class GameEndJudge : MonoBehaviour
    {
        [SerializeField] private PlayerMover mover; // 空なら GameSceneContext.Player から探す
        [SerializeField] private float endEffectSeconds = 2.5f; // 勝敗が決まってから結果画面に移るまで、演出（AN-03）を見せる秒数

        // 勝敗が決まった瞬間（結果画面に移る前）。PlayerVfx がクリア・ゲームオーバーの演出を出す
        public event Action<GameResult> OnEnding;

        private GameSceneContext context;
        private bool ended;

        private void Start()
        {
            context = GameSceneContext.Instance;
            if (context == null || context.Parameters == null || context.Timer == null || context.Balance == null)
            {
                Debug.LogWarning("GameSceneContext の参照が足りないので勝敗を判定できません。", this);
                enabled = false;
                return;
            }
            if (mover == null && context.Player != null) context.Player.TryGetComponent(out mover);

            context.Parameters.OnChanged += HandleParameterChanged;
            context.Timer.OnTimeUp += HandleTimeUp;
            UpdateDebuff();
        }

        private void OnDestroy()
        {
            if (context == null) return;
            if (context.Parameters != null) context.Parameters.OnChanged -= HandleParameterChanged;
            if (context.Timer != null) context.Timer.OnTimeUp -= HandleTimeUp;
        }

        private void HandleParameterChanged(ParameterType type, float before, float after)
        {
            if (type == ParameterType.Individuality) UpdateDebuff();
            Judge();
        }

        private void HandleTimeUp()
        {
            Judge();
            if (!ended) End(GameResult.GameOverTimeUp);
        }

        // win-lose.md の優先順：クリア → 平和度 0 → 個性 0
        private void Judge()
        {
            if (ended) return;
            var parameters = context.Parameters;
            var balance = context.Balance;

            if (parameters.Get(ParameterType.RoleProgress) >= balance.roleProgressToWin) End(GameResult.Clear);
            else if (parameters.Get(ParameterType.Peace) <= 0f) End(GameResult.GameOverPeace);
            else if (parameters.Get(ParameterType.Individuality) <= 0f) End(GameResult.GameOverFaded);
        }

        private void End(GameResult result)
        {
            ended = true;
            context.Timer.Stop();

            var parameters = context.Parameters;
            var summary = new GameResultSummary
            {
                individuality = parameters.Get(ParameterType.Individuality),
                peace = parameters.Get(ParameterType.Peace),
                roleProgress = parameters.Get(ParameterType.RoleProgress),
                elapsedSeconds = context.Timer.Elapsed,
                tasksCompleted = context.Tasks != null ? context.Tasks.CompletedCount : 0,
                timesCaught = context.Surveillance != null ? context.Surveillance.TimesCaught : 0,
            };

            if (mover != null) mover.CanMove = false;
            if (context.Player != null && context.Player.TryGetComponent(out PlayerInteractor interactor)) interactor.enabled = false;
            OnEnding?.Invoke(result);
            StartCoroutine(EndAfterEffect(result, summary));
        }

        private IEnumerator EndAfterEffect(GameResult result, GameResultSummary summary)
        {
            yield return new WaitForSecondsRealtime(endEffectSeconds);
            var manager = GameManager.Instance;
            if (manager == null)
            {
                // Game シーンを直接再生したときも結果画面を見られるように、その場で GameManager を作る
                manager = new GameObject("GameManager").AddComponent<GameManager>();
            }
            manager.EndGame(result, summary);
        }

        // 個性が debuffThreshold 未満の間だけ足が遅くなる
        private void UpdateDebuff()
        {
            if (mover == null) return;
            var balance = context.Balance;
            bool debuffed = context.Parameters.Get(ParameterType.Individuality) < balance.debuffThreshold;
            mover.SpeedMultiplier = debuffed ? balance.debuffSpeedMultiplier : 1f;
        }
    }
}
