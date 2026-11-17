using UnityEngine;
using UnityEngine.Events;

namespace Team5.UI
{
    public sealed class TitleView : MonoBehaviour
    {
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private UnityEvent<bool> onEntryRequested = new();

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
