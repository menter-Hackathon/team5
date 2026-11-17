using Team5.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 仮の結果画面。見た目の作り込みは CD-15
    public class ResultView : MonoBehaviour
    {
        [SerializeField] private Text resultText;

        private void Start()
        {
            var result = GameManager.Instance != null ? GameManager.Instance.LastResult : GameResult.None;
            resultText.text = ToMessage(result);
        }

        // 「タイトルへ」ボタンから呼ぶ
        public void OnBackToTitlePressed()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager がありません。Title シーンから再生してください。", this);
                return;
            }
            GameManager.Instance.ReturnToTitle();
        }

        private static string ToMessage(GameResult result)
        {
            switch (result)
            {
                case GameResult.Clear: return "クリア！";
                case GameResult.GameOverPeace: return "平和度が0になった…";
                case GameResult.GameOverTimeUp: return "時間切れ…";
                case GameResult.GameOverFaded: return "色が消えてしまった…";
                default: return "結果なし";
            }
        }
    }
}
