using Team5.Data;

namespace Team5.Tasks
{
    public class TaskInstance
    {
        public TaskDefinition Definition;
        public TaskState State;
        public float Progress;       // 0〜1
        public float TimeRemaining;
    }
}
