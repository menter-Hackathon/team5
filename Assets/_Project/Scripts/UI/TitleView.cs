using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Team5.UI
{
    // 仮のタイトル画面。見た目の作り込みは CD-15、ロール選択は CD-20
    public class TitleView : MonoBehaviour
    {
        [SerializeField] private RoleDefinition defaultRole; // MVP は Role_Sample
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private UnityEvent<bool> onEntryRequested = new();

        private void Start()
        {
            // ロゴ → ボタン → チェック → 補足 の順にすべり込む
            string[] order = { "Logo", "StartButton", "Tutorial", "Note", "Keys", "Team" };
            for (int i = 0; i < order.Length; i++)
            {
                if (titlePanel != null && titlePanel.transform.Find(order[i]) is RectTransform rect)
                    UiFx.SlideIn(this, rect, new Vector2(-70f, 0f), 0.5f, 0.2f + i * 0.09f);
            }
            if (titlePanel != null && titlePanel.transform.Find("StartButton") is RectTransform start) StartCoroutine(Breathe(start));
        }

        // 「入村してみる」をゆっくり脈打たせて、押すところだと分かるようにする
        private System.Collections.IEnumerator Breathe(RectTransform target)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            while (true)
            {
                UiFx.Punch(this, target, 0.04f, 0.9f);
                yield return new WaitForSecondsRealtime(1.6f);
            }
        }

        // 「入村してみる」ボタンから呼ぶ
        public void OnStartPressed()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager がありません。Title シーンから再生してください。", this);
                return;
            }
            // 「体験入村してみる」にチェック、または初めて遊ぶときはチュートリアルから（CD-16）
            var toggle = titlePanel != null ? titlePanel.GetComponentInChildren<UnityEngine.UI.Toggle>(true) : null;
            TutorialView.Requested = (toggle != null && toggle.isOn) || TutorialView.ShouldShowOnFirstPlay;
            GameManager.Instance.StartGame(defaultRole);
        }

        public void EnterVillage()
        {
            onEntryRequested.Invoke(false);
        }

        public void EnterTrialVillage()
        {
            onEntryRequested.Invoke(true);
        }

        public void OpenSettings()
        {
            titlePanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            settingsPanel.SetActive(false);
            titlePanel.SetActive(true);
        }
    }
}
