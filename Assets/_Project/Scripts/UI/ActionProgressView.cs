using Team5.Core;
using Team5.Player;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // プレイヤーの頭の上に、お題の動作の進み具合を円ゲージで出す
    public class ActionProgressView : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private GameObject root;
        [SerializeField] private Image fill; // Filled / Radial 360

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            root.SetActive(false);
        }

        private void LateUpdate()
        {
            bool acting = interactor != null && interactor.Current is TaskSpot spot && spot.IsActing;
            if (root.activeSelf != acting) root.SetActive(acting);
            if (!acting) return;

            var task = GameSceneContext.Instance.Tasks.Current;
            fill.fillAmount = task != null ? task.Progress : 0f;
            if (mainCamera != null) root.transform.rotation = mainCamera.transform.rotation; // いつもカメラの方を向く
        }
    }
}
