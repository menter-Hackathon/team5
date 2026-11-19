using UnityEngine;

namespace Team5.Core
{
    // このロールのときだけ出す物（素材・作業台など）に付ける。ほかのロールのときは最初に消す
    public class RoleOnly : MonoBehaviour
    {
        [SerializeField] private string roleId = "ramen_chef";
        private const string FallbackRoleId = "ramen_chef"; // Game シーンを直接再生したとき

        private void Awake()
        {
            var role = GameManager.Instance != null ? GameManager.Instance.CurrentRole : null;
            string current = role != null && !string.IsNullOrEmpty(role.roleId) ? role.roleId.Trim() : FallbackRoleId;
            if (current != roleId) gameObject.SetActive(false);
        }
    }
}
