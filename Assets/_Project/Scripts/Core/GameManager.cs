using System;
using Team5.Data;
using UnityEngine;

#pragma warning disable CS0067 // 雛形のため、まだ呼ばれないイベントの警告を止める（CD-04 で実装したら消す）

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
            Debug.Log("TODO: GameManager.StartGame");
        }

        // Result シーンへ
        public void EndGame(GameResult result, GameResultSummary summary)
        {
            Debug.Log("TODO: GameManager.EndGame");
        }

        public void ReturnToTitle()
        {
            Debug.Log("TODO: GameManager.ReturnToTitle");
        }

        // Time.timeScale を 0/1 にする
        public void SetPaused(bool paused)
        {
            Debug.Log("TODO: GameManager.SetPaused");
        }
    }
}
