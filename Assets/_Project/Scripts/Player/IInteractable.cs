namespace Team5.Player
{
    public interface IInteractable
    {
        string Prompt { get; }                 // 画面に出す文「拾う：鶏ガラ」
        bool IsSuspicious { get; }             // 見られているときにやると「らしくない行動」になるか
        bool CanInteract(PlayerInteractor by);
        void Interact(PlayerInteractor by);
    }
}
