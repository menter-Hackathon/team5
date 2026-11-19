using System;
using Team5.Data;
using Team5.UI;
using UnityEngine;

namespace Team5.Core
{
    // Title シーンに置く。DontDestroyOnLoad。
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }
        public RoleDefinition CurrentRole { get; private set; }   // 今回のロール
        public GameResult LastResult { get; private set; }        // 直前のゲームの結果
        public GameResultSummary LastSummary { get; private set; } // 結果画面に出す数値
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Game シーンを読み込み Playing にする
        public void StartGame(RoleDefinition role)
        {
            CurrentRole = role;
            LastResult = GameResult.None;
            Time.timeScale = 1f;
            SceneFader.Instance.LoadScene(SceneNames.Game, () => SetState(GameState.Playing));
        }

        // Result シーンへ
        public void EndGame(GameResult result, GameResultSummary summary)
        {
            LastResult = result;
            LastSummary = summary;
            Time.timeScale = 1f;
            SetState(GameState.Result);
            SceneFader.Instance.LoadScene(SceneNames.Result);
        }

        public void ReturnToTitle()
        {
            Time.timeScale = 1f;
            SetState(GameState.Title);
            SceneFader.Instance.LoadScene(SceneNames.Title);
        }

        // Time.timeScale を 0/1 にする
        public void SetPaused(bool paused)
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            Time.timeScale = paused ? 0f : 1f;
            SetState(paused ? GameState.Paused : GameState.Playing);
        }

        private void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
