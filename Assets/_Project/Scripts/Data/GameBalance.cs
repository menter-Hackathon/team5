using UnityEngine;

namespace Team5.Data
{
    [CreateAssetMenu(menuName = "Team5/GameBalance")]
    public class GameBalance : ScriptableObject
    {
        [Header("時間")]
        public float timeLimitSeconds = 300f;
        [Header("個性の濃さ")]
        public float individualityInitial = 50f;
        public float individualityMax = 100f;
        public float debuffThreshold = 20f;     // これ未満で移動速度が落ちる
        public float debuffSpeedMultiplier = 0.7f;
        [Header("村の平和度")]
        public float peaceInitial = 70f;
        public float peaceMax = 100f;
        public float caughtPeacePenalty = 25f;  // らしくない行動を見られたとき
        public float caughtCooldownSeconds = 3f; // 連続で減りすぎないための間隔
        [Header("ロールへの進み具合")]
        public float roleProgressMax = 100f;
        public float roleProgressToWin = 100f;
        [Header("お題")]
        public float taskIntervalSeconds = 20f; // 次のお題が出るまで
    }
}
