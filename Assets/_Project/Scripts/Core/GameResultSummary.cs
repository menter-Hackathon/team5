using System;

namespace Team5.Core
{
    [Serializable]
    public struct GameResultSummary
    {
        public float individuality;   // 終了時の個性の濃さ（0〜100）
        public float peace;
        public float roleProgress;
        public float elapsedSeconds;
        public int tasksCompleted;
        public int timesCaught;
    }
}
